using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

// Capture Begin rather than polling the typewriter: repeated and manually skipped lines count too.
internal static class DialogueHistory
{
	internal sealed class Entry
	{
		internal string Text = "";
		internal string Speaker = "";
		internal TMPro.TMP_Text NameSource;
		internal Sprite Portrait;
		internal bool PortraitRight;
		internal bool PortraitFlipped;
		internal CharacterData Character;
		internal VoiceOverData Voice;
		internal bool HasVoice => Character != null && Voice != null;
	}
	internal sealed class Page
	{
		internal Entry Entry;
		internal string Text;
	}

	private static readonly List<Entry> Entries = new();
	private static readonly Dictionary<int, Entry> Labels = new();
	private static readonly Dictionary<int, Entry> NameLabels = new();
	private static readonly List<Page> Pages = new();
	private static int _selected;
	// Cards use indices 0..2; the left-hand menu uses these stable focus IDs.
	internal const int Card = 0, Older = 3, Newer = 4, Replay = 5, Exit = 6, Current = 7;
	// The line the game is showing right now is not listed as a card, but its voice can be replayed from the menu.
	internal static Entry LiveEntry { get; private set; }
	internal static int Focus { get; private set; }
	internal static int Selected => _selected;
	internal static Entry SelectedEntry => IsOpen && _selected >= 0 && _selected < Pages.Count ? Pages[_selected].Entry : null;
	private static bool _available;
	private static int _sceneHandle;
	private static Harmony _harmony;
	internal static bool IsOpen { get; private set; }
	internal static int Count => Entries.Count;
	internal static int VoicedCount
	{
		get { int count = 0; foreach (var entry in Entries) if (entry.HasVoice) count++; return count; }
	}
	internal static bool Available => _available;

	internal static void Init()
	{
		try
		{
			_harmony = new Harmony("local.belmontscurse.ultrawide.history");
			Patch(typeof(CutsceneLineReader), "Begin", nameof(BeforeBegin), nameof(AfterBegin));
			Patch(typeof(UILocalizedString), "UpdateString", null, nameof(AfterString));
			Patch(typeof(CutsceneLineReader), "HandleUserSkipped", nameof(AllowSkip), null);
			Patch(typeof(CutsceneLineReader), "Deactivate", null, nameof(AfterDeactivate));
			Patch(typeof(CutsceneDialogueBoxUI), "Begin", null, nameof(AfterBoxBegin));
			Patch(typeof(CutscenePortraitUI), "SetPortrait", null, nameof(AfterPortrait));
			Patch(typeof(CutscenePortraitUI), "Begin", nameof(BeforePortraitBegin), nameof(AfterPortraitBegin));
			_available = true;
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			Plugin.Instance.Log.LogWarning("Dialogue history unavailable: " + ex.Message);
		}
	}

	private static void Patch(Type type, string method, string prefix, string postfix)
	{
		_harmony.Patch(AccessTools.Method(type, method),
			prefix == null ? null : new HarmonyMethod(typeof(DialogueHistory).GetMethod(prefix, BindingFlags.NonPublic | BindingFlags.Static)),
			postfix == null ? null : new HarmonyMethod(typeof(DialogueHistory).GetMethod(postfix, BindingFlags.NonPublic | BindingFlags.Static)));
	}

	private static bool AllowSkip() => !IsOpen;

	// The line the game is showing right now: its reader has not deactivated yet.
	internal static bool IsLive(Entry entry) => Labels.ContainsValue(entry);

	private static void BeforePortraitBegin(CutscenePortraitUI __instance)
	{
		try { if (__instance._actorNameLabel != null) NameLabels.Remove(__instance._actorNameLabel.GetInstanceID()); }
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("History speaker reset unavailable: " + ex.Message); }
	}

	private static void AfterPortraitBegin(CutscenePortraitUI __instance, CharacterData characterData, VoiceOverData voiceOverData)
	{
		try
		{
			if (!Plugin.HistoryEnabled.Value) return;
			var reader = __instance._lineReader;
			if (reader == null || reader._dialogueLabel == null || !Labels.TryGetValue(reader._dialogueLabel.GetInstanceID(), out var entry)) return;
			var name = __instance._actorNameLabel;
			if (name != null)
			{
				NameLabels[name.GetInstanceID()] = entry;
				if (name.textDisplay != null && !string.IsNullOrWhiteSpace(name.textDisplay.text))
					entry.Speaker = Regex.Replace(name.textDisplay.text, "<[^>]*>", "").Trim();
			}
			RecordPortrait(entry, __instance, __instance._foregroundImage != null ? __instance._foregroundImage.sprite : null);
			RecordVoice(entry, characterData, voiceOverData);
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("History dialogue card details unavailable: " + ex.Message); }
	}

	private static void AfterPortrait(CutscenePortraitUI __instance, Sprite portrait, UnityEngine.UI.Image portraitImage)
	{
		try
		{
			if (!Plugin.HistoryEnabled.Value || portraitImage != __instance._foregroundImage) return;
			var reader = __instance._lineReader;
			if (reader != null && reader._dialogueLabel != null && Labels.TryGetValue(reader._dialogueLabel.GetInstanceID(), out var entry))
				RecordPortrait(entry, __instance, portrait);
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("History portrait unavailable: " + ex.Message); }
	}

	private static void RecordPortrait(Entry entry, CutscenePortraitUI portrait, Sprite sprite)
	{
		entry.Portrait = GameUiSprites.Keep(sprite);
		entry.PortraitRight = portrait._isRight;
		entry.PortraitFlipped = portrait._foregroundImage != null && portrait._foregroundImage.transform.lossyScale.x < 0;
	}

	private static void RecordVoice(Entry entry, CharacterData character, VoiceOverData voice)
	{
		if (character == null || voice == null || !voice.TryGetID(out string id) || string.IsNullOrWhiteSpace(id)) return;
		if (voice.GetVoiceRef(character).IsNull) return;
		entry.Character = character;
		entry.Voice = VoiceOverData.Create(id, voice._overrideInHead, voice._inHead);
	}

	private static void AfterDeactivate(CutsceneLineReader __instance)
	{
		try
		{
			if (__instance._dialogueLabel != null && Labels.Remove(__instance._dialogueLabel.GetInstanceID(), out var entry))
			{
				FillSpeaker(entry); var stale = new List<int>();
				foreach (var pair in NameLabels) if (ReferenceEquals(pair.Value, entry)) stale.Add(pair.Key);
				foreach (int id in stale) NameLabels.Remove(id);
			}
		}
		catch (Exception ex) { FailCapture(ex); }
	}

	private static void AfterBoxBegin(CutsceneDialogueBoxUI __instance, CharacterData characterData, VoiceOverData voiceOverData)
	{
		if (!Plugin.HistoryEnabled.Value || characterData == null) return;
		try
		{
			string speaker = characterData.GetDisplayName()?.GetLocalizedString() ?? characterData.characterID;
			foreach (var reader in __instance.GetComponentsInChildren<CutsceneLineReader>(true))
				if (!string.IsNullOrWhiteSpace(speaker) && reader._dialogueLabel != null && Labels.TryGetValue(reader._dialogueLabel.GetInstanceID(), out var entry))
					entry.Speaker = speaker;
			var portrait = __instance._currentPortrait;
			if (portrait != null && portrait._lineReader != null && portrait._lineReader._dialogueLabel != null &&
				Labels.TryGetValue(portrait._lineReader._dialogueLabel.GetInstanceID(), out var current))
			{
				RecordPortrait(current, portrait, portrait._foregroundImage != null ? portrait._foregroundImage.sprite : null);
				RecordVoice(current, characterData, voiceOverData);
			}
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("History speaker name unavailable: " + ex.Message); }
	}

	// Fallback: the game's own "ActorName" label next to the dialogue text, read while the line is still on screen.
	private static TMPro.TMP_Text BoxName(TMPro.TMP_Text line)
	{
		var parent = line.transform.parent;
		return parent == null ? null : parent.Find("ActorName")?.GetComponent<TMPro.TMP_Text>();
	}

	private static void FillSpeaker(Entry entry)
	{
		try
		{
			if (entry.Speaker.Length > 0 || entry.NameSource == null) return;
			entry.Speaker = Regex.Replace(entry.NameSource.text ?? "", "<[^>]*>", "").Trim();
		}
		catch { }
	}

	private static void BeforeBegin(CutsceneLineReader __instance)
	{
		try
		{
			if (!Plugin.HistoryEnabled.Value || __instance._dialogueLabel == null) return;
			int scene = SceneManager.GetActiveScene().handle;
			if (_sceneHandle != scene) { Labels.Clear(); NameLabels.Clear(); _sceneHandle = scene; }
			var entry = new Entry();
			Entries.Add(entry);
			Labels[__instance._dialogueLabel.GetInstanceID()] = entry;
			Trim();
		}
		catch (Exception ex) { FailCapture(ex); }
	}

	private static void AfterBegin(CutsceneLineReader __instance)
	{
		try
		{
			VoiceAdvance.Track(__instance);
			var label = __instance._dialogueLabel;
			if (Plugin.HistoryEnabled.Value && label != null && label.textDisplay != null)
				{ Capture(label, label.textDisplay.text); if (Labels.TryGetValue(label.GetInstanceID(), out var begun)) begun.NameSource = BoxName(label.textDisplay); }
		}
		catch (Exception ex) { FailCapture(ex); }
	}

	private static void AfterString(UILocalizedString __instance, string value)
	{
		// Runs for every localized string the game updates: bail out at once unless a dialogue line is being tracked.
		if (Labels.Count == 0 && NameLabels.Count == 0) return;
		try { if (Plugin.HistoryEnabled.Value) Capture(__instance, value); }
		catch (Exception ex) { FailCapture(ex); }
	}

	private static void FailCapture(Exception ex)
	{
		Plugin.HistoryEnabled.Value = false;
		Plugin.Instance.Log.LogWarning("Dialogue history capture disabled: " + ex.Message);
	}

	private static void Capture(UILocalizedString label, string text)
	{
		if (NameLabels.TryGetValue(label.GetInstanceID(), out var named) && !string.IsNullOrWhiteSpace(text))
			named.Speaker = Regex.Replace(text, "<[^>]*>", "").Trim();
		if (!Labels.TryGetValue(label.GetInstanceID(), out var entry)) return;
		// Keep the localized full string, not the number of currently visible typewriter characters.
		entry.Text = Regex.Replace(Regex.Replace(text ?? "", @"<br\s*/?>", "\n", RegexOptions.IgnoreCase), "<[^>]*>", "").Trim();
	}

	private static void Trim()
	{
		int excess = Entries.Count - Math.Clamp(Plugin.HistoryLimit.Value, 20, 1000);
		if (excess <= 0) return;
		Entries.RemoveRange(0, excess);
		var stale = new List<int>();
		foreach (var pair in Labels) if (!Entries.Contains(pair.Value)) stale.Add(pair.Key);
		foreach (int key in stale) Labels.Remove(key);
		stale.Clear();
		foreach (var pair in NameLabels) if (!Entries.Contains(pair.Value)) stale.Add(pair.Key);
		foreach (int key in stale) NameLabels.Remove(key);
	}

	internal static void Toggle()
	{
		if (IsOpen) { Close(); return; }
		if (!_available || !Plugin.HistoryEnabled.Value) { Toast.Show("Dialogue history is disabled"); return; }
		try
		{
			LiveEntry = null;
			for (int i = Entries.Count - 1; i >= 0; i--) if (IsLive(Entries[i])) { LiveEntry = Entries[i]; break; }
			if (LiveEntry != null) FillSpeaker(LiveEntry);
			HistoryView.Open();
			Trim();
			Pages.Clear();
			foreach (var entry in Entries)
			{
				if (entry.Text.Length == 0) continue;
				if (ReferenceEquals(entry, LiveEntry)) continue;
				// One recorded dialogue line is one history card, regardless of its length.
				Pages.Add(new Page { Entry = entry, Text = entry.Text });
			}
			if (Pages.Count == 0) Pages.Add(new Page { Entry = new Entry(), Text = "No dialogue recorded yet. History starts when this mod is loaded." });
			_selected = Pages.Count - 1;
			Focus = Card;
			IsOpen = true;
			HistoryView.Show(Pages, _selected, true);
		}
		catch (Exception ex) { Close(); Plugin.Instance.Log.LogWarning("Could not open dialogue history: " + ex.Message); }
	}

	internal static void Tick()
	{
		if (!Plugin.HistoryEnabled.Value)
		{
			Close(); Entries.Clear(); Labels.Clear(); NameLabels.Clear(); Pages.Clear(); return;
		}
		if (!IsOpen) return;
		HistoryVoice.Tick();
		HistoryView.Tick();
		var kb = Keyboard.current;
		var pad = Plugin.PadShortcuts.Value ? Gamepad.current : null;
		if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame)) { Close(); return; }
		int change = 0;
		if ((kb != null && kb.pageUpKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) change--;
		if ((kb != null && kb.pageDownKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) change++;
		if (change != 0) ChangePage(change);
		int vertical = 0, horizontal = 0;
		if ((kb != null && kb.upArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.up.wasPressedThisFrame)) vertical--;
		if ((kb != null && kb.downArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.down.wasPressedThisFrame)) vertical++;
		if ((kb != null && kb.leftArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.left.wasPressedThisFrame)) horizontal--;
		if ((kb != null && kb.rightArrowKey.wasPressedThisFrame) || (pad != null && pad.dpad.right.wasPressedThisFrame)) horizontal++;
		if (horizontal < 0 && Focus < Older) Focus = Older;
		else if (horizontal > 0 && Focus >= Older) Focus = Card;
		else if (vertical != 0)
		{
			if (Focus == Current) { if (vertical < 0) Focus = Card; }
			else if (Focus < Older && vertical > 0 && LiveEntry != null && _selected == Pages.Count - 1) Focus = Current;
			else if (Focus < Older)
			{
				int next = Math.Clamp(_selected + vertical, 0, Pages.Count - 1);
				if (next != _selected) HistoryVoice.Stop();
				_selected = next;
			}
			else
			{
				// Visual order in the header: older, newer, replay, close.
				int[] actions = { Older, Newer, Replay, Exit };
				int next = Math.Clamp(Array.IndexOf(actions, Focus) + vertical, 0, actions.Length - 1);
				Focus = actions[next];
			}
		}
		if (horizontal != 0 || vertical != 0) Refresh();
		bool activate = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) ||
			(pad != null && pad.buttonSouth.wasPressedThisFrame);
		if (activate)
		{
			if (Focus == Exit) { Close(); return; }
			if (Focus == Older) ChangePage(-1);
			else if (Focus == Newer) ChangePage(1);
			else HistoryVoice.Toggle(Focus == Current ? LiveEntry : SelectedEntry);
		}
		if (!activate && ((kb != null && kb[Plugin.KeyReplay.Value].wasPressedThisFrame) || (pad != null && pad[Plugin.PadReplay.Value].wasPressedThisFrame)))
			HistoryVoice.Toggle(Focus == Current ? LiveEntry : SelectedEntry);
	}

	// Older/Newer move the selection by one screenful; the view scrolls to follow.
	private static void ChangePage(int change)
	{
		int next = Math.Clamp(_selected + change * HistoryView.Capacity, 0, Pages.Count - 1);
		if (next == _selected) return;
		HistoryVoice.Stop();
		_selected = next;
		if (Focus == Current) Focus = Card;
		Refresh();
	}

	private static void Close()
	{
		IsOpen = false;
		HistoryVoice.Stop();
		HistoryView.Close();
	}

	private static void Refresh() => HistoryView.Show(Pages, _selected, false);
}

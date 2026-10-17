using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

// The panel behind the "Mod settings" row in the game's Settings menu: every on/off option of the mod, in the game's panel style.
internal static class ModMenu
{
	private enum Kind { Header, Toggle, Close }

	private sealed class Item
	{
		internal Kind Kind;
		internal string Name, Note;
		internal ConfigEntry<bool> Entry;
	}

	private sealed class Row
	{
		internal GameObject Root;
		internal Image Back;
		internal TextMeshProUGUI Name, Value;
	}

	private static readonly List<Item> Items = new();
	private static readonly List<Row> Rows = new();
	private static GameObject _root, _anchor;
	private static CanvasScaler _scaler;
	private static RectTransform _panel;
	private static Image _frame;
	private static TextMeshProUGUI _title;
	private static readonly Image[] Icons = new Image[3];
	private static readonly TextMeshProUGUI[] FooterLabels = new TextMeshProUGUI[3];
	private static int _sel, _top, _visible;
	private static float _repeatAt, _previousScale = 1;
	private static bool _global;
	private static int _fontVersion = -1;
	private static EventSystem _events;
	private static bool _eventsWere;
	private static readonly Color Gold = new(0.85f, 0.729f, 0.508f, 1);
	private static readonly Color Bright = new(1f, 0.94f, 0.78f, 1);
	private static readonly Color Dim = new(0.72f, 0.63f, 0.46f, 1);
	private static readonly Color Glow = new(1f, 0.72f, 0.3f, 0.22f);

	private const float PanelWidth = 1000, RowHeight = 46, TitleHeight = 78, FooterHeight = 84;

	internal static bool IsOpen { get; private set; }

	// F1 / controller shortcut: open from anywhere, even when the Settings screen is unusable. The game is paused meanwhile.
	internal static void Toggle()
	{
		if (IsOpen) { Close(); return; }
		if (DialogueHistory.IsOpen) return;
		Open(null);
	}

	private static void Header(string name) => Items.Add(new Item { Kind = Kind.Header, Name = name });
	private static void Toggle(string name, ConfigEntry<bool> entry, string note = null) => Items.Add(new Item { Kind = Kind.Toggle, Name = name, Entry = entry, Note = note });

	private static void BuildItems()
	{
		if (Items.Count > 0) return;
		Header("Ultrawide fixes");
		Toggle("Camera aspect limit", Plugin.Camera16x9Logic);
		Toggle("Camera zoom fix", Plugin.FixScreenSize);
		Toggle("Camera viewport fix", Plugin.FixCameraRect);
		Toggle("Disable camera confiners", Plugin.DisableConfiners);
		Toggle("Fit videos", Plugin.FixVideos);
		Toggle("Hide cutscene bars", Plugin.RemoveLetterbox);
		Toggle("Center menus", Plugin.CenterMenus, "next launch");
		Toggle("Fit narrow screens", Plugin.FixNarrow, "next launch");
		Header("HUD");
		Toggle("Centered HUD", Plugin.CenterHud);
		Toggle("Bigger HUD", Plugin.BigHud);
		Header("Dialogue");
		Toggle("Auto-advance (voiced)", Plugin.AutoAdvance);
		Toggle("Auto-advance (no voice)", Plugin.AutoAdvanceUnvoiced);
		Toggle("Dialogue history", Plugin.HistoryEnabled, "restart to enable");
		Header("Other");
		Toggle("On-screen messages", Plugin.ShowToasts);
		Toggle("Controller shortcuts", Plugin.PadShortcuts);
		Toggle("Hide mouse when idle", Plugin.HideMouse);
		Toggle("TAA in Video menu", Plugin.EnableTaaOption, "reopen Settings");
		Toggle("Skip intros", Plugin.SkipIntros, "next launch");
		Toggle("Force resolution", Plugin.ForceResolution, "next launch");
		Items.Add(new Item { Kind = Kind.Close, Name = "Close" });
	}

	internal static void Open(GameObject anchor)
	{
		if (IsOpen) return;
		try
		{
			BuildItems();
			if (_root == null) Build();
			HistoryView.PrepareStyle();
			float height = Math.Min(HistoryView.UiHeight - 120, 860);
			_visible = Math.Max(5, (int)Math.Floor((height - TitleHeight - FooterHeight) / RowHeight));
			height = TitleHeight + FooterHeight + _visible * RowHeight;
			_scaler.matchWidthOrHeight = Screen.width / (float)Math.Max(1, Screen.height) < 16f / 9f ? 0 : 1;
			HistoryView.Place(_panel, new Vector2((HistoryView.UiWidth - PanelWidth) * 0.5f, -(HistoryView.UiHeight - height) * 0.5f), new Vector2(PanelWidth, height));
			HistoryView.Skin(_frame, HistoryView.CardHeight);
			HistoryView.Inflate(_frame.rectTransform, HistoryView.CardHeight);
			_frame.color = Color.white;
			HistoryView.Place(_title.rectTransform, new Vector2(40, -16), new Vector2(PanelWidth - 80, 52));
			while (Rows.Count < _visible) Rows.Add(BuildRow());
			for (int i = 0; i < Rows.Count; i++)
			{
				HistoryView.Place(Rows[i].Root.GetComponent<RectTransform>(), new Vector2(30, -(TitleHeight + i * RowHeight)), new Vector2(PanelWidth - 60, RowHeight - 4));
				HistoryView.Stretch(Rows[i].Back.rectTransform);
				HistoryView.Place(Rows[i].Name.rectTransform, new Vector2(18, 0), new Vector2(PanelWidth - 360, RowHeight - 4));
				HistoryView.Place(Rows[i].Value.rectTransform, new Vector2(PanelWidth - 60 - 160, 0), new Vector2(140, RowHeight - 4));
			}
			if (_fontVersion != GameUiFont.Version) { ApplyFont(); _fontVersion = GameUiFont.Version; }
			_anchor = anchor;
			_global = anchor == null;
			if (_global)
			{
				_previousScale = Time.timeScale;
				Time.timeScale = 0f;
				DialogueHistory.HookSkip(true);
			}
			_sel = 1; _top = 0;
			_events = EventSystem.current;
			// The game's own menu would also react to these keys; hold its input while this panel is open.
			if (_events != null) { _eventsWere = _events.enabled; _events.enabled = false; }
			_root.SetActive(true);
			IsOpen = true;
			Refresh();
		}
		catch (Exception ex)
		{
			Plugin.Instance.Log.LogWarning("Mod menu could not open: " + ex.Message);
			Close();
		}
	}

	internal static void Close()
	{
		IsOpen = false;
		if (_global)
		{
			_global = false;
			// Restore only if nothing else (the game's own pause) has changed it since.
			if (Time.timeScale == 0f) Time.timeScale = _previousScale > 0f ? _previousScale : 1f;
			if (!DialogueHistory.IsOpen) DialogueHistory.HookSkip(false);
		}
		if (_root != null) _root.SetActive(false);
		if (_events != null && _eventsWere) _events.enabled = true;
		_events = null;
	}

	private static void ApplyFont()
	{
		GameUiFont.Apply(_title);
		foreach (var l in FooterLabels) GameUiFont.Apply(l);
		foreach (var r in Rows) { GameUiFont.Apply(r.Name); GameUiFont.Apply(r.Value); }
	}

	internal static void Tick()
	{
		if (!IsOpen) return;
		// Back out together with the game's Settings screen (its Back button, tab change, closing the menu).
		if (!_global && (_anchor == null || !_anchor.activeInHierarchy)) { Close(); return; }
		var kb = Keyboard.current;
		var pad = Gamepad.current;
		int dir = 0;
		bool up = (kb != null && kb.upArrowKey.isPressed) || (pad != null && pad.dpad.up.isPressed);
		bool down = (kb != null && kb.downArrowKey.isPressed) || (pad != null && pad.dpad.down.isPressed);
		bool fresh = (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) ||
			(pad != null && (pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame));
		float now = Time.unscaledTime;
		if (up != down)
		{
			if (fresh) { dir = up ? -1 : 1; _repeatAt = now + 0.4f; }
			else if (now >= _repeatAt) { dir = up ? -1 : 1; _repeatAt = now + 0.07f; }
		}
		if (dir != 0) Move(dir);
		bool activate = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
		bool back = (kb != null && kb.backspaceKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
		if (back) { Close(); return; }
		if (activate)
		{
			var item = Items[_sel];
			if (item.Kind == Kind.Close) { Close(); return; }
			if (item.Kind == Kind.Toggle)
			{
				// The master switch is not saved, so it cannot be left off by accident for the next launch.
				if (item.Entry == Plugin.Enabled) Plugin.On = !Plugin.On; else item.Entry.Value = !item.Entry.Value;
				UwBehaviour.ApplyFixToggles();
				Plugin.Instance.Log.LogInfo("Mod menu: " + item.Name + " = " + ValueOf(item));
				Refresh();
			}
		}
		UpdateFooter();
	}

	private static bool ValueOf(Item item) => item.Entry == Plugin.Enabled ? Plugin.On : item.Entry.Value;

	private static void Move(int dir)
	{
		int next = _sel;
		do { next += dir; } while (next >= 0 && next < Items.Count && Items[next].Kind == Kind.Header);
		if (next < 0 || next >= Items.Count) return;
		_sel = next;
		Refresh();
	}

	private static void Refresh()
	{
		if (_sel < _top) _top = _sel;
		if (_sel >= _top + _visible) _top = _sel - _visible + 1;
		// Show the section heading above the first item of a section.
		if (_top > 0 && Items[_top].Kind != Kind.Header && Items[_top - 1].Kind == Kind.Header && _sel == _top) _top--;
		_title.text = "Mod settings";
		for (int i = 0; i < Rows.Count; i++)
		{
			var row = Rows[i];
			int index = _top + i;
			row.Root.SetActive(index < Items.Count && i < _visible);
			if (index >= Items.Count) continue;
			var item = Items[index];
			bool selected = index == _sel;
			row.Back.gameObject.SetActive(selected);
			string note = item.Note != null ? "  <size=65%><color=#b8a070>(" + item.Note + ")</color></size>" : "";
			switch (item.Kind)
			{
				case Kind.Header:
					row.Name.text = "<size=75%>" + item.Name.ToUpperInvariant() + "</size>"; row.Name.color = Dim; row.Value.text = ""; break;
				case Kind.Close:
					row.Name.text = item.Name; row.Name.color = selected ? Bright : Gold; row.Value.text = ""; break;
				default:
					row.Name.text = item.Name + note; row.Name.color = selected ? Bright : Gold;
					row.Value.text = ValueOf(item) ? "On" : "Off"; row.Value.color = ValueOf(item) ? Bright : Dim; break;
			}
		}
		UpdateFooter();
	}

	private static void UpdateFooter()
	{
		bool pad = Gamepad.current != null;
		SetFooter(0, pad ? GamepadGlyphs.Dpad() : null, "Up/Down", "Select", pad);
		SetFooter(1, pad ? GamepadGlyphs.Get(GamepadButton.South) : null, "Enter", "Change", pad);
		SetFooter(2, pad ? GamepadGlyphs.Get(GamepadButton.East) : null, "Backspace", "Back", pad);
	}

	private static void SetFooter(int slot, Sprite glyph, string key, string text, bool pad)
	{
		bool icon = pad && glyph != null;
		var image = Icons[slot];
		if (image.sprite != glyph) image.sprite = glyph;
		if (image.gameObject.activeSelf != icon) image.gameObject.SetActive(icon);
		string value = icon ? text : key + "  " + text;
		if (FooterLabels[slot].text != value) FooterLabels[slot].text = value;
		float cell = (PanelWidth - 80) / 3, x = 40 + cell * slot, top = -(TitleHeight + _visible * RowHeight);
		HistoryView.Place(image.rectTransform, new Vector2(x + 20, top - 22), new Vector2(36, 36));
		HistoryView.Place(FooterLabels[slot].rectTransform, new Vector2(x + (icon ? 66 : 20), top - 18), new Vector2(cell - 70, 44));
	}

	private static void Build()
	{
		_root = new GameObject("UwModMenu");
		UnityEngine.Object.DontDestroyOnLoad(_root);
		var canvas = _root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = short.MaxValue - 1;
		_scaler = _root.AddComponent<CanvasScaler>();
		_scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		_scaler.referenceResolution = new Vector2(1920, 1080);
		var group = _root.AddComponent<CanvasGroup>();
		group.interactable = false; group.blocksRaycasts = false;
		var dim = HistoryView.NewImage(_root, "Dim");
		HistoryView.Stretch(dim.rectTransform);
		dim.color = new Color(0, 0, 0, 0.55f);
		var panel = HistoryView.NewImage(_root, "Panel");
		panel.enabled = false;
		_panel = panel.rectTransform;
		_frame = HistoryView.NewImage(panel.gameObject, "Frame");
		HistoryView.Stretch(_frame.rectTransform);
		_title = HistoryView.Label(panel.gameObject, "Title", 34, TextAlignmentOptions.MidlineLeft);
		_title.color = Bright;
		for (int i = 0; i < 3; i++)
		{
			Icons[i] = HistoryView.NewImage(panel.gameObject, "Glyph" + i);
			Icons[i].preserveAspect = true;
			FooterLabels[i] = HistoryView.Label(panel.gameObject, "Hint" + i, 24, TextAlignmentOptions.MidlineLeft);
			FooterLabels[i].color = Gold;
		}
		_root.SetActive(false);
	}

	private static Row BuildRow()
	{
		var root = new GameObject("Row");
		root.AddComponent<RectTransform>().SetParent(_panel, false);
		var row = new Row { Root = root };
		row.Back = HistoryView.NewImage(root, "Selected");
		row.Back.color = Glow;
		row.Name = HistoryView.Label(root, "Name", 30, TextAlignmentOptions.MidlineLeft);
		row.Name.richText = true;
		row.Value = HistoryView.Label(root, "Value", 30, TextAlignmentOptions.MidlineRight);
		return row;
	}
}

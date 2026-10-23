using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

// The panel behind the "Mod settings" row in the game's Settings menu (and F1): the mod's options, in the game's panel style.
// Toggles switch on Enter / A / click; sliders move with Left / Right / click on either side of the value.
internal static class ModMenu
{
	private enum Kind { Header, Toggle, Slider, Reset, Close }

	private sealed class Item
	{
		internal Kind Kind;
		internal string Name, Note, Unit = "";
		internal ConfigEntry<bool> Toggle;
		internal ConfigEntry<float> Slider;
		internal float Min, Max, Step;
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
	private static int _sel, _top, _visible, _fontVersion = -1;
	private static float _repeatAt, _previousScale = 1, _panelLeft, _panelTop;
	private static bool _global, _confirmReset, _cursorWasVisible;
	private static EventSystem _events;
	private static bool _eventsWere;
	private static GameObject _previousSelection;
	private static bool _restoreInput;
	private static int _closedFrame;
	private static int _releasedFrame = -1;
	private static Harmony _inputHooks;
	private static int _footerDevice = -2, _footerVisible;
	private static bool _footerSlider;
	private static bool _retryFooterGlyphs;
	private static float _nextFooterRefresh;
	private static readonly Color Gold = new(0.85f, 0.729f, 0.508f, 1);
	private static readonly Color Bright = new(1f, 0.94f, 0.78f, 1);
	private static readonly Color Dim = new(0.72f, 0.63f, 0.46f, 1);
	private static readonly Color Glow = new(1f, 0.72f, 0.3f, 0.22f);

	private const float PanelWidth = 1000, RowHeight = 46, TitleHeight = 78, FooterHeight = 84;

	internal static bool IsOpen { get; private set; }

	internal static void Init()
	{
		try
		{
			_inputHooks = new Harmony("local.belmontscurse.ultrawide.modmenu");
			_inputHooks.Patch(AccessTools.Method(typeof(InputActionsToUnityEvent), "OnInputPerformed"),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(ModMenu), nameof(AllowGameInput))));
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("Mod menu input isolation unavailable: " + ex.Message); }
	}

	// The game also handles Back through InputActionsToUnityEvent, independently of EventSystem.
	// Keep the closing press isolated through its release, regardless of Update ordering.
	private static bool AllowGameInput() => !IsOpen && !_restoreInput;

	private static bool ClosingControlHeld()
	{
		var kb = Keyboard.current;
		var pad = Gamepad.current;
		var mouse = Mouse.current;
		return (kb != null && (kb.escapeKey.isPressed || kb.backspaceKey.isPressed || kb.enterKey.isPressed
			|| kb.numpadEnterKey.isPressed || (Plugin.KeyMenu.Value != Key.None && kb[Plugin.KeyMenu.Value].isPressed)))
			|| (pad != null && (pad.buttonEast.isPressed || pad.buttonSouth.isPressed || pad.selectButton.isPressed
				|| pad[Plugin.PadMenu.Value].isPressed))
			|| (mouse != null && mouse.leftButton.isPressed);
	}

	private static void RestoreInput()
	{
		if (_events != null && _eventsWere)
		{
			_events.enabled = true;
			if (_previousSelection != null && _previousSelection.activeInHierarchy)
				_events.SetSelectedGameObject(_previousSelection);
		}
		_events = null;
		_previousSelection = null;
		_restoreInput = false;
	}

	// F1 / controller shortcut: open from anywhere, even when the Settings screen is unusable. The game is paused meanwhile.
	internal static void Toggle()
	{
		if (IsOpen) { Close(); return; }
		if (DialogueHistory.IsOpen) return;
		Open(null);
	}

	private static void Header(string name) => Items.Add(new Item { Kind = Kind.Header, Name = name });
	private static void Switch(string name, ConfigEntry<bool> entry, string note = null) => Items.Add(new Item { Kind = Kind.Toggle, Name = name, Toggle = entry, Note = note });
	private static void Slide(string name, ConfigEntry<float> entry, float min, float max, float step, string unit = "") =>
		Items.Add(new Item { Kind = Kind.Slider, Name = name, Slider = entry, Min = min, Max = max, Step = step, Unit = unit });

	private static void BuildItems()
	{
		if (Items.Count > 0) return;
		Header("Ultrawide fixes");
		Switch("Camera zoom fix", Plugin.FixScreenSize);
		Switch("Camera viewport fix", Plugin.FixCameraRect);
		Switch("Disable camera confiners", Plugin.DisableConfiners);
		Switch("Fit videos", Plugin.FixVideos);
		Switch("Hide cutscene bars", Plugin.RemoveLetterbox);
		Switch("Center menus", Plugin.CenterMenus, "next launch");
		Switch("Fit narrow screens", Plugin.FixNarrow, "next launch");
		Header("HUD");
		Switch("Centered HUD", Plugin.CenterHud);
		Switch("Bigger HUD", Plugin.BigHud);
		Slide("Bigger HUD size", Plugin.HudScale, 1f, 1.6f, 0.05f, "x");
		Slide("HUD margin, sides", Plugin.HudSafeHorizontal, 0f, 20f, 1f, "%");
		Slide("HUD margin, top and bottom", Plugin.HudSafeVertical, 0f, 20f, 1f, "%");
		Header("Dialogue");
		Switch("Auto-advance (voiced)", Plugin.AutoAdvance);
		Switch("Auto-advance (no voice)", Plugin.AutoAdvanceUnvoiced);
		Slide("Wait after voice", Plugin.AutoAdvanceDelay, 0f, 3f, 0.1f, " s");
		Slide("Wait before no-voice line ends", Plugin.UnvoicedBaseSeconds, 0f, 5f, 0.5f, " s");
		Slide("Reading time per letter", Plugin.UnvoicedSecondsPerCharacter, 0.02f, 0.15f, 0.01f, " s");
		Switch("Dialogue history", Plugin.HistoryEnabled, "restart to enable");
		Switch("Save history to disk", Plugin.SaveHistory, "next launch");
		Header("Other");
		Switch("On-screen messages", Plugin.ShowToasts);
		Switch("Controller shortcuts", Plugin.PadShortcuts);
		Switch("Hide mouse when idle", Plugin.HideMouse);
		Switch("Keep mouse always hidden", Plugin.HideMouseAlways);
		Slide("Mouse idle time", Plugin.MouseIdleSeconds, 0.5f, 15f, 0.5f, " s");
		Switch("TAA in Video menu", Plugin.EnableTaaOption, "reopen Settings");
		Switch("Skip intros", Plugin.SkipIntros, "next launch");
		Switch("Force resolution", Plugin.ForceResolution, "next launch");
		Items.Add(new Item { Kind = Kind.Reset, Name = "Reset to defaults" });
		Items.Add(new Item { Kind = Kind.Close, Name = "Close" });
	}

	internal static void Open(GameObject anchor)
	{
		if (IsOpen) return;
		if (_restoreInput) RestoreInput();
		try
		{
			BuildItems();
			if (_root == null) Build();
			HistoryView.PrepareStyle();
			float height = Math.Min(HistoryView.UiHeight - 120, 860);
			_visible = Math.Max(5, (int)Math.Floor((height - TitleHeight - FooterHeight) / RowHeight));
			height = TitleHeight + FooterHeight + _visible * RowHeight;
			_scaler.matchWidthOrHeight = Screen.width / (float)Math.Max(1, Screen.height) < 16f / 9f ? 0 : 1;
			_panelLeft = (HistoryView.UiWidth - PanelWidth) * 0.5f;
			_panelTop = (HistoryView.UiHeight - height) * 0.5f;
			HistoryView.Place(_panel, new Vector2(_panelLeft, -_panelTop), new Vector2(PanelWidth, height));
			HistoryView.Skin(_frame, HistoryView.CardHeight);
			HistoryView.Inflate(_frame.rectTransform, HistoryView.CardHeight);
			_frame.color = Color.white;
			HistoryView.Place(_title.rectTransform, new Vector2(40, -16), new Vector2(PanelWidth - 80, 52));
			while (Rows.Count < _visible) Rows.Add(BuildRow());
			for (int i = 0; i < Rows.Count; i++)
			{
				HistoryView.Place(Rows[i].Root.GetComponent<RectTransform>(), new Vector2(30, -(TitleHeight + i * RowHeight)), new Vector2(PanelWidth - 60, RowHeight - 4));
				HistoryView.Stretch(Rows[i].Back.rectTransform);
				HistoryView.Place(Rows[i].Name.rectTransform, new Vector2(18, 0), new Vector2(PanelWidth - 400, RowHeight - 4));
				HistoryView.Place(Rows[i].Value.rectTransform, new Vector2(PanelWidth - 60 - 260, 0), new Vector2(240, RowHeight - 4));
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
			_cursorWasVisible = Cursor.visible;
			Cursor.visible = true;
			_sel = FirstSelectable(); _top = 0; _confirmReset = false;
			_events = EventSystem.current;
			_previousSelection = _events == null ? null : _events.currentSelectedGameObject;
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
		bool wasOpen = IsOpen;
		IsOpen = false;
		if (_global)
		{
			_global = false;
			// Restore only if nothing else (the game's own pause) has changed it since.
			// Put back exactly what was there: opening over the game's own pause (scale 0) must not unpause it.
			if (Time.timeScale == 0f) Time.timeScale = _previousScale;
			if (!DialogueHistory.IsOpen) DialogueHistory.HookSkip(false);
		}
		if (wasOpen) Cursor.visible = _cursorWasVisible;
		if (_root != null) _root.SetActive(false);
		if (wasOpen || _events != null)
		{
			_closedFrame = Time.frameCount;
			_releasedFrame = -1;
			_restoreInput = true;
		}
	}

	// The panel's feature was switched off after repeated failures: close it and return input now,
	// because Tick (which normally does that) will no longer run.
	internal static void Abort()
	{
		Close();
		if (_restoreInput) RestoreInput();
	}

	private static void ApplyFont()
	{
		GameUiFont.Apply(_title);
		foreach (var l in FooterLabels) GameUiFont.Apply(l);
		foreach (var r in Rows) { GameUiFont.Apply(r.Name); GameUiFont.Apply(r.Value); }
	}

	private static int FirstSelectable()
	{
		for (int i = 0; i < Items.Count; i++) if (Items[i].Kind != Kind.Header) return i;
		return 0;
	}

	internal static void Tick()
	{
		if (_restoreInput && Time.frameCount > _closedFrame)
		{
			if (ClosingControlHeld()) _releasedFrame = -1;
			else if (_releasedFrame < 0) _releasedFrame = Time.frameCount;
			else if (Time.frameCount > _releasedFrame) RestoreInput();
		}
		if (!IsOpen) return;
		// Back out together with the game's Settings screen (its Back button, tab change, closing the menu).
		if (!_global && (_anchor == null || !_anchor.activeInHierarchy)) { Close(); return; }
		var kb = Keyboard.current;
		var pad = Gamepad.current;
		float now = Time.unscaledTime;

		// Direction keys with hold-to-repeat.
		int dx = 0, dy = 0;
		bool left = (kb != null && kb.leftArrowKey.isPressed) || (pad != null && pad.dpad.left.isPressed);
		bool right = (kb != null && kb.rightArrowKey.isPressed) || (pad != null && pad.dpad.right.isPressed);
		bool up = (kb != null && kb.upArrowKey.isPressed) || (pad != null && pad.dpad.up.isPressed);
		bool down = (kb != null && kb.downArrowKey.isPressed) || (pad != null && pad.dpad.down.isPressed);
		bool fresh = (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) ||
			(pad != null && (pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame));
		bool step = false;
		if ((left != right) || (up != down))
		{
			if (fresh) { step = true; _repeatAt = now + 0.4f; }
			else if (now >= _repeatAt) { step = true; _repeatAt = now + 0.07f; }
		}
		if (step) { dx = left ? -1 : right ? 1 : 0; dy = up ? -1 : down ? 1 : 0; }
		if (dy != 0) Move(dy);
		if (dx != 0) Adjust(Items[_sel], dx);

		bool activate = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
		bool back = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)) || (pad != null && pad.buttonEast.wasPressedThisFrame);
		if (back) { Close(); return; }

		// Mouse: hover selects a row, a click acts on it, the wheel moves the selection.
		var mouse = Mouse.current;
		bool click = false;
		int clickSide = 0;
		if (mouse != null)
		{
			var pos = mouse.position.ReadValue();
			float ours = Math.Max(0.01f, HistoryView.Ours);
			float ux = pos.x / ours - _panelLeft, uy = (Screen.height - pos.y) / ours - _panelTop;
			int hovered = (int)Math.Floor((uy - TitleHeight) / RowHeight);
			bool inside = ux >= 30 && ux <= PanelWidth - 30 && hovered >= 0 && hovered < _visible && uy >= TitleHeight;
			if (inside && mouse.delta.ReadValue().sqrMagnitude > 0.01f)
			{
				int index = _top + hovered;
				if (index < Items.Count && Items[index].Kind != Kind.Header && index != _sel) { _sel = index; _confirmReset = false; Refresh(); }
			}
			if (inside && mouse.leftButton.wasPressedThisFrame && _top + hovered == _sel)
			{
				click = true;
				clickSide = ux > PanelWidth - 30 - 130 ? 1 : ux > PanelWidth - 30 - 260 ? -1 : 0;
			}
			float wheel = mouse.scroll.ReadValue().y;
			if (Math.Abs(wheel) > 0.01f) Move(wheel > 0 ? -1 : 1);
		}

		if (activate || click)
		{
			var item = Items[_sel];
			if (item.Kind == Kind.Slider)
			{
				if (click && clickSide != 0) Adjust(item, clickSide);
			}
			else Activate(item);
		}
		UpdateFooter();
	}

	private static void Activate(Item item)
	{
		switch (item.Kind)
		{
			case Kind.Close:
				Close();
				return;
			case Kind.Toggle:
				item.Toggle.Value = !item.Toggle.Value;
				UwBehaviour.ApplyFixToggles();
				Plugin.Instance.Log.LogInfo("Mod menu: " + item.Name + " = " + item.Toggle.Value);
				break;
			case Kind.Reset:
				// Two steps, so a stray press cannot wipe the settings.
				if (!_confirmReset) { _confirmReset = true; break; }
				ResetAll();
				_confirmReset = false;
				break;
		}
		Refresh();
	}

	private static void Adjust(Item item, int direction)
	{
		if (item.Kind == Kind.Toggle && direction != 0) { Activate(item); return; }
		if (item.Kind != Kind.Slider) return;
		float next = Logic.Step(item.Slider.Value, direction, item.Min, item.Max, item.Step);
		if (next == item.Slider.Value) return;
		item.Slider.Value = next;
		UwBehaviour.ApplyFixToggles();
		Refresh();
	}

	private static void ResetAll()
	{
		foreach (var item in Items)
		{
			if (item.Kind == Kind.Toggle) item.Toggle.Value = (bool)item.Toggle.DefaultValue;
			else if (item.Kind == Kind.Slider) item.Slider.Value = (float)item.Slider.DefaultValue;
		}
		UwBehaviour.ApplyFixToggles();
		Plugin.Instance.Log.LogInfo("Mod menu: all options reset to their defaults");
	}

	private static void Move(int dir)
	{
		int next = _sel;
		do { next += dir; } while (next >= 0 && next < Items.Count && Items[next].Kind == Kind.Header);
		if (next < 0 || next >= Items.Count) return;
		_sel = next;
		_confirmReset = false;
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
				case Kind.Reset:
					row.Name.text = _confirmReset && selected ? "Press again to reset everything" : item.Name;
					row.Name.color = selected ? Bright : Gold; row.Value.text = ""; break;
				case Kind.Slider:
					row.Name.text = item.Name; row.Name.color = selected ? Bright : Gold;
					row.Value.text = (selected ? "<  " : "") + Logic.FormatValue(item.Slider.Value, item.Step) + item.Unit + (selected ? "  >" : "");
					row.Value.color = selected ? Bright : Dim; break;
				default:
					row.Name.text = item.Name + note; row.Name.color = selected ? Bright : Gold;
					row.Value.text = item.Toggle.Value ? "On" : "Off"; row.Value.color = item.Toggle.Value ? Bright : Dim; break;
			}
		}
		UpdateFooter(true);
	}

	private static void UpdateFooter(bool force = false)
	{
		var controller = Gamepad.current;
		bool pad = controller != null;
		bool slider = Items.Count > _sel && Items[_sel].Kind == Kind.Slider;
		int device = controller?.deviceId ?? -1;
		float now = Time.unscaledTime;
		if (!force && device == _footerDevice && slider == _footerSlider && _visible == _footerVisible && (!_retryFooterGlyphs || now < _nextFooterRefresh)) return;
		_footerDevice = device; _footerSlider = slider; _footerVisible = _visible;
		// Retry occasionally for assets that were not available on the first attempt.
		_nextFooterRefresh = now + 0.5f;
		_retryFooterGlyphs = false;
		SetFooter(0, pad ? GamepadGlyphs.Dpad() : null, "Up/Down", "Select", pad);
		if (slider) SetFooter(1, pad ? GamepadGlyphs.Dpad() : null, "Left/Right", "Adjust", pad);
		else SetFooter(1, pad ? GamepadGlyphs.Get(GamepadButton.South) : null, "Enter", "Change", pad);
		SetFooter(2, pad ? GamepadGlyphs.Get(GamepadButton.East) : null, "Backspace", "Back", pad);
	}

	private static void SetFooter(int slot, Sprite glyph, string key, string text, bool pad)
	{
		bool icon = pad && glyph != null;
		if (pad && glyph == null) _retryFooterGlyphs = true;
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

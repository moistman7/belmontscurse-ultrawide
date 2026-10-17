using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

// Cards use the game's own dialogue panel art. Everything sits in the top of the screen so the live dialogue box stays readable.
internal static class HistoryView
{
	private sealed class Row
	{
		internal GameObject Root;
		internal int Index = -1;
		internal Image Back, Glow, Portrait;
		internal TextMeshProUGUI Speaker, Body;
	}

	private static readonly List<Row> Rows = new();
	private static readonly List<TextMeshProUGUI> Labels = new();
	private static GameObject _root;
	private static RectTransform _content;
	private static CanvasScaler _scaler;
	private sealed class Slot { internal GameObject Root; internal Image Back, Icon; internal TextMeshProUGUI Label; }
	private static TextMeshProUGUI _title;
	private static Image _bar;
	private static Slot _nav, _older, _newer, _close, _replay, _current;
	private static Image _currentBox, _currentFrame;
	private static Image _barFrame;
	private static IReadOnlyList<DialogueHistory.Page> _pages;
	private static int _sel;
	private static float _scroll, _target;
	private static bool _dirty;
	private static int _sig;
	private static float _width, _height, _cardHeight, _cardWidth, _uiWidth;
	private static Sprite _panel;
	private static UnityEngine.UI.Image.Type _panelType = UnityEngine.UI.Image.Type.Sliced;
	private static float _panelMul = 1, _liveScale = 1, _ours = 1;

	private static readonly Color Gold = new(0.85f, 0.729f, 0.508f, 1);
	private static readonly Color Bright = new(1f, 0.94f, 0.78f, 1);
	private static readonly Color Dim = new(0.72f, 0.63f, 0.46f, 1);
	private static readonly Color Glow = new(1f, 0.72f, 0.3f, 0.3f);
	private static readonly Color Idle = new(0.8f, 0.78f, 0.75f, 1);

	private const float HeaderHeight = 118, BarHeight = 84, BarPadY = 20, PadX = 90, PadY = 14, CurrentW = 340, CurrentH = 70, Gap = 12, LiveFraction = 0.36f, CardAspect = 5.4f;

	internal static int Capacity { get; private set; } = 2;

	internal static void Open()
	{
		if (_root == null) Build();
		foreach (var label in Labels) GameUiFont.Apply(label);
		ReadPanel();
		_root.SetActive(true);
		float aspect = Screen.width / (float)Math.Max(1, Screen.height);
		_scaler.matchWidthOrHeight = aspect < 16f / 9f ? 0 : 1;
		_uiWidth = aspect < 16f / 9f ? 1920 : 1080 * aspect;
		float uiHeight = aspect < 16f / 9f ? 1920 / aspect : 1080;
		_ours = Screen.height / uiHeight;
		// Game-sized cards (about 20% of screen height), shrunk only to fit two above the live dialogue.
		_height = uiHeight * (1 - LiveFraction) - HeaderHeight - 8;
		_cardHeight = Math.Min(uiHeight * 0.197f, (_height - Gap) / 2);
		_cardWidth = Math.Min(_cardHeight * CardAspect, _uiWidth - 120);
		_width = _cardWidth;
		Capacity = Math.Clamp((int)Math.Floor((_height + Gap) / (_cardHeight + Gap)), 2, 3);
		float left = (_uiWidth - _width) * 0.5f;
		Place(_bar.rectTransform, new Vector2(left, -BarPadY), new Vector2(_width, BarHeight));
		Skin(_barFrame, BarHeight); _barFrame.color = Color.white; Inflate(_barFrame.rectTransform, BarHeight);
		Place(_title.rectTransform, new Vector2(_width * 0.05f, -14), new Vector2(_width * 0.14f, BarHeight - 28));
		float colLeft = _width * 0.2f, col = _width * 0.76f / 5;
		var slots = new[] { _nav, _older, _newer, _replay, _close };
		for (int i = 0; i < slots.Length; i++)
		{
			Stretch(slots[i].Root.GetComponent<RectTransform>());
			float x = colLeft + col * i, size = 36;
			Place(slots[i].Back.rectTransform, new Vector2(x, -14), new Vector2(col - 6, BarHeight - 28));
			Place(slots[i].Icon.rectTransform, new Vector2(x + 6, -(BarHeight - size) * 0.5f), new Vector2(size, size));
			Place(slots[i].Label.rectTransform, new Vector2(x + size + 12, -14), new Vector2(col - size - 16, BarHeight - 28));
		}
		float view = Capacity * (_cardHeight + Gap) - Gap;
		_scroll = _target = 0; _pages = null; foreach (var r in Rows) { r.Index = -1; r.Root.SetActive(false); }
		Place(_content, new Vector2(left - PadX, -(HeaderHeight - PadY)), new Vector2(_width + PadX * 2, view + PadY * 2));
		// "Current" sits under the cards, above the game's own dialogue box.
		float cw = CurrentW, ch = CurrentH;
		Place(_currentBox.rectTransform, new Vector2(left + (_width - cw) * 0.5f, -(HeaderHeight + view + 8)), new Vector2(cw, ch));
		Skin(_currentFrame, ch); _currentFrame.color = Color.white; Inflate(_currentFrame.rectTransform, ch);
		// Selection looks like the cards: the same panel art as a warm glow.
		Skin(_current.Back, ch); Inflate(_current.Back.rectTransform, ch); _current.Back.color = Glow;
		Stretch(_current.Root.GetComponent<RectTransform>());
	}

	// The game's panel sprite and slicing, so cards share its frame art and corner size.
	private static void ReadPanel()
	{
		var panel = GameUiFont.FindDialoguePanel();
		if (panel == null) return;
		_panel = panel.sprite; _panelType = panel.type; _panelMul = panel.pixelsPerUnitMultiplier;
		_liveScale = panel.canvas != null ? panel.canvas.rootCanvas.scaleFactor : 1;
	}

	// The panel sprite has transparent padding around its visible frame; grow the art so the frame matches the card rectangle.
	private static void Inflate(RectTransform rect, float height)
	{
		float x = height * 0.35f, y = height * 0.045f;
		rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(-x, -y); rect.offsetMax = new Vector2(x, y);
	}

	// height = the panel's own height; short panels get proportionally smaller corners, like a miniature card.
	private static void Skin(Image image, float height)
	{
		image.sprite = _panel;
		image.type = _panel != null && (_panelType == UnityEngine.UI.Image.Type.Sliced || _panelType == UnityEngine.UI.Image.Type.Tiled) ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
		image.preserveAspect = false; image.fillCenter = true;
		// Keep the frame's corners the same physical size as the game's own box.
		image.pixelsPerUnitMultiplier = Math.Max(0.01f, _panelMul * _ours / Math.Max(0.01f, _liveScale) * _cardHeight / Math.Max(1, height));
		image.enabled = _panel != null;
	}

	internal static void Close() { if (_root != null) _root.SetActive(false); }

	// Selection moved or content changed: retarget the scroll. The scroll itself eases in Tick.
	internal static void Show(IReadOnlyList<DialogueHistory.Page> pages, int selected, bool snap)
	{
		_pages = pages; _sel = selected; _sig = 0;
		int n = pages.Count;
		_target = Math.Clamp(_target, Math.Max(0, selected - Capacity + 1), selected);
		_target = Math.Min(_target, Math.Max(0, n - Capacity));
		if (snap) _scroll = _target;
		_dirty = true;
		Tick();
	}

	private static void Bind(Row row, int i)
	{
		var entry = _pages[i].Entry;
		bool cursor = i == _sel && DialogueHistory.Focus < DialogueHistory.Older;
		row.Index = i;
		row.Root.SetActive(true);
		Skin(row.Back, _cardHeight); Skin(row.Glow, _cardHeight); Inflate(row.Back.rectTransform, _cardHeight); Inflate(row.Glow.rectTransform, _cardHeight);
		row.Back.color = cursor ? Color.white : Idle;
		row.Glow.color = Glow;
		row.Glow.gameObject.SetActive(cursor);
		var portrait = GameUiSprites.Trim(entry.Portrait);
		bool has = portrait != null, right = entry.PortraitRight;
		row.Portrait.sprite = portrait;
		row.Portrait.gameObject.SetActive(has);
		float side = _cardWidth * 0.16f, pw = 0;
		if (has)
		{
			float h = _cardHeight * 0.9f, w = h * portrait.rect.width / Math.Max(1, portrait.rect.height);
			w = Math.Min(w, side * 1.4f); pw = w;
			var rect = row.Portrait.rectTransform;
			Place(rect, new Vector2(right ? _cardWidth - side * 0.2f - w * 0.5f : side * 0.2f + w * 0.5f, -_cardHeight * 0.05f), new Vector2(w, h));
			rect.pivot = new Vector2(0.5f, 1);
			rect.localScale = new Vector3(entry.PortraitFlipped ? -1 : 1, 1, 1);
		}
		float textLeft = has && !right ? side * 0.2f + pw + 16 : _cardWidth * 0.06f;
		float textWidth = _cardWidth - textLeft - (has && right ? side * 0.2f + pw + 16 : _cardWidth * 0.06f);
		bool named = entry.Speaker.Length > 0;
		SetText(row.Speaker, entry.Speaker);
		row.Speaker.gameObject.SetActive(named);
		row.Speaker.color = cursor ? Bright : Gold;
		Place(row.Speaker.rectTransform, new Vector2(textLeft, -_cardHeight * 0.1f), new Vector2(textWidth, _cardHeight * 0.22f));
		float bodyTop = _cardHeight * (named ? 0.34f : 0.14f);
		Place(row.Body.rectTransform, new Vector2(textLeft, -bodyTop), new Vector2(textWidth, _cardHeight * 0.84f - bodyTop));
		row.Body.color = cursor ? Bright : Dim;
		SetText(row.Body, _pages[i].Text);
	}

	// Eases the scroll and positions only the few cards near the viewport; idle frames do nothing.
	private static void Scroll()
	{
		if (_pages == null) return;
		float diff = _target - _scroll;
		bool moving = Math.Abs(diff) > 0.002f;
		if (moving)
		{
			_scroll += diff * (1 - Mathf.Exp(-16f * Time.unscaledDeltaTime));
			if (Math.Abs(_target - _scroll) < 0.002f) _scroll = _target;
		}
		else if (_scroll != _target) { _scroll = _target; moving = true; }
		if (!moving && !_dirty) return;
		int n = _pages.Count, pool = Capacity + 3;
		int first = Math.Max(0, (int)Math.Floor(_scroll) - 1), last = Math.Min(n - 1, (int)Math.Ceiling(_scroll) + Capacity);
		while (Rows.Count < pool) Rows.Add(BuildRow());
		foreach (var r in Rows)
			if (r.Index < first || r.Index > last) { if (r.Root.activeSelf) r.Root.SetActive(false); r.Index = -1; }
		float stride = _cardHeight + Gap;
		for (int i = first; i <= last; i++)
		{
			var row = Rows[i % pool];
			if (row.Index != i || _dirty) Bind(row, i);
			Place(row.Root.GetComponent<RectTransform>(), new Vector2(PadX, -(PadY + (i - _scroll) * stride)), new Vector2(_cardWidth, _cardHeight));
		}
		_dirty = false;
	}

	// Called every frame while open; only touches text, sprites or colors that changed.
	internal static void Tick()
	{
		if (_root == null || !_root.activeSelf || _pages == null) return;
		Scroll();
		bool pad = Plugin.PadShortcuts.Value && Gamepad.current != null;
		var entry = DialogueHistory.SelectedEntry;
		var liveNow = DialogueHistory.LiveEntry;
		// Header text only changes with these; skip rebuilding strings on every other frame.
		int sig = HashCode.Combine(_sel, DialogueHistory.Focus, HistoryVoice.Playing, pad, entry?.HasVoice == true, liveNow != null && liveNow.HasVoice, _pages.Count);
		if (sig == _sig) return;
		_sig = sig;
		string action = HistoryVoice.Playing ? "Stop" : entry?.HasVoice == true ? "Voice" : "No voice";
		SetText(_title, "History\n" + (_sel + 1) + " / " + _pages.Count);
		Set(_nav, pad, GamepadGlyphs.Dpad(), "Up/Down", "Select", -1);
		Set(_older, pad, GamepadGlyphs.Get(GamepadButton.LeftShoulder), "PgUp", _sel > 0 ? "Older" : "Oldest", DialogueHistory.Older);
		Set(_newer, pad, GamepadGlyphs.Get(GamepadButton.RightShoulder), "PgDn", _sel < _pages.Count - 1 ? "Newer" : "Latest", DialogueHistory.Newer);
		Set(_replay, pad, GamepadGlyphs.Get(Plugin.PadReplay.Value), Plugin.KeyReplay.Value.ToString(), action, DialogueHistory.Replay);
		var live = DialogueHistory.LiveEntry;
		_currentBox.gameObject.SetActive(live != null);
		if (live != null) Set(_current, pad, GamepadGlyphs.Get(GamepadButton.South), "Enter", live.HasVoice ? (HistoryVoice.Playing ? "Stop" : "Current") : "Current", DialogueHistory.Current);
		if (liveNow != null) CenterCurrent();
		Set(_close, pad, GamepadGlyphs.Get(GamepadButton.East), "Esc", "Close", DialogueHistory.Exit);
	}

	// Glyph + label are centered as a group inside the Current panel.
	private static void CenterCurrent()
	{
		bool icon = _current.Icon.gameObject.activeSelf;
		float w = _current.Label.GetPreferredValues(_current.Label.text).x + 4, size = 36, gap = 10;
		float total = (icon ? size + gap : 0) + w, start = (CurrentW - total) * 0.5f;
		if (icon) Place(_current.Icon.rectTransform, new Vector2(start, -(CurrentH - size) * 0.5f), new Vector2(size, size));
		Place(_current.Label.rectTransform, new Vector2(start + (icon ? size + gap : 0), -10), new Vector2(w, CurrentH - 20));
	}

	private static void Set(Slot slot, bool pad, Sprite glyph, string key, string text, int focus)
	{
		bool on = focus >= 0 && DialogueHistory.Focus == focus, icon = pad && glyph != null;
		if (slot.Icon.sprite != glyph) slot.Icon.sprite = glyph;
		if (slot.Icon.gameObject.activeSelf != icon) slot.Icon.gameObject.SetActive(icon);
		SetText(slot.Label, icon ? text : key + "  " + text);
		slot.Label.color = on ? Bright : Gold;
		if (slot.Back.gameObject.activeSelf != on) slot.Back.gameObject.SetActive(on);
	}

	private static void SetText(TextMeshProUGUI label, string text) { if (label.text != text) label.text = text; }

	private static void Build()
	{
		_root = new GameObject("UwDialogueHistory");
		UnityEngine.Object.DontDestroyOnLoad(_root);
		var canvas = _root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = short.MaxValue - 1;
		_scaler = _root.AddComponent<CanvasScaler>();
		_scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		_scaler.referenceResolution = new Vector2(1920, 1080);
		var group = _root.AddComponent<CanvasGroup>();
		group.interactable = false; group.blocksRaycasts = false;
		_content = new GameObject("Transcript").AddComponent<RectTransform>();
		_content.SetParent(_root.transform, false);
		_content.gameObject.AddComponent<RectMask2D>();
		_title = Label(_root, "Title", 24, TextAlignmentOptions.MidlineLeft);
		_title.enableWordWrapping = true;
		_bar = NewImage(_root, "Header"); _bar.enabled = false;
		_barFrame = NewImage(_bar.gameObject, "Frame"); Stretch(_barFrame.rectTransform);
		_title.transform.SetParent(_bar.transform, false);
		_nav = BuildSlot("Navigate"); _older = BuildSlot("Older"); _newer = BuildSlot("Newer"); _replay = BuildSlot("Replay"); _close = BuildSlot("Close");
		_currentBox = NewImage(_root, "CurrentBox"); _currentBox.enabled = false;
		_currentFrame = NewImage(_currentBox.gameObject, "Frame"); Stretch(_currentFrame.rectTransform);
		_current = BuildSlot("Current"); _current.Root.transform.SetParent(_currentBox.transform, false);
	}

	private static Slot BuildSlot(string name)
	{
		var slot = new Slot { Root = new GameObject(name) };
		slot.Root.AddComponent<RectTransform>().SetParent(_bar.transform, false);
		slot.Back = NewImage(slot.Root, "Selected"); slot.Back.color = new Color(1f, 0.72f, 0.3f, 0.2f);
		slot.Icon = NewImage(slot.Root, "Glyph"); slot.Icon.preserveAspect = true;
		slot.Label = Label(slot.Root, "Label", 24, TextAlignmentOptions.MidlineLeft);
		slot.Label.enableAutoSizing = true; slot.Label.fontSizeMin = 12; slot.Label.fontSizeMax = 24;
		slot.Label.enableWordWrapping = true;
		return slot;
	}

	private static Row BuildRow()
	{
		var root = new GameObject("HistoryEntry");
		root.AddComponent<RectTransform>().SetParent(_content, false);
		var row = new Row { Root = root };
		row.Back = NewImage(root, "Card"); Stretch(row.Back.rectTransform);
		row.Glow = NewImage(root, "Selected"); Stretch(row.Glow.rectTransform);
		row.Portrait = NewImage(root, "Portrait"); row.Portrait.preserveAspect = true;
		row.Speaker = Label(root, "Speaker", 30, TextAlignmentOptions.TopLeft);
		row.Speaker.overflowMode = TextOverflowModes.Overflow;
		row.Body = Label(root, "Dialogue", 32, TextAlignmentOptions.TopLeft);
		row.Body.enableAutoSizing = true; row.Body.fontSizeMin = 16; row.Body.fontSizeMax = 32;
		row.Body.enableWordWrapping = true;
		return row;
	}

	private static TextMeshProUGUI Label(GameObject parent, string name, float size, TextAlignmentOptions align)
	{
		var go = new GameObject(name);
		go.AddComponent<RectTransform>().SetParent(parent.transform, false);
		var text = go.AddComponent<TextMeshProUGUI>();
		GameUiFont.Apply(text);
		text.fontSize = size; text.enableAutoSizing = false;
		text.richText = false; text.raycastTarget = false;
		text.alignment = align;
		text.enableWordWrapping = false;
		text.overflowMode = TextOverflowModes.Truncate;
		Labels.Add(text);
		return text;
	}

	private static Image NewImage(GameObject parent, string name)
	{
		var go = new GameObject(name);
		go.AddComponent<RectTransform>().SetParent(parent.transform, false);
		var image = go.AddComponent<Image>(); image.raycastTarget = false; return image;
	}

	private static void Place(RectTransform rect, Vector2 position, Vector2 size)
	{
		rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
		rect.anchoredPosition = position; rect.sizeDelta = size;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
		rect.offsetMin = rect.offsetMax = Vector2.zero;
	}
}

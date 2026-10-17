using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

// A short on-screen message ("Auto-advance: ON") so toggles give visible feedback.
internal static class Toast
{
	private const float ShowSeconds = 2f;

	private const float FadeSeconds = 0.4f;

	private static GameObject _root;

	private static TextMeshProUGUI _text;
	private static Image _background;

	private static CanvasGroup _group;

	private static float _hideAt;

	private static bool _failed;

	internal static void Show(string message)
	{
		if (!Plugin.ShowToasts.Value || _failed)
		{
			return;
		}
		try
		{
			if (_root == null)
			{
				Build();
			}
			GameUiFont.Apply(_text);
			ApplyGamePanel();
			_text.text = message;
			_group.alpha = 1f;
			_root.SetActive(true);
			_hideAt = Time.unscaledTime + ShowSeconds;
		}
		catch (Exception ex)
		{
			_failed = true;
			Plugin.Instance.Log.LogWarning("On-screen messages are unavailable: " + ex.Message);
		}
	}

	internal static void Tick()
	{
		if (_root == null || !_root.activeSelf)
		{
			return;
		}
		float left = _hideAt - Time.unscaledTime;
		if (left <= 0f)
		{
			_root.SetActive(false);
		}
		else if (left < FadeSeconds)
		{
			_group.alpha = left / FadeSeconds;
		}
	}

	private static void Build()
	{
		_root = new GameObject("UwToast");
		UnityEngine.Object.DontDestroyOnLoad(_root);
		Canvas canvas = _root.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = short.MaxValue;
		CanvasScaler scaler = _root.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 1f;
		_group = _root.AddComponent<CanvasGroup>();
		_group.interactable = false;
		_group.blocksRaycasts = false;

		GameObject panel = new GameObject("Panel");
		panel.transform.SetParent(_root.transform, false);
		_background = panel.AddComponent<Image>();
		_background.raycastTarget = false;
		_background.preserveAspect = true;
		RectTransform panelRect = panel.GetComponent<RectTransform>();
		panelRect.anchorMin = new Vector2(0.5f, 1f);
		panelRect.anchorMax = new Vector2(0.5f, 1f);
		panelRect.pivot = new Vector2(0.5f, 1f);
		panelRect.anchoredPosition = new Vector2(0f, -20f);
		panelRect.sizeDelta = new Vector2(600f, 160f);

		GameObject label = new GameObject("Text");
		label.transform.SetParent(panel.transform, false);
		_text = label.AddComponent<TextMeshProUGUI>();
		GameUiFont.Apply(_text);
		_text.fontSize = 30;
		_text.enableAutoSizing = true;
		_text.fontSizeMin = 20;
		_text.fontSizeMax = 30;
		_text.richText = false;
		_text.alignment = TextAlignmentOptions.Center;
		_text.raycastTarget = false;
		RectTransform labelRect = label.GetComponent<RectTransform>();
		labelRect.anchorMin = new Vector2(0.14f, 0.2f);
		labelRect.anchorMax = new Vector2(0.86f, 0.8f);
		labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
	}

	private static void ApplyGamePanel()
	{
		var source = GameUiFont.FindDialoguePanel();
		_background.sprite = GameUiSprites.Trim(source != null ? source.sprite : null);
		_background.enabled = _background.sprite != null;
		_background.type = Image.Type.Simple;
		_background.color = Color.white;
		float ratio = _background.sprite != null && _background.sprite.rect.height > 0
			? _background.sprite.rect.width / _background.sprite.rect.height : 3.75f;
		_background.rectTransform.sizeDelta = new Vector2(600, 600 / ratio);
	}
}

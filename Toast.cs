using System;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

// A short on-screen message ("Auto-advance: ON") so toggles give visible feedback.
internal static class Toast
{
	private const float ShowSeconds = 2f;

	private const float FadeSeconds = 0.4f;

	private static GameObject _root;

	private static Text _text;

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
		Image background = panel.AddComponent<Image>();
		background.color = new Color(0f, 0f, 0f, 0.7f);
		background.raycastTarget = false;
		RectTransform panelRect = panel.GetComponent<RectTransform>();
		panelRect.anchorMin = new Vector2(0.5f, 1f);
		panelRect.anchorMax = new Vector2(0.5f, 1f);
		panelRect.pivot = new Vector2(0.5f, 1f);
		panelRect.anchoredPosition = new Vector2(0f, -40f);
		panelRect.sizeDelta = new Vector2(560f, 64f);

		GameObject label = new GameObject("Text");
		label.transform.SetParent(panel.transform, false);
		_text = label.AddComponent<Text>();
		_text.font = LoadFont();
		_text.fontSize = 30;
		_text.alignment = TextAnchor.MiddleCenter;
		_text.color = new Color(1f, 0.9f, 0.6f, 1f);
		_text.raycastTarget = false;
		RectTransform labelRect = label.GetComponent<RectTransform>();
		labelRect.anchorMin = Vector2.zero;
		labelRect.anchorMax = Vector2.one;
		labelRect.offsetMin = Vector2.zero;
		labelRect.offsetMax = Vector2.zero;
	}

	private static Font LoadFont()
	{
		try
		{
			Font builtin = Resources.GetBuiltinResource(Il2CppType.Of<Font>(), "LegacyRuntime.ttf")?.TryCast<Font>();
			if (builtin != null)
			{
				return builtin;
			}
		}
		catch (Exception)
		{
		}
		return Font.CreateDynamicFontFromOSFont("Arial", 30);
	}
}

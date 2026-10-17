using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace BelmontsCurseUltrawide;

internal static class GameUiFont
{
	private static readonly Dictionary<int, Material> Materials = new();
	// The dialogue label once found; reused so toasts do not scan every text object in the scene.
	private static TMP_Text _source, _anyLabel;
	private static float _nextScan;
	private static UnityEngine.UI.Image _panel;
	internal static Material OwnMaterial(TMP_Text source)
	{
		var original = source != null ? source.fontSharedMaterial : null;
		if (original == null) return null;
		int id = original.GetInstanceID();
		if (!Materials.TryGetValue(id, out var material) || material == null)
		{
			material = new Material(original) { name = "UwUi_" + original.name };
			// Source labels may belong to a masked game menu; our overlays have their own layout.
			material.DisableKeyword("UNITY_UI_CLIP_RECT");
			material.DisableKeyword("MASK_SOFT");
			material.DisableKeyword("MASK_HARD");
			if (material.HasProperty("_StencilComp")) material.SetFloat("_StencilComp", 8);
			Materials[id] = material;
		}
		return material;
	}

	internal static UnityEngine.UI.Image FindDialoguePanel()
	{
		if (_panel != null && _panel.sprite != null) return _panel;
		return _panel = ScanDialoguePanel();
	}

	private static UnityEngine.UI.Image ScanDialoguePanel()
	{
		foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (canvas.name != "DialogueBoxFullScreen") continue;
			foreach (string side in new[] { "DialogueBoxRight", "DialogueBoxLeft" })
			{
				var panel = canvas.transform.Find(side + "/BackgroundBox")?.GetComponent<UnityEngine.UI.Image>();
				if (panel != null && panel.sprite != null) return panel;
			}
		}
		return null;
	}

	internal static void Apply(TMP_Text target)
	{
		TMP_Text fallback = _source;
		// Without the dialogue label yet, scan the scene at most once a second instead of once per label.
		if (fallback == null && Time.unscaledTime < _nextScan && _anyLabel != null) fallback = _anyLabel;
		else if (fallback == null) _nextScan = Time.unscaledTime + 1f;
		if (fallback == null)
		foreach (var label in UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
		{
			if (label.font == null || IsModUi(label.transform)) continue;
			fallback ??= label;
			if (label.name == "Line" && label.transform.parent != null &&
				(label.transform.parent.name == "DialogueBoxLeft" || label.transform.parent.name == "DialogueBoxRight"))
			{ fallback = _source = label; break; }
		}
		_anyLabel = fallback ?? _anyLabel;
		target.font = fallback != null ? fallback.font : TMP_Settings.defaultFontAsset;
		// Use the native dialogue palette, not a source label's transient animation color.
		target.color = new Color(0.85f, 0.729f, 0.508f, 1);
		if (fallback != null)
		{
			target.fontSharedMaterial = OwnMaterial(fallback);
			target.fontStyle = fallback.fontStyle;
		}
	}

	private static bool IsModUi(Transform transform)
	{
		for (var node = transform; node != null; node = node.parent)
			if (node.name == "UwToast" || node.name == "UwDialogueHistory") return true;
		return false;
	}
}

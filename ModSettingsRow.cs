using System;
using System.Collections.Generic;
using HarmonyLib;
using Doozy.Runtime.UIManager.Components;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

// Adds a "Mod settings" row to the game's Graphics settings tab. It is a copy of the game's own V-Sync row,
// so it looks and navigates like the rest; selecting it opens the mod's toggle panel.
internal static class ModSettingsRow
{
	private const string RowName = "SettingsDisplay_ModSettings";
	private static readonly HashSet<int> Done = new();

	internal static void Init()
	{
		try
		{
			var harmony = new Harmony("local.belmontscurse.ultrawide.modrow");
			harmony.Patch(AccessTools.Method(typeof(VideoSettingsView), "ConstructUI"), null,
				new HarmonyMethod(typeof(ModSettingsRow).GetMethod(nameof(After), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("Mod settings row unavailable: " + ex.Message); }
	}

	private static void After(VideoSettingsView __instance)
	{
		try
		{
			if (!Done.Add(__instance.GetInstanceID())) return;
			var template = __instance.GetComponentInChildren<UIBoolSettingBinding>(true);
			if (template == null) { Plugin.Instance.Log.LogInfo("Mod settings row: no toggle row to copy"); return; }
			var layout = template.transform.parent;
			if (layout == null || layout.Find(RowName) != null) return;
			// Build the copy under an inactive holder so none of its components start (and bind to V-Sync) until it is cleaned up.
			var holder = new GameObject("UwRowHolder");
			holder.SetActive(false);
			var clone = UnityEngine.Object.Instantiate(template.gameObject, holder.transform);
			clone.name = RowName;
			var binding = clone.GetComponent<UIBoolSettingBinding>();
			if (binding != null) UnityEngine.Object.DestroyImmediate(binding);
			var label = clone.transform.Find("Background/Category_Text");
			if (label != null)
			{
				var localized = label.GetComponent<UILocalizedString>();
				if (localized != null) UnityEngine.Object.DestroyImmediate(localized);
				var text = label.GetComponent<TMPro.TextMeshProUGUI>();
				if (text != null) text.text = "Mod settings";
			}
			var check = clone.transform.Find("Background/Checkbox");
			if (check != null) check.gameObject.SetActive(false);
			clone.transform.SetParent(layout, false);
			UnityEngine.Object.Destroy(holder);
			var toggle = clone.GetComponent<UIToggle>();
			toggle.OnValueChangedCallback.AddListener((UnityAction<bool>)(Action<bool>)(on =>
			{
				if (!on) return;
				toggle.UpdateValueFromGroup(false, false, false);
				ModMenu.Open(clone);
			}));
			var nav = __instance.navigationGroup;
			if (nav != null && nav.selectables != null)
			{
				var old = nav.selectables;
				var grown = new Il2CppReferenceArray<UISelectable>(old.Length + 1);
				for (int i = 0; i < old.Length; i++) grown[i] = old[i];
				grown[old.Length] = toggle;
				nav.selectables = grown;
				nav.UpdateNavigation();
			}
			var rect = layout.GetComponent<RectTransform>();
			if (rect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
			Plugin.Instance.Log.LogInfo("Mod settings row added to the Graphics tab");
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("Mod settings row failed: " + ex.Message); }
	}
}

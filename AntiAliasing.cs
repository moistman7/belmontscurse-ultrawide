using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace BelmontsCurseUltrawide;

// The game's anti-aliasing enum already contains TAA, but its menu setting may not offer it.
// Make the game's own setting list it before the Video menu is built; the game applies the value itself.
internal static class AntiAliasing
{
	private const int TaaValue = 3;
	private static Harmony _harmony;
	private static string _borrowedLabel;

	internal static void Init()
	{
		try
		{
			_harmony = new Harmony("local.belmontscurse.ultrawide.aa");
			_harmony.Patch(AccessTools.Method(typeof(VideoSettingsView), "ConstructUI"),
				new HarmonyMethod(typeof(AntiAliasing).GetMethod(nameof(BeforeConstruct), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)),
				new HarmonyMethod(typeof(AntiAliasing).GetMethod(nameof(AfterConstruct), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
		}
		catch (Exception ex)
		{
			_harmony?.UnpatchSelf();
			Plugin.Instance.Log.LogWarning("TAA menu option unavailable: " + ex.Message);
		}
	}

	private static void BeforeConstruct(VideoSettingsView __instance)
	{
		try
		{
			// The view's own reference is still empty when it builds, so look the settings object up in the scene.
			var video = __instance._settings;
			if (video == null) foreach (var v in UnityEngine.Object.FindObjectsByType<VideoSettings>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None)) { video = v; break; }
			var setting = video?._antialiasing;
			var all = new StringBuilder();
			// Fallback: find the setting by id in the settings list if the direct field is empty.
			if (video != null && video._enumSettings != null)
				foreach (var s in video._enumSettings)
				{
					if (s == null) continue;
					all.Append(" [" + s.id + " options=" + (s._options != null ? s._options.Length : -1) + "]");
					if (setting == null && s.id != null && s.id.ToLowerInvariant().Contains("alias")) setting = s;
				}
			if (setting == null || setting._options == null)
			{
				Plugin.Instance.Log.LogInfo("AA menu: no antialiasing setting found (settings=" + (video != null) + ", _antialiasing=" + (video?._antialiasing != null) + ", enum settings:" + all + ")");
				return;
			}
			var options = setting._options;
			var sb = new StringBuilder("AA menu options before:");
			foreach (var o in options) sb.Append(" [" + o.id + " value=" + o.value + " supported=" + o.supported + " name=" + Safe(o) + "]");
			Plugin.Instance.Log.LogInfo(sb.ToString());
			if (!Plugin.EnableTaaOption.Value) return;
			EnumSetting.EnumSettingOption taa = null;
			foreach (var o in options) if (o.value == TaaValue) taa = o;
			if (taa != null) { taa.supported = true; return; }
			var template = options[options.Length - 1];
			var added = new EnumSetting.EnumSettingOption { id = "TAA", value = TaaValue, data = template.data, supported = true, name = template.name };
			var grown = new Il2CppReferenceArray<EnumSetting.EnumSettingOption>(options.Length + 1);
			for (int i = 0; i < options.Length; i++) grown[i] = options[i];
			grown[options.Length] = added;
			setting._options = grown;
			_borrowedLabel = Safe(template);
			Plugin.Instance.Log.LogInfo("AA menu: added TAA option (label copied from '" + Safe(template) + "' until a TAA string is found)");
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("TAA menu option failed: " + ex.Message); }
	}

	// The added option borrows another option's text; once the menu is built, relabel that last row "TAA".
	private static void AfterConstruct(VideoSettingsView __instance)
	{
		try
		{
			if (string.IsNullOrEmpty(_borrowedLabel)) return;
			TMPro.TMP_Text last = null;
			int matches = 0;
			foreach (var text in __instance.GetComponentsInChildren<TMPro.TMP_Text>(true))
				if (text != null && text.text == _borrowedLabel) { last = text; matches++; }
			if (matches >= 2 && last != null) { last.text = "TAA"; Plugin.Instance.Log.LogInfo("AA menu: relabeled the added option 'TAA'"); }
			else Plugin.Instance.Log.LogInfo("AA menu: could not find the added option's label to rename (" + matches + " matches for '" + _borrowedLabel + "')");
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("TAA label: " + ex.Message); }
	}

	private static string Safe(EnumSetting.EnumSettingOption o)
	{
		try { return o.name?.GetLocalizedString(); } catch { return "?"; }
	}
}

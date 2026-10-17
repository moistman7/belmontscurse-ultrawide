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

	internal static void Init()
	{
		try
		{
			_harmony = new Harmony("local.belmontscurse.ultrawide.aa");
			_harmony.Patch(AccessTools.Method(typeof(VideoSettingsView), "ConstructUI"),
				new HarmonyMethod(typeof(AntiAliasing).GetMethod(nameof(BeforeConstruct), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
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
			var setting = __instance._settings?._antialiasing;
			if (setting == null || setting._options == null) { Plugin.Instance.Log.LogInfo("AA menu: no antialiasing setting found"); return; }
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
			Plugin.Instance.Log.LogInfo("AA menu: added TAA option (label copied from '" + Safe(template) + "' until a TAA string is found)");
		}
		catch (Exception ex) { Plugin.Instance.Log.LogWarning("TAA menu option failed: " + ex.Message); }
	}

	private static string Safe(EnumSetting.EnumSettingOption o)
	{
		try { return o.name?.GetLocalizedString(); } catch { return "?"; }
	}
}

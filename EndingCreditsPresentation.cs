using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BelmontsCurseUltrawide;

// CreditsViewManager belongs to the standalone credits flow and remains alive on
// its final Continue screen. Camera clear colors do not reliably cover that UI.
internal static class EndingCreditsPresentation
{
    private static readonly List<CreditsViewManager> Views = new();
    private static readonly ScreenSideBars Bars = new("UwEndingCreditsBars");
    internal static bool Hooked { get; private set; }

    internal static void Init()
    {
        try
        {
            new Harmony("local.belmontscurse.ultrawide.credits").Patch(
                AccessTools.Method(typeof(CreditsViewManager), "Start"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(EndingCreditsPresentation), nameof(Started))));
            Hooked = true;
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.LogWarning("Credits presentation hook unavailable; polling remains active: " + ex.Message);
        }
    }

    private static void Started(CreditsViewManager __instance)
    {
        try
        {
            Track(__instance);
            Apply();
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.LogWarning("Ending credits presentation: " + ex.Message);
        }
    }

    private static void Track(CreditsViewManager view)
    {
        if (view == null) return;
        foreach (var existing in Views)
            if (existing != null && existing.Pointer == view.Pointer) return;
        Views.Add(view);
    }

    internal static void Scan()
    {
        for (int i = Views.Count - 1; i >= 0; i--)
            if (Views[i] == null) Views.RemoveAt(i);
        foreach (var view in Object.FindObjectsByType<CreditsViewManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Track(view);
        Apply();
    }

    internal static void Apply()
    {
        bool active = false;
        if (Plugin.On)
            foreach (var view in Views)
                if (view != null && view.isActiveAndEnabled) { active = true; break; }
        float width = active ? Logic.PresentationWidth(Screen.width, Screen.height) : 1f;
        Bars.Set(active && width < 1f, width);
    }

    internal static void Dump(StringBuilder output)
    {
        output.AppendLine("Ending-credits pure-black bars=" + Bars.Visible + " views=" + Views.Count);
        foreach (var view in Views)
            if (view != null)
                output.AppendLine("  credits view '" + view.name + "' active=" + view.isActiveAndEnabled
                    + " scene=" + view.gameObject.scene.name);
    }
}

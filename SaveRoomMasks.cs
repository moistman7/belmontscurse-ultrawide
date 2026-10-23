#nullable disable
using System.Collections.Generic;
using System.Diagnostics;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BelmontsCurseUltrawide;

internal static class SaveRoomMasks
{
    private static readonly List<MeshRenderer> Masks = new();
    private static readonly List<MeshRenderer> Hidden = new();
    private static GameObject StartMenu;
    private static Il2CppArrayBase<MeshRenderer> ScanRenderers;
    private static readonly List<MeshRenderer> NextMasks = new();
    private static int ScanIndex;
    private static bool RescanRequested;
    private static double SnapshotMaxMs, StepMaxMs;

    internal static void Dump(System.Text.StringBuilder output)
    {
        int found = 0, hidden = 0;
        foreach (var renderer in Masks) if (renderer != null) found++;
        foreach (var renderer in Hidden) if (renderer != null && !renderer.enabled) hidden++;
        output.AppendLine("Save-room side masks: found=" + found + " hidden=" + hidden
            + " mainMenu=" + (StartMenu != null && StartMenu.activeInHierarchy));
        output.AppendLine("  discovery pending=" + (ScanRenderers != null) + " rescanRequested=" + RescanRequested + " index=" + ScanIndex
            + " total=" + (ScanRenderers == null ? 0 : ScanRenderers.Length)
            + " snapshotMaxMs=" + SnapshotMaxMs.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
            + " stepMaxMs=" + StepMaxMs.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
        foreach (var renderer in Masks)
            if (renderer != null)
                output.AppendLine("  save-room mask active=" + renderer.gameObject.activeInHierarchy
                    + " enabled=" + renderer.enabled + " bounds=" + renderer.bounds);
    }

    internal static void Scan()
    {
        // Finish the current generation, then honor a refresh on a later frame.
        // Restarting here could starve a traversal; discarding it could miss a new scene.
        if (ScanRenderers != null) { RescanRequested = true; return; }
        RescanRequested = false;
        long started = Stopwatch.GetTimestamp();
        // The title screen can reuse save-room scenery. Leave its presentation intact.
        StartMenu = GameObject.Find("MainMenu/StartMenuView");
        NextMasks.Clear();
        ScanIndex = 0;
        ScanRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SnapshotMaxMs = System.Math.Max(SnapshotMaxMs, (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency);
    }

    internal static void ScanStep()
    {
        if (ScanRenderers == null)
        {
            if (RescanRequested) Scan();
            return;
        }
        long started = Stopwatch.GetTimestamp();
        int first = ScanIndex;
        int end = System.Math.Min(ScanRenderers.Length, ScanIndex + 128);
        // Unity object discovery itself remains synchronous. Bound the subsequent
        // native name/parent/material reads; never move Unity access to a worker.
        while (ScanIndex < end)
        {
            // Always make progress even if the thread is descheduled just after
            // taking the start timestamp. A single native call can exceed the budget.
            if (ScanIndex > first && (ScanIndex & 15) == 0 && (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency >= 0.25) break;
            var renderer = ScanRenderers[ScanIndex++];
            // These are opaque side panels, separate from the decorative columns/statues.
            if (renderer == null || renderer.gameObject.name != "00_Foreground_BlackSquare") continue;
            var parent = renderer.transform.parent;
            if (parent == null || parent.name != "Foreground_Saveroom_Black") continue;
            var material = renderer.sharedMaterial;
            if (material == null || !material.name.StartsWith("00_BlackUnlit_Foreground", System.StringComparison.Ordinal)) continue;
            NextMasks.Add(renderer);
        }
        if (ScanIndex >= ScanRenderers.Length)
        {
            // Publish a complete generation atomically; existing masks keep being
            // enforced during discovery. Hidden retains ownership for restoration.
            Masks.Clear();
            Masks.AddRange(NextMasks);
            NextMasks.Clear();
            ScanRenderers = null;
            Apply();
        }
        StepMaxMs = System.Math.Max(StepMaxMs, (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency);
    }

    internal static void Apply()
    {
        bool hide = Plugin.On && (StartMenu == null || !StartMenu.activeInHierarchy)
            && Screen.height > 0 && (float)Screen.width / Screen.height > 1.7877778f;
        for (int i = Hidden.Count - 1; i >= 0; i--)
        {
            var renderer = Hidden[i];
            if (renderer == null)
            {
                Hidden.RemoveAt(i);
                continue;
            }
            if (hide)
            {
                if (renderer.enabled) renderer.enabled = false;
            }
            else
            {
                renderer.enabled = true;
                Hidden.RemoveAt(i);
            }
        }
        if (!hide) return;
        foreach (var renderer in Masks)
        {
            // Only restore renderers that this feature actually disabled.
            if (renderer == null || !renderer.enabled) continue;
            renderer.enabled = false;
            Hidden.Add(renderer);
            Plugin.Instance.Log.LogInfo("Hid save-room side mask '" + renderer.gameObject.name + "' under '" + renderer.transform.parent.name + "'.");
        }
    }
}

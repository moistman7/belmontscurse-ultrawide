using System.Collections.Generic;
using System.Text;
using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BelmontsCurseUltrawide;

// Preserve a 16:9 presentation for loading artwork. The post-ending report still needs scene verification.
// Keep its camera separate from the gameplay viewport expansion and cinematic-bar removal.
internal static class LoadingScreenPresentation
{
    private sealed class Entry
    {
        internal Camera Camera;
        internal Rect OriginalRect;
        internal bool Applied;
    }

    private static readonly List<Entry> Cameras = new();
    private static readonly ScreenSideBars Bars = new("UwLoadingScreenBars");
    private static Harmony Hooks;
    internal static bool Hooked { get; private set; }

    internal static void Init()
    {
        try
        {
            Hooks = new Harmony("local.belmontscurse.ultrawide.loading");
            var postfix = new HarmonyMethod(AccessTools.Method(typeof(LoadingScreenPresentation), nameof(CameraChanged)));
            Hooks.Patch(AccessTools.Method(typeof(LoadingScreenCameraManager), "EnableLoadingScreenCamera"), postfix: postfix);
            Hooks.Patch(AccessTools.Method(typeof(LoadingScreenCameraManager), "DisableLoadingScreenCamera"), postfix: postfix);
            Hooked = true;
        }
        catch (Exception ex)
        {
            Hooks?.UnpatchSelf();
            Plugin.Instance.Log.LogWarning("Loading camera hooks unavailable; polling remains active: " + ex.Message);
        }
    }

    private static void CameraChanged(LoadingScreenCameraManager __instance)
    {
        try
        {
            Track(__instance);
            Apply();
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.LogWarning("Loading camera presentation: " + ex.Message);
        }
    }

    private static void Track(LoadingScreenCameraManager manager)
    {
        if (manager == null || manager.loadingScreenCamera == null) return;
        var camera = manager.loadingScreenCamera;
        foreach (var entry in Cameras)
            if (entry.Camera != null && entry.Camera.Pointer == camera.Pointer) return;
        Cameras.Add(new Entry { Camera = camera });
    }

    internal static void Scan()
    {
        // Preserve ownership across scans so disabling the feature can restore the original rect.
        for (int i = Cameras.Count - 1; i >= 0; i--)
            if (Cameras[i].Camera == null) Cameras.RemoveAt(i);
        foreach (var manager in Object.FindObjectsByType<LoadingScreenCameraManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Track(manager);
        Apply();
    }

    // MiscJob must not undo the viewport, including while the loading camera is inactive.
    internal static bool Owns(Camera camera)
    {
        if (!Plugin.On || camera == null) return false;
        foreach (var entry in Cameras)
            if (entry.Camera != null && entry.Camera.Pointer == camera.Pointer) return true;
        return false;
    }

    internal static void Apply()
    {
        // No scene cameras have been discovered: avoid native Screen reads in the per-frame path.
        if (Cameras.Count == 0) { Bars.Set(false); return; }
        float width = 1f;
        bool widthRead = false;
        bool show = false;
        foreach (var entry in Cameras)
        {
            var camera = entry.Camera;
            if (camera == null) continue;
            // Inactive loading cameras are the normal gameplay case. Test that before querying the render target.
            bool active = Plugin.On && camera.isActiveAndEnabled;
            if (active && !widthRead)
            {
                width = Logic.PresentationWidth(Screen.width, Screen.height);
                widthRead = true;
            }
            bool apply = active && width < 1f && camera.targetTexture == null;
            if (apply)
            {
                if (!entry.Applied)
                {
                    entry.OriginalRect = camera.rect;
                    entry.Applied = true;
                }
                var viewport = new Rect((1f - width) * 0.5f, 0f, width, 1f);
                if (camera.rect != viewport) camera.rect = viewport;
                show = true;
            }
            else if (entry.Applied)
            {
                camera.rect = entry.OriginalRect;
                entry.Applied = false;
            }
        }
        Bars.Set(show, width);
    }

    internal static void Dump(StringBuilder output)
    {
        output.AppendLine("Loading-screen pure-black bars=" + Bars.Visible + " cameras=" + Cameras.Count);
        foreach (var entry in Cameras)
            if (entry.Camera != null)
                output.AppendLine("  loading camera '" + entry.Camera.name + "' active=" + entry.Camera.isActiveAndEnabled
                    + " rect=" + entry.Camera.rect + " owned=" + entry.Applied + " original=" + entry.OriginalRect
                    + " targetTexture=" + (entry.Camera.targetTexture != null));
        foreach (var loader in Object.FindObjectsByType<LoadingScreenImageLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (loader == null) continue;
            var image = loader.target;
            output.AppendLine("  loading artwork '" + loader.name + "' active=" + loader.isActiveAndEnabled
                + " target=" + (image == null ? "none" : image.name + " active=" + image.isActiveAndEnabled
                    + " size=" + image.rectTransform.rect.size + " canvas=" + (image.canvas == null ? "none" : image.canvas.name)));
        }
    }
}

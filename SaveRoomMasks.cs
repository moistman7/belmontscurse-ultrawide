using System.Collections.Generic;
using UnityEngine;

namespace BelmontsCurseUltrawide;

internal static class SaveRoomMasks
{
    private static readonly List<MeshRenderer> Masks = new();
    private static readonly List<MeshRenderer> Hidden = new();
    private static GameObject StartMenu;

    internal static void Dump(System.Text.StringBuilder output)
    {
        int found = 0, hidden = 0;
        foreach (var renderer in Masks) if (renderer != null) found++;
        foreach (var renderer in Hidden) if (renderer != null && !renderer.enabled) hidden++;
        output.AppendLine("Save-room side masks: found=" + found + " hidden=" + hidden
            + " mainMenu=" + (StartMenu != null && StartMenu.activeInHierarchy));
    }

    internal static void Scan()
    {
        // The title screen can reuse save-room scenery. Leave its presentation intact.
        StartMenu = GameObject.Find("MainMenu/StartMenuView");
        Masks.Clear();
        foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // These are opaque side panels, separate from the decorative columns/statues.
            if (renderer == null || renderer.gameObject.name != "00_Foreground_BlackSquare") continue;
            var parent = renderer.transform.parent;
            if (parent == null || parent.name != "Foreground_Saveroom_Black") continue;
            var material = renderer.sharedMaterial;
            if (material == null || !material.name.StartsWith("00_BlackUnlit_Foreground", System.StringComparison.Ordinal)) continue;
            Masks.Add(renderer);
        }
        Apply();
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

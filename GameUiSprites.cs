using System;
using System.Collections.Generic;
using UnityEngine;

namespace BelmontsCurseUltrawide;

// Derive display bounds from asset data only. Never issue graphics commands from UI construction.
internal static class GameUiSprites
{
    private sealed class SpriteAsset
    {
        internal Sprite Source;
        internal Sprite Display;
    }
    private static readonly Dictionary<int, SpriteAsset> Cache = new();
    // The game unloads portrait art after dialogue ends, so history keeps its own copy of what it displays.
    private static readonly Dictionary<int, Sprite> Kept = new();
    internal static Sprite Keep(Sprite source)
    {
        if (source == null) return null;
        int id = source.GetInstanceID();
        if (Kept.TryGetValue(id, out var existing) && existing != null) return existing;
        var previous = RenderTexture.active;
        RenderTexture target = null;
        try
        {
            if (source.packed && source.packingRotation != SpritePackingRotation.None) return source;
            var texture = source.texture;
            Rect area = source.textureRect;
            float scale = Math.Min(1f, 512f / Math.Max(area.width, area.height));
            int w = Math.Max(1, (int)(area.width * scale)), h = Math.Max(1, (int)(area.height * scale));
            target = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            Graphics.Blit(texture, target, new Vector2(area.width / texture.width, area.height / texture.height), new Vector2(area.x / texture.width, area.y / texture.height));
            RenderTexture.active = target;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "UwKept_" + source.name };
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            copy.Apply(false, false);
            copy.hideFlags = HideFlags.HideAndDontSave;
            var kept = Sprite.Create(copy, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), source.pixelsPerUnit * scale);
            kept.name = "UwKept_" + source.name;
            kept.hideFlags = HideFlags.HideAndDontSave;
            Kept[id] = kept;
            return kept;
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.LogWarning("History art copy failed, using the game's sprite: " + ex.Message);
            return source;
        }
        finally
        {
            RenderTexture.active = previous;
            if (target != null) RenderTexture.ReleaseTemporary(target);
        }
    }
    // Free a copy made by Keep (and its trimmed display sprite) once no history entry uses it any more.
    // Only copies this class made are destroyed; a fallback that returned the game's own sprite is left alone.
    internal static void Release(Sprite kept)
    {
        if (kept == null) return;
        int key = 0;
        bool ours = false;
        foreach (var pair in Kept)
            if (pair.Value == kept) { key = pair.Key; ours = true; break; }
        if (!ours) return;
        Kept.Remove(key);
        int id = kept.GetInstanceID();
        if (Cache.TryGetValue(id, out var cached))
        {
            Cache.Remove(id);
            if (cached.Display != null && cached.Display != cached.Source) UnityEngine.Object.Destroy(cached.Display);
        }
        var texture = kept.texture;
        UnityEngine.Object.Destroy(kept);
        if (texture != null) UnityEngine.Object.Destroy(texture);
    }

    internal static int KeptCount => Kept.Count;

    internal static Sprite Trim(Sprite source)
    {
        if (source == null) return null;
        int id = source.GetInstanceID();
        if (Cache.TryGetValue(id, out var cached) && cached.Source == source && cached.Display != null)
            return cached.Display;
        Sprite display = source;
        try
        {
            if (!source.packed || source.packingRotation == SpritePackingRotation.None)
            {
                var texture = source.texture;
                Rect area = source.textureRect;
                float left = area.xMax, right = area.xMin, bottom = area.yMax, top = area.yMin;
                // Tight meshes already describe visible bounds, including non-readable GPU textures.
                foreach (var uv in source.uv)
                {
                    left = Math.Min(left, uv.x * texture.width);
                    right = Math.Max(right, uv.x * texture.width);
                    bottom = Math.Min(bottom, uv.y * texture.height);
                    top = Math.Max(top, uv.y * texture.height);
                }
                int step = 1;
                if (texture.isReadable)
                {
                    var pixels = texture.GetPixels32();
                    int x0 = (int)area.xMin, x1 = (int)area.xMax;
                    int y0 = (int)area.yMin, y1 = (int)area.yMax;
                    step = Math.Max(1, Math.Max(x1 - x0, y1 - y0) / 192);
                    left = x1; right = x0; bottom = y1; top = y0;
                    for (int y = y0; y < y1; y += step)
                        for (int x = x0; x < x1; x += step)
                            if (pixels[y * texture.width + x].a > 12)
                            {
                                left = Math.Min(left, x); right = Math.Max(right, x + 1);
                                bottom = Math.Min(bottom, y); top = Math.Max(top, y + 1);
                            }
                }
                if (right > left && top > bottom)
                {
                    left = Math.Max(area.xMin, left - 2 - step); right = Math.Min(area.xMax, right + 2 + step);
                    bottom = Math.Max(area.yMin, bottom - 2 - step); top = Math.Min(area.yMax, top + 2 + step);
                    var crop = new Rect(left, bottom, right - left, top - bottom);
                    display = Sprite.Create(texture, crop, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
                    display.name = "UwUiSprite_" + source.name;
                }
            }
        }
        catch (Exception ex) { Plugin.Instance.Log.LogWarning("UI sprite alignment fallback: " + ex.Message); }
        if (cached != null && cached.Display != null && cached.Display != cached.Source)
            UnityEngine.Object.Destroy(cached.Display);
        Cache[id] = new SpriteAsset { Source = source, Display = display };
        return display;
    }
}

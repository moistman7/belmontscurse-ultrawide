using UnityEngine;
using UnityEngine.UI;

namespace BelmontsCurseUltrawide;

internal static class MainMenuBars
{
    private static GameObject Root;
    private static RectTransform Left, Right;
    private static float LastWidth = -1f;

    internal static bool Visible => Root != null && Root.activeSelf;

    internal static void Set(bool visible, float viewportWidth = 1f)
    {
        if (!visible)
        {
            if (Root != null && Root.activeSelf) Root.SetActive(false);
            return;
        }
        if (Root == null)
        {
            Root = new GameObject("UwMainMenuBars");
            Object.DontDestroyOnLoad(Root);
            var canvas = Root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the game's tinted overlay, below mod windows and notifications.
            canvas.sortingOrder = short.MaxValue - 2;
            Left = Create("Left");
            Right = Create("Right");
            LastWidth = -1f;
        }
        if (!Root.activeSelf) Root.SetActive(true);
        if (Mathf.Abs(LastWidth - viewportWidth) < 0.00001f) return;
        LastWidth = viewportWidth;
        float edge = (1f - viewportWidth) * 0.5f;
        Left.anchorMin = Vector2.zero;
        Left.anchorMax = new Vector2(edge, 1f);
        Right.anchorMin = new Vector2(1f - edge, 0f);
        Right.anchorMax = Vector2.one;
    }

    private static RectTransform Create(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(Root.transform, false);
        var image = go.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        var rect = image.rectTransform;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }
}

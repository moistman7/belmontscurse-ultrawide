using BelmontsCurseUltrawide;
using UnityEngine;

public class SaveRoomMasksTests
{
    [Fact]
    public void Batched_discovery_preserves_ownership_and_menu_protection()
    {
        MeshRenderer Make(string name = "00_Foreground_BlackSquare", string parent = "Foreground_Saveroom_Black")
            => new() { gameObject = new() { name = name }, transform = new() { parent = new() { name = parent } }, sharedMaterial = new() { name = "00_BlackUnlit_Foreground (Instance)" } };
        var oldMask = Make();
        UnityEngine.Object.Renderers = new[] { oldMask };
        SaveRoomMasks.Scan();
        SaveRoomMasks.ScanStep();
        Assert.False(oldMask.enabled);

        var nextMask = Make();
        var wrongParent = Make(parent: "OtherRoom");
        var wrongName = Make(name: "DecorativeBlackSquare");
        UnityEngine.Object.Renderers = Enumerable.Range(0, 300).Select(_ => Make(name: "Scenery")).Concat(new[] { wrongParent, wrongName, nextMask }).ToArray();
        int finds = UnityEngine.Object.Finds;
        SaveRoomMasks.Scan();
        var refreshedMask = Make();
        UnityEngine.Object.Renderers = new[] { refreshedMask };
        SaveRoomMasks.Scan(); // A newer scene arrives while the old generation is pending.
        for (int frame = 0; frame < 1024 && nextMask.enabled; frame++)
        {
            Assert.False(oldMask.enabled); // A partial generation must not release owned masks.
            int reads = Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<MeshRenderer>.Reads;
            SaveRoomMasks.ScanStep();
            Assert.InRange(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<MeshRenderer>.Reads - reads, 0, 128);
            if (nextMask.enabled) SaveRoomMasks.Scan(); // Refresh cannot restart in-flight traversal.
        }
        Assert.Equal(finds + 1, UnityEngine.Object.Finds);
        Assert.False(nextMask.enabled);
        Assert.True(wrongParent.enabled);
        Assert.True(wrongName.enabled);

        SaveRoomMasks.ScanStep(); // Queued snapshot starts on a separate frame.
        Assert.Equal(finds + 2, UnityEngine.Object.Finds);
        for (int frame = 0; frame < 1024 && refreshedMask.enabled; frame++) SaveRoomMasks.ScanStep();
        Assert.False(refreshedMask.enabled);

        Plugin.On = false;
        SaveRoomMasks.Apply();
        Assert.True(oldMask.enabled);
        Assert.True(nextMask.enabled);
        Assert.True(refreshedMask.enabled);
        Plugin.On = true;
        GameObject.Menu.activeInHierarchy = true;
        SaveRoomMasks.Apply();
        Assert.True(refreshedMask.enabled);
        GameObject.Menu.activeInHierarchy = false;
        SaveRoomMasks.Apply();
        Assert.False(refreshedMask.enabled);
        Assert.True(nextMask.enabled);
        Assert.True(oldMask.enabled); // Removed generation is no longer owned.

        UnityEngine.Object.Renderers = Array.Empty<MeshRenderer>();
        SaveRoomMasks.Scan();
        SaveRoomMasks.ScanStep(); // An empty generation must complete, not leave a scan pending.
        Plugin.On = false;
        SaveRoomMasks.Apply();
        Assert.True(refreshedMask.enabled);
    }
}

// Minimal fake API for exercising the actual discovery/ownership code without the
// game. This does not model native cost, Unity destroyed-object semantics or rendering.
namespace UnityEngine
{
    public class Object
    {
        public static MeshRenderer[] Renderers = Array.Empty<MeshRenderer>();
        public static int Finds;
        public static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<T> FindObjectsByType<T>(FindObjectsInactive _, FindObjectsSortMode __)
        { Finds++; return new(Renderers.Cast<T>().ToArray()); }
    }
    public enum FindObjectsInactive { Include }
    public enum FindObjectsSortMode { None }
    public class GameObject { public string name = ""; public bool activeInHierarchy; public static readonly GameObject Menu = new(); public static GameObject Find(string _) => Menu; }
    public class Transform { public Transform? parent; public string name = ""; }
    public class Material { public string name = ""; }
    public class MeshRenderer { public GameObject gameObject = new(); public Transform transform = new(); public Material sharedMaterial = new(); public bool enabled = true; public string bounds = "fake"; }
    public static class Screen { public static int width = 5120, height = 1440; }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public class Il2CppArrayBase<T>(T[] values)
    {
        public static int Reads;
        public int Length => values.Length;
        public T this[int i] { get { Reads++; return values[i]; } }
    }
}
namespace BelmontsCurseUltrawide
{
    internal static class Plugin
    {
        internal static bool On = true;
        internal static readonly FakeInstance Instance = new();
        internal sealed class FakeInstance { public readonly FakeLog Log = new(); }
        internal sealed class FakeLog { public void LogInfo(string _) { } }
    }
}

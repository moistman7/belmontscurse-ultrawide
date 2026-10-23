using System;
using System.Text;
using Cinemachine;
using HarmonyLib;

namespace BelmontsCurseUltrawide;

// Enforce the logic aspect at the camera solver, independently of MonoBehaviour order.
// A UniversalRenderPipeline.InitializeCameraData patch was tried in 0.8.17/0.8.18 and crashed the
// game at startup (a static method with an out struct parameter); the begin/end camera render
// callbacks in UwBehaviour apply the final projection instead.
internal static class CameraTimingHooks
{
    private static Harmony Hooks;
    private static int SolverHooks;
    private static long SolverRuns;

    internal static void Init()
    {
        Hooks = new Harmony("local.belmontscurse.ultrawide.camera-timing");
        // UpdateVirtualCameras covers physics/SmartUpdate. ProcessActiveCamera also
        // covers transition-time camera-state updates outside that method.
        foreach (string name in new[] { "UpdateVirtualCameras", "ProcessActiveCamera" })
        {
            try
            {
                var target = AccessTools.Method(typeof(CinemachineBrain), name);
                if (target == null) throw new MissingMethodException(name);
                Hooks.Patch(target, prefix: new HarmonyMethod(typeof(CameraTimingHooks), nameof(BeforeSolver)));
                SolverHooks++;
            }
            catch (Exception ex) { Plugin.Instance.Log.LogWarning("Camera timing hook " + name + " unavailable: " + ex.Message); }
        }
        Plugin.Instance.Log.LogInfo("Camera timing: solver hooks=" + SolverHooks + "/2");
    }

    private static void BeforeSolver(CinemachineBrain __instance)
    {
        SolverRuns++;
        UwBehaviour.BeforeCameraSolve(__instance);
    }

    internal static void Dump(StringBuilder output)
        => output.AppendLine("Camera timing hooks: solver=" + SolverHooks + "/2 runs=" + SolverRuns);
}

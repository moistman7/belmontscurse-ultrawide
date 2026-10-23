using System;
using System.Text;
using Cinemachine;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BelmontsCurseUltrawide;

// Enforce the camera invariants at their consumers, independently of MonoBehaviour order.
internal static class CameraTimingHooks
{
    private static Harmony Hooks;
    internal static bool RenderReady { get; private set; }
    private static int SolverHooks;
    private static long SolverRuns, RenderRuns;

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
        try
        {
            var target = AccessTools.Method(typeof(UniversalRenderPipeline), "InitializeCameraData");
            if (target == null) throw new MissingMethodException("InitializeCameraData");
            Hooks.Patch(target, prefix: new HarmonyMethod(typeof(CameraTimingHooks), nameof(BeforeCameraData)));
            RenderReady = true;
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.LogWarning("Final camera-data hook unavailable; render callback fallback remains: " + ex.Message);
        }
        Plugin.Instance.Log.LogInfo("Camera timing candidate: solver hooks=" + SolverHooks + "/2 final render hook=" + RenderReady);
    }

    private static void BeforeSolver(CinemachineBrain __instance)
    {
        SolverRuns++;
        UwBehaviour.BeforeCameraSolve(__instance);
    }

    private static void BeforeCameraData(Camera camera)
    {
        RenderRuns++;
        UwBehaviour.BeforeCameraData(camera);
    }

    internal static void Dump(StringBuilder output)
        => output.AppendLine("Camera timing hooks: solver=" + SolverHooks + "/2 runs=" + SolverRuns
            + " finalRender=" + RenderReady + " runs=" + RenderRuns);
}

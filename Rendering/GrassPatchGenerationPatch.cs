using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Landoria.SagaCapture
{
    // Keeps vegetation patches around the second camera as well as the player.
    [HarmonyPatch(typeof(ClutterSystem), "LateUpdate")]
    internal static class GrassPatchGenerationPatch
    {
        private static readonly MethodInfo GeneratePatches = AccessTools.Method(
            typeof(ClutterSystem), "GeneratePatches");

        // Shares the game's existing patch generator and one-patch-per-frame limit.
        private static void Postfix(ClutterSystem __instance)
        {
            Camera capture = GrassDistanceController.CaptureCamera;
            if (capture == null ||
                !RenderGroupSystem.IsGroupActive(RenderGroup.Overworld) ||
                GraphicsSettingsManager.Instance.ActiveSettings.m_vegetation ==
                ClutterSystem.Quality.Off)
            {
                return;
            }
            GeneratePatches.Invoke(__instance,
                new object[] { false, capture.transform.position });
        }
    }
}

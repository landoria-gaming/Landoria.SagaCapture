using System;
using System.Reflection;
using HarmonyLib;

namespace Landoria.SagaCapture
{
    // Observes the recorder delegate without changing audio samples or queue behavior.
    [HarmonyPatch]
    internal static class SagaCaptureAudioDiagnosticsPatch
    {
        // Locates the internal CineCapture method in the merged assembly.
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                "UnityRuntimeCameraRecorder.UnityAudioCapture:Initialize");
        }

        // Wraps the recorder's audio writer before the audio callback uses it.
        private static void Prefix(object __instance, ref Func<byte[], bool> writeAudio)
        {
            SagaCaptureAudioDiagnostics.Wrap(__instance, ref writeAudio);
        }
    }
}

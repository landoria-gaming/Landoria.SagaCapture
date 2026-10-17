using UnityEngine;

namespace Landoria.SagaCapture
{
    // Applies the optional grass distance while preserving the game's original value.
    internal static class GrassDistanceController
    {
        private static ClutterSystem system;
        private static float vanillaDistance;
        internal static Camera CaptureCamera { get; set; }

        // Updates the grass distance after configuration changes or world loads.
        internal static void Update()
        {
            ClutterSystem current = ClutterSystem.instance;
            if (current == null)
            {
                system = null;
                return;
            }

            if (current != system)
            {
                system = current;
                vanillaDistance = current.m_distance;
            }

            float configured = Preference.GrassDistance;
            float desired = configured == 0f ? vanillaDistance : configured;
            if (Mathf.Approximately(current.m_distance, desired))
            {
                return;
            }

            current.m_distance = desired;
            current.ClearAll();
            SagaCapturePlugin.Log.LogInfo(
                $"Grass distance set to {desired:0.##} m.");
        }

        // Restores the original distance when the plugin unloads.
        internal static void Dispose()
        {
            if (system != null &&
                !Mathf.Approximately(system.m_distance, vanillaDistance))
            {
                system.m_distance = vanillaDistance;
                system.ClearAll();
            }

            system = null;
            CaptureCamera = null;
        }
    }
}

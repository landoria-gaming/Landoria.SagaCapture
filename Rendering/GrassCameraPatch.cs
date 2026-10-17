using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Landoria.SagaCapture
{
    // Adds grass visible only from the recording camera without changing gameplay.
    [HarmonyPatch(typeof(InstanceRenderer), nameof(InstanceRenderer.CustomUpdate))]
    internal static class GrassCameraPatch
    {
        private static readonly FieldInfo BoundsField = AccessTools.Field(
            typeof(InstanceRenderer), "m_bounds");
        private static readonly FieldInfo CountField = AccessTools.Field(
            typeof(InstanceRenderer), "m_instanceCount");
        private static readonly FieldInfo MatricesField = AccessTools.Field(
            typeof(InstanceRenderer), "m_instances");
        private static Plane[] _captureFrustum;
        private static int _frustumFrame = -1;

        // Draws an extra batch only when the gameplay camera skipped the grass.
        private static void Postfix(InstanceRenderer __instance)
        {
            Camera capture = GrassDistanceController.CaptureCamera;
            ClutterSystem clutter = ClutterSystem.instance;
            if (capture == null || !capture.enabled || clutter == null ||
                !__instance.transform.IsChildOf(clutter.transform))
            {
                return;
            }
            int total = (int)CountField.GetValue(__instance);
            if (total == 0 || __instance.m_mesh == null ||
                __instance.m_material == null)
            {
                return;
            }
            BoundingSphere bounds = (BoundingSphere)BoundsField.GetValue(__instance);
            if (GameplayDrawsGrass(__instance, bounds) ||
                __instance.m_frustumCull && !CaptureSeesGrass(capture, bounds))
            {
                return;
            }
            DrawForCapture(__instance, capture, total);
        }

        // Matches the vanilla frustum and distance checks for the main view.
        private static bool GameplayDrawsGrass(
            InstanceRenderer renderer, BoundingSphere bounds)
        {
            Camera main = Utils.GetMainCamera();
            if (main == null || renderer.m_frustumCull &&
                !Utils.InsideMainCamera(bounds))
            {
                return false;
            }
            return !renderer.m_useLod ||
                Distance(renderer, main) < renderer.m_lodMaxDistance;
        }

        // Uses the recording camera's frustum, cached once per frame.
        private static bool CaptureSeesGrass(
            Camera capture, BoundingSphere bounds)
        {
            if (_frustumFrame != Time.frameCount)
            {
                _captureFrustum = GeometryUtility.CalculateFrustumPlanes(capture);
                _frustumFrame = Time.frameCount;
            }
            var box = new Bounds(bounds.position,
                Vector3.one * (bounds.radius * 2f));
            return GeometryUtility.TestPlanesAABB(_captureFrustum, box);
        }

        // Schedules the missing instances for this camera only.
        private static void DrawForCapture(
            InstanceRenderer renderer, Camera capture, int total)
        {
            int count = total;
            if (renderer.m_useLod)
            {
                float distance = Distance(renderer, capture);
                count = Mathf.Clamp(Mathf.RoundToInt((1f - Utils.LerpStep(
                    renderer.m_lodMinDistance, renderer.m_lodMaxDistance,
                    distance)) * total), 0, total);
            }
            if (count == 0)
            {
                return;
            }
            var matrices = (Matrix4x4[])MatricesField.GetValue(renderer);
            Graphics.DrawMeshInstanced(renderer.m_mesh, 0,
                renderer.m_material, matrices, count, null,
                renderer.m_shadowCasting, true,
                LayerMask.NameToLayer("InstanceRenderer"), capture);
        }

        private static float Distance(InstanceRenderer renderer, Camera camera)
        {
            return renderer.m_useXZLodDistance
                ? Utils.DistanceXZ(camera.transform.position,
                    renderer.transform.position)
                : Vector3.Distance(camera.transform.position,
                    renderer.transform.position);
        }
    }
}

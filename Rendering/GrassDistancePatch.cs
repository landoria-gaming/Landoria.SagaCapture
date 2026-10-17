using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Landoria.SagaCapture
{
    // Extends the visibility of newly generated grass when distance is overridden.
    [HarmonyPatch(typeof(ClutterSystem), "GenerateVegPatch")]
    internal static class GrassDistancePatch
    {
        private static readonly FieldInfo ObjectsField = AccessTools.Field(
            typeof(ClutterSystem).GetNestedType(
                "PatchData", BindingFlags.NonPublic), "m_objects");

        // Raises the instance fade limit to match the configured patch radius.
        private static void Postfix(object __result, ClutterSystem __instance)
        {
            if (__result == null || Preference.GrassDistance == 0f)
            {
                return;
            }

            float limit = Mathf.Max(
                0f, __instance.m_distance - __instance.m_grassPatchSize / 2f);
            var objects = (List<GameObject>)ObjectsField.GetValue(__result);
            foreach (GameObject grassObject in objects)
            {
                InstanceRenderer renderer =
                    grassObject.GetComponent<InstanceRenderer>();
                if (renderer != null)
                {
                    renderer.m_lodMaxDistance = limit;
                }
            }
        }
    }
}

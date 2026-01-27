
using HarmonyLib;

using static NACopsV1.NACops;

#if MONO
using ScheduleOne.Building.Doors;
using ScheduleOne.Doors;
#else
using Il2CppScheduleOne.Building.Doors;
using Il2CppScheduleOne.Doors;
#endif

namespace NACopsV1
{
    [HarmonyPatch(typeof(PropertyDoorController), "CanPlayerAccess")]
    public static class PropertyDoorController_CanPlayerAccess_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(PropertyDoorController __instance, ref EDoorSide side, ref bool __result, ref string reason)
        {
            // So if player has police nearby patch it to be openable only when officers can do it too
            if (reason == null) return;
            if (!registered) return;
            if (officerConfig == null) return;
            if (!officerConfig.CanEnterBuildings) return;
            if (reason == "Police are nearby!")
                __result = true;
            return;
        }

    }

}
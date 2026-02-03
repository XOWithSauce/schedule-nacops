



using HarmonyLib;

using static NACopsV1.NACops;
using static NACopsV1.PrivateInvestigator;

#if MONO
using ScheduleOne.Police;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.AvatarFramework.Equipping;
#else
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.AvatarFramework.Equipping;
#endif

namespace NACopsV1
{
    // To make investigators random name visible
    [HarmonyPatch(typeof(PoliceOfficer), "GetNameAddress")]
    public static class PoliceOfficer_GetNameAddress_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(PoliceOfficer __instance, ref string __result)
        {
            // IF the officer is a private investigator
            if (!currentConfig.PrivateInvestigator) return;
            if (investigatorObjectIDs.Count > 0 && investigatorObjectIDs.Contains(__instance.transform.root.gameObject.GetInstanceID()))
                __result = __instance.FirstName;
            return;
        }

    }

    // To make invesitgator not throw null reference excpetion from missing avatar belt
    [HarmonyPatch(typeof(PursuitBehaviour), "OnCurrentWeaponChanged")]
    public static class PursuitBehaviour_OnCurrentWeaponChanged_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(PursuitBehaviour __instance, AvatarWeapon weapon)
        {
            // IF the officer is a private investigator
            if (!currentConfig.PrivateInvestigator) return true;
            if (__instance.officer.belt == null)
                return false;
            return true;
        }

    }

}
using HarmonyLib;
using static NACops.PrivateInvestigator;
using static NACops.RaidPropertyEvent;
using static NACops.CopInitHelper;

#if MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vision;
using ScheduleOne.Law;
using ScheduleOne.DevUtilities;
#else
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vision;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.DevUtilities;
#endif

using static NACops.NACops;

namespace NACops
{
    // Fixes a bug where during curfew player can be arrested multiple times in their property
    // Should run for both server and clients to force block the processing
    // Additionally handles raidcop and investigator vision event blocking
    // Should maybe patch ShouldNoticeGeneralCrime Player player instead?=
    [HarmonyPatch(typeof(VisionCone), "SetSightableStateEnabled")]
    public static class VisionCone_SetSightableStateEnabled_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(VisionCone __instance, ISightable sightable, EVisualState state, ref bool enabled)
        {
            int instanceId = __instance.transform.root.gameObject.GetInstanceID();
            if (investigatorActive && instanceId == investigatorID)
            {
                
                if (PIdisabledVisualStates.Contains(state))
                    enabled = false;
                return true;
            }
            else if (raidActive && raidOfficerObjIDs.Contains(instanceId))
            {
                
                if (raiderDisabledVisualStates.Contains(state))
                    enabled = false;
                return true;
            }

            // During curfew if player was recently arrested or in property dont process vision event
            if (state == EVisualState.DisobeyingCurfew)
            {
                if (sightable.NetworkObject.TryGetComponent<Player>(out Player player))
                {
                    if ((player.CrimeData.MinsSinceLastArrested < 60 && NetworkSingleton<CurfewManager>.Instance.IsHardCurfewActive) || player.CurrentProperty != null)
                    {
                        enabled = false;
                    }
                }
            }

            return true;
        }

    }
}
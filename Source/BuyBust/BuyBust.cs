using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

using static NACopsV1.NACops;
using static NACopsV1.DebugModule;
using static NACopsV1.OfficerOverrides;
using static NACopsV1.AvatarUtility;

#if MONO
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.Law;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.Quests;
using ScheduleOne.UI.Handover;
using ScheduleOne.VoiceOver;
#else
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.UI.Handover;
using Il2CppScheduleOne.VoiceOver;
#endif
namespace NACopsV1
{
    [HarmonyPatch(typeof(Customer), "ProcessHandover")]
    public static class Customer_ProcessHandover_Patch
    {
        [HarmonyPrefix]
#if MONO
        public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
#else
        public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, Il2CppSystem.Collections.Generic.List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
#endif
        {
            coros.Add(MelonCoroutines.Start(PreProcessHandover(__instance, handoverByPlayer)));
            return true;
        }
        public static IEnumerator PreProcessHandover(Customer __instance, bool handoverByPlayer)
        {
            if (!handoverByPlayer) yield break;
            if (currentConfig.BuyBusts)
                coros.Add(MelonCoroutines.Start(SummonBustCop(__instance)));
            yield return null;
        }
        public static IEnumerator SummonBustCop(Customer customer)
        {
            int relation = Mathf.RoundToInt(customer.NPC.RelationData.RelationDelta * 10f);
            (float min, float max) = ThresholdUtils.Evaluate(thresholdConfig.BuyBustProbability, relation);
            if (!currentConfig.DebugMode && UnityEngine.Random.Range(min, max) < 0.5f) yield break;
            PoliceOfficer offc = SpawnOfficerRuntime(autoDeactivate: false);
            offc.Movement.PauseMovement();
            SetRandomAvatar(offc);
            offc.Behaviour.ScheduleManager.DisableSchedule();
            currentSummoned.Add(offc);
            Player target = null;
            Vector3 spawnPos = customer.transform.position + customer.transform.forward * 3f;
            bool flag = offc.Movement.GetClosestReachablePoint(spawnPos, out Vector3 closest);
            if (flag && closest != Vector3.zero)
            {
                coros.Add(MelonCoroutines.Start(SetTaser(offc)));
                offc.Movement.Warp(closest);
                offc.Movement.ResumeMovement();
                Log("Drug bust officer spawned now at " + offc.CenterPoint);
                offc.ChatterVO.Play(EVOLineType.Command);
                offc.Movement.FacePoint(customer.transform.position);
                target = Player.GetClosestPlayer(closest, out _);
                target.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.NonLethal);
                offc.BeginFootPursuit(target.PlayerCode);
                offc.PursuitBehaviour.Enable_Networked();
                // Needs to momentarily disable arrest or almost insta arrest;
                coros.Add(MelonCoroutines.Start(LateEnableArrest(offc)));
                target.CrimeData.AddCrime(new AttemptingToSell(), 10);
            }
            else
            {
                Log("Failed to Get closest reachable position for drug bust");
                coros.Add(MelonCoroutines.Start(DisposeSummoned(offc, true, target)));
            }
            coros.Add(MelonCoroutines.Start(DisposeSummoned(offc, false, target)));
            yield break;
        }

        public static IEnumerator LateEnableArrest(PoliceOfficer offc)
        {
            float maxWait = 8f;
            float current = 0f;
            for (; ; )
            {
                if (!registered) yield break;
                if (current >= maxWait) break;
                yield return Wait05;
                if (offc.PursuitBehaviour.arrestingEnabled)
                    offc.PursuitBehaviour.arrestingEnabled = false;
                current += 0.5f;
            }
            offc.PursuitBehaviour.arrestingEnabled = true;
            yield break;
        }

        public static IEnumerator SetTaser(PoliceOfficer offc)
        {
            offc.Behaviour.CombatBehaviour.SetWeapon(offc.TaserPrefab != null ? offc.TaserPrefab.AssetPath : string.Empty);

            if (offc.Behaviour.CombatBehaviour.currentWeapon == null) yield break;
#if MONO
            AvatarRangedWeapon rangedWeapon = offc.Behaviour.CombatBehaviour.currentWeapon as AvatarRangedWeapon;
#else
            AvatarRangedWeapon rangedWeapon = offc.Behaviour.CombatBehaviour.currentWeapon.TryCast<AvatarRangedWeapon>();
#endif
            if (rangedWeapon != null)
            {
                rangedWeapon.CanShootWhileMoving = true;
                rangedWeapon.MagazineSize = 20;
                rangedWeapon.MaxFireRate = 0.3f;
                rangedWeapon.MaxUseRange = 24f;
                rangedWeapon.ReloadTime = 0.2f;
                rangedWeapon.RaiseTime = 0.1f;
                rangedWeapon.HitChance_MaxRange = 0.6f;
                rangedWeapon.HitChance_MinRange = 0.9f;
                rangedWeapon.CooldownDuration = 0.3f;
            }

            yield return null;
        }
        public static IEnumerator DisposeSummoned(PoliceOfficer offc, bool instant, Player target)
        {
            yield return Wait1;
            if (!registered) yield break;

            int lifeTime = 0;
            int maxTime = 20;
            NPC npc = offc.NetworkObject.GetComponent<NPC>();

            if (!instant && target != null && npc != null)
            {
                while (lifeTime <= maxTime || target.IsArrested || npc.Health.IsDead || npc.Health.IsKnockedOut)
                {
                    lifeTime++;
                    yield return Wait1;
                    if (!registered) yield break;
                }
            }
            try
            {
                if (currentSummoned.Contains(offc))
                    currentSummoned.Remove(offc);
                if (npc != null && NPCManager.NPCRegistry.Contains(npc))
                    NPCManager.NPCRegistry.Remove(npc);
                if (npc != null && npc.gameObject != null)
                    UnityEngine.Object.Destroy(npc.gameObject);
            }
            catch (Exception ex)
            {
                MelonLogger.Error(ex);
            }
            Log("Disposed summoned bustcop");
        }
    }

}
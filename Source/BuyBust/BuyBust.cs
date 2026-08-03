using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.AI;

using static NACops.NACops;
using static NACops.DebugModule;
using static NACops.CopInitHelper;

#if MONO
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.Economy;
using ScheduleOne.ItemFramework;
using ScheduleOne.Law;
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
using Il2CppScheduleOne.AvatarFramework;
#endif
namespace NACops
{
    [HarmonyPatch(typeof(Customer), "ProcessHandover")]
    public static class Customer_ProcessHandover_Patch
    {
        public static int cooldownHours = 3;

        [HarmonyPrefix]
#if MONO
        public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
#else
        public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, Il2CppSystem.Collections.Generic.List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
#endif
        {
            MelonCoroutines.Start(PreProcessHandover(__instance, handoverByPlayer));
            return true;
        }
        public static IEnumerator PreProcessHandover(Customer __instance, bool handoverByPlayer)
        {
            if (!handoverByPlayer) yield break;
            if (cooldownHours > 0)
            {
                Log($"Cant run buy bust, on cooldown: {cooldownHours}");
                yield break;
            }

            if (currentConfig.BuyBusts)
                MelonCoroutines.Start(SummonBustCop(__instance));
            yield return null;
        }
        public static IEnumerator SummonBustCop(Customer customer)
        {
            int relation = Mathf.RoundToInt(customer.NPC.RelationData.RelationDelta * 10f);
            (float min, float max) = ThresholdUtils.Evaluate(thresholdConfig.BuyBustProbability, relation);
            if (!currentConfig.DebugMode && UnityEngine.Random.Range(min, max) < 0.5f) yield break;
            Log("Spawn buy bust");
            cooldownHours = 3;

            buyBustCop.gameObject.SetActive(true);
            buyBustCop.transform.Find("Avatar").gameObject.SetActive(true);
            buyBustCop.GetComponent<NavMeshAgent>().enabled = true;
            if (!buyBustCop.Movement.IsPaused)
                buyBustCop.Movement.PauseMovement();
            buyBustCop.Awareness.SetAwarenessActive(true);

            Player target = Player.Local;
            Vector3 spawnPos = customer.transform.position + customer.transform.forward * 3f;
            bool flag = buyBustCop.Movement.GetClosestReachablePoint(spawnPos, out Vector3 closest);
            bool instantDeactivate = false;
            if (flag && closest != Vector3.zero)
            {
                buyBustCop.Movement.Warp(closest);
                buyBustCop.Movement.ResumeMovement();
                Log("Drug bust officer spawned now at " + buyBustCop.CenterPoint);
                buyBustCop.ChatterVO.Play(EVOLineType.Command);
                buyBustCop.Movement.FacePoint(customer.transform.position);
                target.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.NonLethal);
                buyBustCop.BeginFootPursuit(target.PlayerCode);
                buyBustCop.PursuitBehaviour.Enable_Networked();
                coros.Add(MelonCoroutines.Start(SetTaser(buyBustCop)));
                // Needs to momentarily disable arrest or almost insta arrest;
                coros.Add(MelonCoroutines.Start(LateEnableArrest(buyBustCop)));
                target.CrimeData.AddCrime(new AttemptingToSell(), 10);
            }
            else
            {
                Log("Failed to Get closest reachable position for drug bust");
                instantDeactivate = true;
            }
            coros.Add(MelonCoroutines.Start(DisposeSummoned(instantDeactivate, target)));
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
                yield return Wait01;
                if (offc.PursuitBehaviour.arrestingEnabled)
                    offc.PursuitBehaviour.arrestingEnabled = false;
                current += 0.1f;
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

            yield break;
        }
        public static IEnumerator DisposeSummoned(bool instant, Player target)
        {
            yield return Wait1;
            if (!registered) yield break;

            int lifeTime = 0;
            int maxTime = 30;

            if (!instant && target != null && buyBustCop != null)
            {
                while (lifeTime <= maxTime)
                {
                    if (target.IsArrested || !buyBustCop.IsConscious) break;
                    lifeTime++;
                    yield return Wait1;
                    if (!registered) yield break;
                }
            }


            if (!buyBustCop.IsConscious)
            {
                yield return Wait30;
                buyBustCop.Health.Revive();
            }

            buyBustCop.Awareness.SetAwarenessActive(false);

            buyBustCop.gameObject.SetActive(false);

            buyBustCop.transform.Find("Avatar").gameObject.SetActive(false);

            if (!buyBustCop.Movement.IsPaused)
                buyBustCop.Movement.PauseMovement();
            buyBustCop.GetComponent<NavMeshAgent>().enabled = false;
            Log("Disposed summoned bustcop");
            yield break;
        }

        public static void ReduceBuyBustHours()
        {
            if (cooldownHours > 0)
                cooldownHours -= 1;
            Log($"Reduce buy bust hours now: {cooldownHours}");
        }
    }

}
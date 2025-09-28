using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.AI;

using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

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
using static ScheduleOne.AvatarFramework.AvatarSettings;
using FishNet.Object;
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
using static Il2CppScheduleOne.AvatarFramework.AvatarSettings;
using Il2CppFishNet.Object;
#endif
namespace NACopsV1
{

    #region Harmony Customer Buy Bust
    [HarmonyPatch(typeof(Customer), "ProcessHandover")]
    public static class Customer_ProcessHandover_Patch
    {
        public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
        {
            Log("ProcessHandover Customer Postfix");
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
            (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.BuyBustProbability, relation);
            if (UnityEngine.Random.Range(min, max) < 0.5) yield break;
            NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(policeBase);
            NPC myNpc = copNet.gameObject.GetComponent<NPC>();
            NavMeshAgent nav = copNet.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nav != null && !nav.enabled || nav.isStopped)
            {
                nav.enabled = true;
                nav.isStopped = false;
            }

            myNpc.ID = $"NACop_{Guid.NewGuid()}";
            myNpc.FirstName = $"NACop_{Guid.NewGuid()}";
            myNpc.LastName = "";
            myNpc.transform.parent = NPCManager.Instance.NPCContainer;
            NPCManager.NPCRegistry.Add(myNpc);

            networkManager.ServerManager.Spawn(copNet);
            yield return Wait05;

            copNet.gameObject.SetActive(true);
            yield return Wait01;

            PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
            currentSummoned.Add(offc);
            Player target = null;
            Vector3 spawnPos = customer.transform.position + customer.transform.forward * 3f;
            bool flag = offc.Movement.GetClosestReachablePoint(spawnPos, out Vector3 closest);
            if (flag && closest != Vector3.zero)
            {
                coros.Add(MelonCoroutines.Start(SetTaser(offc)));
                offc.Movement.Warp(closest);
                yield return Wait1;
                offc.ChatterVO.Play(EVOLineType.Command);
                offc.Movement.FacePoint(customer.transform.position);
                yield return Wait05;
                target = Player.GetClosestPlayer(closest, out _);
                target.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.NonLethal);
                offc.Movement.SetAgentType(NPCMovement.EAgentType.BigHumanoid);
                offc.BeginFootPursuit(target.PlayerCode);
                offc.PursuitBehaviour.SendEnable();
                target.CrimeData.AddCrime(new AttemptingToSell(), 10);
            }
            else
            {
                Log("Failed to Get closest reachable position for drug bust");
                coros.Add(MelonCoroutines.Start(DisposeSummoned(myNpc, offc, true, target)));
            }
            coros.Add(MelonCoroutines.Start(DisposeSummoned(myNpc, offc, false, target)));
            yield return null;
        }
        
        public static IEnumerator SetTaser(PoliceOfficer offc)
        {
            var taser = offc.TaserPrefab;
            if (taser == null) yield break;
#if MONO
            if (taser is AvatarRangedWeapon rangedWeapon)
            {
                rangedWeapon.CanShootWhileMoving = true;
                rangedWeapon.MagazineSize = 20;
                rangedWeapon.MaxFireRate = 0.3f;
                rangedWeapon.MaxUseRange = 24f;
                rangedWeapon.ReloadTime = 0.2f;
                rangedWeapon.RaiseTime = 0.1f;
                rangedWeapon.HitChance_MaxRange = 0.6f;
                rangedWeapon.HitChance_MinRange = 0.9f;
            }
            if (taser is AvatarWeapon weapon)
            {
                weapon.CooldownDuration = 0.3f;
            }
#else

            AvatarRangedWeapon temp = taser.TryCast<AvatarRangedWeapon>();
            if (temp != null)
            {
                temp.CanShootWhileMoving = true;
                temp.MagazineSize = 20;
                temp.MaxFireRate = 0.3f;
                temp.MaxUseRange = 24f;
                temp.ReloadTime = 0.2f;
                temp.RaiseTime = 0.1f;
                temp.HitChance_MaxRange = 0.6f;
                temp.HitChance_MinRange = 0.9f;
            }
            AvatarWeapon temp2 = taser.TryCast<AvatarWeapon>();
            if (temp2 != null)
            {
                temp2.CooldownDuration = 0.3f;
            }
#endif

            yield return null;
        }
        public static IEnumerator DisposeSummoned(NPC npc, PoliceOfficer offc, bool instant, Player target)
        {
            yield return Wait1;
            if (!registered) yield break;

            int lifeTime = 0;
            int maxTime = 20;
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
        }
#endregion
    }

}
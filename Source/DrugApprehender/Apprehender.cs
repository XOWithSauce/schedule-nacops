

using MelonLoader;
using UnityEngine;
using System.Collections;
using HarmonyLib;

using static NACopsV1.NACops;
using static NACopsV1.BaseUtility;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.Product;
#else
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Product;
#endif

namespace NACopsV1
{


    [HarmonyPatch(typeof(Player), "ConsumeProduct")]
    public static class Player_ConsumeProduct_Patch
    {
        public static bool evaluating = false;

#if IL2CPP
        static WeedInstance tempWeed = null;
        static MethInstance tempMeth = null;
        static CocaineInstance tempCoke = null;
#endif
        public static bool Prefix(Player __instance, ProductItemInstance product)
        {
            Log("ConsumePrefix");
            if (!evaluating && currentDrugApprehender.Count < 1)
            {
                evaluating = true;
                Log("CorosBegin");
                coros.Add(MelonCoroutines.Start(DrugConsumedCoro(__instance, product)));
            }
            return true;
        }

        public static IEnumerator DrugConsumedCoro(Player player, ProductItemInstance product)
        {
            if (!currentConfig.WeedInvestigator) yield break;
            bool pass = false;
#if MONO
            bool isWeed = product is WeedInstance;
            bool isMeth = product is MethInstance;
            bool isCoca = product is CocaineInstance;
            pass = isWeed || isMeth || isCoca;
            Log("Is Supported Instance for Apprehender: " + pass);
#else
            tempWeed = product.TryCast<WeedInstance>();
            tempMeth = product.TryCast<MethInstance>();
            tempCoke = product.TryCast<CocaineInstance>();
            pass = tempWeed != null || tempMeth != null || tempCoke != null;
            Log("Is Supported Instance for Apprehender: " + pass);
            tempWeed = null;
            tempMeth = null;
            tempCoke = null;
#endif
            if (pass)
            {
                Log("Instance casted, check officers count: " + allActiveOfficers.Count);

                PoliceOfficer noticeOfficer = null;
                float smallestDistance = 49f;
                bool direct = false;
                foreach (PoliceOfficer offc in allActiveOfficers)
                {
                    yield return Wait01;
                    if (GUIDInUse.Contains(offc.BakedGUID) || currentDrugApprehender.Contains(offc) || currentSummoned.Contains(offc)) continue;
                    if (Vector3.Distance(offc.transform.position, player.transform.position) > 50f) continue;
                    if (offc.Health.IsDead || offc.Health.IsKnockedOut) continue;
                    if (offc.Awareness.VisionCone.IsPlayerVisible(player) && offc.Movement.CanMove() && !offc.IsInVehicle && !offc.isInBuilding)
                    {
                        offc.BeginFootPursuit(player.PlayerCode);
                        coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player)));
                        direct = true;
                        Log("Apprehend immediate direct");
                        string activity = offc.Behaviour.activeBehaviour?.ToString();
                        Log("Current Activity: " + activity);
                        break;
                    }
                    else
                    {
                        float distance = Vector3.Distance(offc.transform.position, player.transform.position);
                        if (distance < smallestDistance && !offc.IsInVehicle && !offc.isInBuilding)
                        {
                            smallestDistance = distance;
                            noticeOfficer = offc;
                        }
                    }
                }

                if (noticeOfficer == null || direct)
                {
                    Log("No apprehender candidate found");
                    evaluating = false;
                    yield break;
                }

                currentDrugApprehender.Add(noticeOfficer);
                Log("Proceed apprehender candidate");

                coros.Add(MelonCoroutines.Start(ApprehenderOfficerClear(noticeOfficer)));

                bool apprehending = false;
                noticeOfficer.Movement.FacePoint(player.transform.position, lerpTime: 0.4f);
                yield return Wait05;
                if (noticeOfficer.Awareness.VisionCone.IsPlayerVisible(player))
                {
                    Log("Apprehend immediate candidate");
                    noticeOfficer.BeginBodySearch(player.PlayerCode);
                    coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player)));
                    apprehending = true;
                }

                if (noticeOfficer != null && !apprehending)
                {
                    for (int i = 0; i <= 6; i++)
                    {
                        Log("Apprehend Search suspect");

                        if (!registered) yield break;

                        // End foot search early if 5% random roll hits
                        if (i > 3 && UnityEngine.Random.Range(1f, 0f) > 0.95f)
                            break;

                        noticeOfficer.Movement.FacePoint(player.CenterPointTransform.position, lerpTime: 0.3f);
                        yield return Wait05;
                        if (!registered) yield break;
                        if (noticeOfficer.Health.IsDead || noticeOfficer.Health.IsKnockedOut) break;

                        if (noticeOfficer.Awareness.VisionCone.IsPlayerVisible(player))
                        {
                            noticeOfficer.BeginBodySearch(player.PlayerCode);
                            if (UnityEngine.Random.Range(1f, 0f) > 0.8f)
                                coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 1, player)));
                            break;
                        }

                        yield return Wait05;
                        if (!registered) yield break;

                        if (!(noticeOfficer.Health.IsDead || noticeOfficer.Health.IsKnockedOut))
                            noticeOfficer.Movement.SetDestination(player.CenterPointTransform.position);
                        else
                            break;

                        yield return Wait1;
                        if (!registered) yield break;
                        if (noticeOfficer.Health.IsDead || noticeOfficer.Health.IsKnockedOut) break;


                    }
                }
            }
            Log("evaluate apprehender end");
            evaluating = false;
            yield return null;
        }

        public static IEnumerator ApprehenderOfficerClear(PoliceOfficer offc)
        {
            if (offc == null) yield break;
            yield return Wait30;
            Log(" apprehender clear");
            if (currentDrugApprehender.Contains(offc))
                currentDrugApprehender.Remove(offc);
        }


    }
}

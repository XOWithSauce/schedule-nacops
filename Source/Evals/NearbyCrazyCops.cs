using System.Collections;
using MelonLoader;
using UnityEngine;

using static NACopsV1.BaseUtility;
using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.VoiceOver;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.VoiceOver;
#endif


namespace NACopsV1
{

    public static class NearbyCrazyCops
    {


        private static float minWait;
        private static float maxWait;

        private static float minRange;
        private static float maxRange;

        private static List<WaitForSeconds> randWaits;
        private static WaitForSeconds currentAwait;

        public static IEnumerator RunNearbyCrazyCops()
        {
            Log("Nearby Crazy Cop Enabled");
            (minWait, maxWait) = ThresholdUtils.Evaluate(ThresholdMappings.NearbyCrazThres, TimeManager.Instance.ElapsedDays);
            randWaits = new()
            {
                new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait)),
                new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait)),
                new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait)),
            };

            for (; ; )
            {
                currentAwait = randWaits[UnityEngine.Random.Range(0, randWaits.Count)];
                yield return currentAwait;
                if (!registered) yield break;
                Log("Nearby Crazy Cop Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                (minRange, maxRange) = ThresholdUtils.Evaluate(ThresholdMappings.NearbyCrazRange, (int)MoneyManager.Instance.LifetimeEarnings);
                float minDistance = UnityEngine.Random.Range(minRange, maxRange);

                foreach (Player player in players)
                {
                    if (player.CurrentProperty != null)
                        continue;

                    foreach (PoliceOfficer officer in allActiveOfficers)
                    {
                        yield return Wait01;
                        if (!registered) yield break;

                        if (officer.IsInVehicle)
                            continue;

                        if (officer.Behaviour.activeBehaviour && officer.Behaviour.activeBehaviour is VehiclePatrolBehaviour)
                            continue;

                        if (officer.Behaviour.activeBehaviour && officer.Behaviour.activeBehaviour is VehiclePursuitBehaviour)
                            continue;

                        if (GUIDInUse.Contains(officer.BakedGUID))
                            continue;

                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);
                        if (distance < minDistance && !currentSummoned.Contains(officer) && !currentDrugApprehender.Contains(officer) && !IsStationNearby(player.transform.position) && !officer.IsInVehicle && !officer.isInBuilding && !officer.Health.IsDead && !officer.Health.IsKnockedOut)
                        {
                            GUIDInUse.Add(officer.BakedGUID);
                            coros.Add(MelonCoroutines.Start(GreedyBodySearchFind(officer, player, minDistance)));
                            break;
                        }

                    }
                }
            }

        }

        public static IEnumerator GreedyBodySearchFind(PoliceOfficer officer, Player player, float minDistance)
        {

            officer.ChatterVO.Play(EVOLineType.PoliceChatter);
            officer.Movement.FacePoint(player.transform.position, lerpTime: 0.4f);

            yield return Wait05;
            if (!registered) yield break;

            if (officer.Awareness.VisionCone.IsPlayerVisible(player) && !player.CrimeData.BodySearchPending)
            {
                officer.BeginBodySearch(player.PlayerCode);
                if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
                    coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 2, player: player)));

                if (GUIDInUse.Contains(officer.BakedGUID))
                    GUIDInUse.Remove(officer.BakedGUID);
                yield break;
            }

            officer.Movement.GetClosestReachablePoint(player.CenterPointTransform.position, out Vector3 pos);
            if (pos != Vector3.zero && officer.Movement.CanMove() && officer.Movement.CanGetTo(pos))
                officer.Movement.SetDestination(pos);

            if (GUIDInUse.Contains(officer.BakedGUID))
                GUIDInUse.Remove(officer.BakedGUID);
            yield return null;
        }

    }
   
}
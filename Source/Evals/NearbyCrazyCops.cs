using System.Collections;
using MelonLoader;
using UnityEngine;

using static NACopsV1.BaseUtility;
using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.VoiceOver;
using ScheduleOne.DevUtilities;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.VoiceOver;
using Il2CppScheduleOne.DevUtilities;
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
            if (!networkManager.IsServer) yield break;

            Log("Nearby Crazy Cop Enabled");
            (minWait, maxWait) = ThresholdUtils.Evaluate(thresholdConfig.NearbyCrazyFrequency, NetworkSingleton<TimeManager>.Instance.ElapsedDays);
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

                // if threshold has changed update awaits now
                float newMin;
                float newMax;
                (newMin, newMax) = ThresholdUtils.Evaluate(thresholdConfig.NearbyCrazyFrequency, NetworkSingleton<TimeManager>.Instance.ElapsedDays);
                if (newMin != minWait || newMax != maxWait)
                {
                    randWaits.Clear();
                    for (int i = 0; i < 3; i++)
                        randWaits.Add(new WaitForSeconds(UnityEngine.Random.Range(newMin, newMax)));
                }

                (minRange, maxRange) = ThresholdUtils.Evaluate(thresholdConfig.NearbyCrazyRange, (int)MoneyManager.Instance.LifetimeEarnings);
                float minDistance = UnityEngine.Random.Range(minRange, maxRange);

                foreach (Player player in Player.PlayerList)
                {
                    player.CrimeData.CheckNearestOfficer();
                    PoliceOfficer officer = player.CrimeData.NearestOfficer;
                    if (officer == null) continue;

                    if (!CanProceed(officer, player, minDistance, ignoreVehicle:true)) continue;

                    GUIDInUse.Add(officer.BakedGUID);
                    if (officer.IsInVehicle && player.IsInVehicle)
                    {
                        officer.VehiclePursuitBehaviour.AssignTarget(player);
                        officer.VehiclePursuitBehaviour.beginAsSighted = true;
                        officer.VehiclePursuitBehaviour.Activate();
                    }
                    else if (!officer.IsInVehicle)
                    {
                        coros.Add(MelonCoroutines.Start(GreedyBodySearchFind(officer, player, minDistance)));
                    }
                    break;
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
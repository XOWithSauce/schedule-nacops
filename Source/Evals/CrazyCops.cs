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
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
#endif

namespace NACopsV1
{
    public static class CrazyCops
    {
        private static float minWait;
        private static float maxWait;

        private static float minRange;
        private static float maxRange;

        private static List<WaitForSeconds> randWaits;
        private static WaitForSeconds currentAwait;

        private static PoliceOfficer nearestOfficer = null;
        private static float closestDistance = 100f;
        private static float earlyTermDist = 20f;
        public static IEnumerator RunCrazyCops()
        {
            Log("Crazy Cops Enabled");
            (minWait, maxWait) = ThresholdUtils.Evaluate(ThresholdMappings.CrazyCopsFreq, TimeManager.Instance.ElapsedDays);
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

                Log("Crazy Cops Evaluate");

                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                if (randomPlayer.CurrentProperty != null)
                    continue;

                Vector3 playerPosition = randomPlayer.transform.position;

                nearestOfficer = null;

                foreach (PoliceOfficer officer in allActiveOfficers)
                {
                    yield return Wait01;
                    if (!registered) yield break;

                    float distance = Vector3.Distance(officer.transform.position, playerPosition);
                    if (distance < closestDistance && !currentSummoned.Contains(officer) && !currentDrugApprehender.Contains(officer) && !GUIDInUse.Contains(officer.BakedGUID) && !IsStationNearby(playerPosition) && !officer.isInBuilding)
                    {
                        
                        closestDistance = distance;
                        nearestOfficer = officer;
                        if (closestDistance <= earlyTermDist)
                            break;
                    }
                }

                if (nearestOfficer == null)
                    continue;


                GUIDInUse.Add(nearestOfficer.BakedGUID);
                (minRange, maxRange) = ThresholdUtils.Evaluate(ThresholdMappings.CrazyCopsRange, (int)MoneyManager.Instance.LifetimeEarnings);
                earlyTermDist = Mathf.Lerp(minRange, maxRange, 0.3f);
                if (nearestOfficer != null && closestDistance < UnityEngine.Random.Range(minRange, maxRange))
                {
                    // Vehicle patrols we need diff behaviour
                    if (nearestOfficer.Behaviour.activeBehaviour && nearestOfficer.Behaviour.activeBehaviour is VehiclePatrolBehaviour)
                    {
                        nearestOfficer.BeginVehiclePursuit(randomPlayer.PlayerCode, nearestOfficer.AssignedVehicle.NetworkObject, true);
                        // does this need the agent too for driving?
                        coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player: randomPlayer)));
                        if (GUIDInUse.Contains(nearestOfficer.BakedGUID))
                            GUIDInUse.Remove(nearestOfficer.BakedGUID);
                        continue;
                    }

                    // Foot patrols checkpoints etc
                    if (UnityEngine.Random.Range(0f, 1f) > 0.3f)
                    {
                        nearestOfficer.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.4f);
                        yield return Wait05;
                        if (!registered) yield break;

                        if (nearestOfficer.Awareness.VisionCone.IsPlayerVisible(randomPlayer))
                        {
                            nearestOfficer.BeginFootPursuit(randomPlayer.PlayerCode);
                            coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player: randomPlayer)));
                        }
                    }
                    else
                    {
                        // Police called -> dispatch vehicle and sleep 4s -> investigation status
                        coros.Add(MelonCoroutines.Start(LateInvestigation(randomPlayer)));
                    }

                    if (GUIDInUse.Contains(nearestOfficer.BakedGUID))
                        GUIDInUse.Remove(nearestOfficer.BakedGUID);
                }
            }
        }

    }
    
}
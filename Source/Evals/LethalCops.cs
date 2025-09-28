
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
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
#endif

namespace NACopsV1
{

    public static class LethalCops
    {

        private static float minWait;
        private static float maxWait;

        private static float minRange;
        private static float maxRange;

        private static List<WaitForSeconds> randWaits;
        private static WaitForSeconds currentAwait;

        public static IEnumerator RunNearbyLethalCops()
        {

            (minWait, maxWait) = ThresholdUtils.Evaluate(ThresholdMappings.LethalCopFreq, TimeManager.Instance.ElapsedDays);
            randWaits = new()
            {
                new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait)),
                new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait)),
                new WaitForSeconds(UnityEngine.Random.Range(minWait, maxWait)),
            };

            Log("Nearby Lethal Cop Enabled");
            for (; ; )
            {
                Log("Nearby Lethal Cop Evaluate");

                currentAwait = randWaits[UnityEngine.Random.Range(0, randWaits.Count)];
                yield return currentAwait;
                if (!registered) yield break;

                (minRange, maxRange) = ThresholdUtils.Evaluate(ThresholdMappings.LethalCopRange, (int)MoneyManager.Instance.LifetimeEarnings);
                float minDistance = UnityEngine.Random.Range(minRange, maxRange);

                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];

                foreach (PoliceOfficer officer in allActiveOfficers)
                {
                    yield return Wait01;
                    if (!registered) yield break;

                    float distance = Vector3.Distance(officer.transform.position, randomPlayer.transform.position);

                    if (distance < minDistance && !currentSummoned.Contains(officer) && !currentDrugApprehender.Contains(officer) && !IsStationNearby(randomPlayer.transform.position) && !randomPlayer.CrimeData.BodySearchPending && !officer.IsInVehicle && officer.Behaviour.activeBehaviour != officer.CheckpointBehaviour && !officer.isInBuilding && !GUIDInUse.Contains(officer.BakedGUID))
                    {
                        GUIDInUse.Add(officer.BakedGUID);
                        officer.Movement.FacePoint(Player.Local.transform.position, lerpTime: 0.4f);
                        yield return Wait05;
                        if (!registered) yield break;

                        if (officer.Awareness.VisionCone.IsPlayerVisible(randomPlayer))
                        {
                            randomPlayer.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Lethal);
                            officer.BeginFootPursuit(randomPlayer.PlayerCode);
                        }
                        if (GUIDInUse.Contains(officer.BakedGUID))
                            GUIDInUse.Remove(officer.BakedGUID);
                        break;
                    }
                }
            }
        }

    }

}
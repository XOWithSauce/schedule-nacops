
using System.Collections;
using UnityEngine;

using static NACopsV1.BaseUtility;
using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.DevUtilities;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.DevUtilities;
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
            if (!networkManager.IsServer) yield break;

            (minWait, maxWait) = ThresholdUtils.Evaluate(thresholdConfig.LethalCopFrequency, NetworkSingleton<TimeManager>.Instance.ElapsedDays);
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

                // if threshold has changed update awaits now
                float newMin;
                float newMax;
                (newMin, newMax) = ThresholdUtils.Evaluate(thresholdConfig.LethalCopFrequency, NetworkSingleton<TimeManager>.Instance.ElapsedDays);
                if (newMin != minWait || newMax != maxWait)
                {
                    randWaits.Clear();
                    for (int i = 0; i < 3; i++)
                        randWaits.Add(new WaitForSeconds(UnityEngine.Random.Range(newMin, newMax)));
                }

                (minRange, maxRange) = ThresholdUtils.Evaluate(thresholdConfig.LethalCopRange, (int)MoneyManager.Instance.LifetimeEarnings);
                float minDistance = UnityEngine.Random.Range(minRange, maxRange);

                foreach (Player player in Player.PlayerList)
                {
                    player.CrimeData.CheckNearestOfficer();
                    PoliceOfficer officer = player.CrimeData.NearestOfficer;
                    if (officer == null) continue;

                    if (!CanProceed(officer, player, minDistance)) continue;

                    GUIDInUse.Add(officer.BakedGUID);
                    officer.Movement.FacePoint(Player.Local.transform.position, lerpTime: 0.4f);
                    yield return Wait05;
                    if (!registered) yield break;

                    if (officer.Awareness.VisionCone.IsPlayerVisible(player))
                    {
                        player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Lethal);
                        officer.BeginFootPursuit(player.PlayerCode);
                    }
                    if (GUIDInUse.Contains(officer.BakedGUID))
                        GUIDInUse.Remove(officer.BakedGUID);
                }
                
            }
        }

    }

}
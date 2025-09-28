
using System.Collections;

using MelonLoader;
using UnityEngine;

using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.Law;
using ScheduleOne.Map;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using FishNet.Managing.Object;
using FishNet.Object;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppFishNet.Managing.Object;
using Il2CppFishNet.Object;
#endif

namespace NACopsV1
{
    public static class BaseUtility
    {
        public static List<string> GUIDInUse = new List<string>();
        public static IEnumerator AttemptWarp(PoliceOfficer offc, Transform target)
        {
            Vector3 warpInit = Vector3.zero;
            int maxWarpAttempts = 10;
            for (int i = 0; i < maxWarpAttempts; i++)
            {
                yield return Wait05;
                if (!registered) yield break;

                float xInitOffset = UnityEngine.Random.Range(8f, 30f);
                float zInitOffset = UnityEngine.Random.Range(8f, 30f);
                xInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                zInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                Vector3 targetWarpPosition = target.position + new Vector3(xInitOffset, 0f, zInitOffset);
                offc.Movement.GetClosestReachablePoint(targetWarpPosition, out warpInit);
                if (warpInit != Vector3.zero)
                {
                    offc.Movement.Warp(warpInit);
                    break;
                }
            }
            yield break;
        }
        public static bool IsStationNearby(Vector3 pos)
        {
            float distToStation = Vector3.Distance(PoliceStation.GetClosestPoliceStation(pos).transform.position, pos);
            return distToStation < 20f;
        }
        public static void SetPoliceNPC()
        {
            PrefabObjects spawnablePrefabs = networkManager.SpawnablePrefabs;
            for (int i = 0; i < spawnablePrefabs.GetObjectCount(); i++)
            {
                NetworkObject prefab = spawnablePrefabs.GetObject(true, i);
                if (prefab?.gameObject?.name == "PoliceNPC")
                {
                    policeBase = prefab;
                    break;
                }
            }
        }
        public static IEnumerator GiveFalseCharges(int severity, Player player)
        {
            if (!currentConfig.CorruptCops) yield break;
            switch (severity)
            {
                case 1:
                    player.CrimeData.AddCrime(new DrugTrafficking());
                    player.CrimeData.AddCrime(new AttemptingToSell());
                    player.CrimeData.AddCrime(new Evading());
                    break;
                case 2:
                    player.CrimeData.AddCrime(new FailureToComply(), 10);
                    player.CrimeData.AddCrime(new Evading());
                    break;
                case 3:
                    player.CrimeData.AddCrime(new PossessingHighSeverityDrug(), 60);
                    break;

            }
            yield return null;
        }
        public static IEnumerator LateInvestigation(Player player)
        {
            yield return Wait1;
            if (!registered || !Singleton<LawManager>.InstanceExists || PoliceStation.PoliceStations.Count == 0) yield break;
            PoliceStation station = PoliceStation.GetClosestPoliceStation(player.transform.position);
            if (station.OccupantCount < 2) yield break;
            try
            {
                Singleton<LawManager>.Instance.PoliceCalled(player, new DrugTrafficking());
            }
            catch (NullReferenceException ex)
            {
                MelonLogger.Error("Failed to invoke PoliceCalled status " + ex);
            }
            yield return Wait5;
            if (!registered) yield break;
            player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Investigating);
            yield return null;
        }

    }
}
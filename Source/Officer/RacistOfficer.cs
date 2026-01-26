
using MelonLoader;
using System.Collections;
using UnityEngine;

using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.Map;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
#else
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
#endif

namespace NACopsV1
{
    public static class RacistOfficers
    {
        // So when 1 cop notices a black player in their vision it begins lethal pursuit
        // and makes all officers within 80 units trigger pursuit and they cant arrest
        public static IEnumerator EvaluateOfficersVision()
        {
            if (!networkManager.IsServer) yield break;

            Log("Racist officers enabled");
            List<Player> blackPlayers = new();
            foreach (Player player in Player.PlayerList)
                if (!player.Avatar.IsWhite())
                    blackPlayers.Add(player);
            
            if (blackPlayers.Count == 0)
                yield break;
            while (registered)
            {
                yield return Wait2;
                foreach (Player player in blackPlayers)
                {
                    if (player.CurrentProperty != null)
                        continue;

                    bool blackPlayerNoticed = false;
                    foreach (PoliceOfficer officer in allActiveOfficers)
                    {
                        if (officer.Health.IsDead || officer.Health.IsKnockedOut) continue;

                        if (officer.Awareness.VisionCone.IsPlayerVisible(player))
                        {
                            blackPlayerNoticed = true;
                            break;
                        }
                    }
                    if (!blackPlayerNoticed) continue;
                    foreach (PoliceOfficer officer in allActiveOfficers)
                    {
                        yield return Wait05;
                        if (officer.Behaviour.activeBehaviour && (officer.Behaviour.activeBehaviour == officer.PursuitBehaviour || officer.Behaviour.activeBehaviour == officer.VehiclePursuitBehaviour)) continue;
                        if (Vector3.Distance(officer.CenterPoint, player.CenterPointTransform.position) < 80f)
                        {
                            if (officer.PursuitBehaviour.arrestingEnabled)
                                coros.Add(MelonCoroutines.Start(TempDisableArrest(officer)));

                            if (player.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.Lethal)
                                player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Lethal);

                            if (officer.isInBuilding) officer.ExitBuilding(PoliceStation.PoliceStations[0]);

                            if (!officer.IsInVehicle && officer.Behaviour.activeBehaviour != officer.PursuitBehaviour)
                            {
                                officer.BeginFootPursuit(player.PlayerCode);
                            }
                            else if (officer.IsInVehicle && officer.Behaviour.activeBehaviour != officer.VehiclePursuitBehaviour)
                            {
                                officer.VehiclePursuitBehaviour.AssignTarget(player);
                                officer.VehiclePursuitBehaviour.StartPursuit();
                            }
                        }
                    }
                    yield return Wait30;
                }
            }
            yield return null;
        }

        public static IEnumerator TempDisableArrest(PoliceOfficer offc)
        {
            yield return Wait30;
            offc.PursuitBehaviour.arrestingEnabled = true;
            yield return false;
        }
    }
}
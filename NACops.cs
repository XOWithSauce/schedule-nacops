using MelonLoader;
using System.Collections;
using System.Reflection;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.NPCs;
using UnityEngine;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.NPCs.Schedules;
using ScheduleOne.GameTime;
using ScheduleOne.Audio;
[assembly: MelonInfo(typeof(NACopsV1.NACops), NACopsV1.BuildInfo.Name, NACopsV1.BuildInfo.Version, NACopsV1.BuildInfo.Author, NACopsV1.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame(null, null)]

// Create and Setup a MelonGame Attribute to mark a Melon as Universal or Compatible with specific Games.
// If no MelonGame Attribute is found or any of the Values for any MelonGame Attribute on the Melon is null or empty it will be assumed the Melon is Universal.
// Values for MelonGame Attribute can be found in the Game's app.info file or printed at the top of every log directly beneath the Unity version.

namespace NACopsV1
{
    public static class BuildInfo
    {
        public const string Name = "NACopsV1";
        public const string Description = "Crazyyyy cops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.3";
        public const string DownloadLink = null;
    }
    public class NACops : MelonMod
    {
        PoliceOfficer[] officers;
        List<object> coros = new();
        private HashSet<PoliceOfficer> currentPIs = new HashSet<PoliceOfficer>();

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("NACops Loaded, Enjoy getting fucked by the POPO!!");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                if (this.officers == null || this.officers.Length == 0)
                    this.officers = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>(true);

                this.coros.Add(MelonCoroutines.Start(this.SetOfficers()));
                this.coros.Add(MelonCoroutines.Start(this.CrazyCops()));
                this.coros.Add(MelonCoroutines.Start(this.NearbyCrazyCop()));
                this.coros.Add(MelonCoroutines.Start(this.NearbyLethalCop()));
                this.coros.Add(MelonCoroutines.Start(this.PrivateInvestigator()));
            } else
            {
                foreach(object coro in coros)
                {
                    MelonCoroutines.Stop(coro);
                }
                coros.Clear();
            }
        }

        private IEnumerator NearbyLethalCop()
        {
            for (; ;)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(8f, 20f));
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                PoliceOfficer nearestOfficer = null;

                float minDistance = 6f;
                foreach (Player player in players)
                {
                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);
                        if (distance < minDistance && !currentPIs.Contains(officer))
                        {
                            nearestOfficer = officer;
                            try
                            {
                                player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Lethal);
                                officer.BeginFootPursuit_Networked(player.NetworkObject, false);
                            }
                            catch (Exception ex)
                            {
                                MelonLogger.Error("Error while starting Lethal Cop: " + ex.Message);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private IEnumerator NearbyCrazyCop()
        {
            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(60f, 480f));
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                PoliceOfficer nearestOfficer = null;
                float minDistance = 30f;
                foreach (Player player in players)
                {
                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);
                        if (distance < minDistance && !currentPIs.Contains(officer))
                        {
                            nearestOfficer = officer;
                            nearestOfficer.Movement.WalkSpeed = 7f;
                            nearestOfficer.Movement.SetDestination(player.transform.position);
                            yield return new WaitForSeconds(8f);
                            nearestOfficer.BeginBodySearch_Networked(player.NetworkObject);
                            nearestOfficer.Movement.WalkSpeed = 2.4f;
                            break;
                        } else { continue; }
                    }
                }
            }
            
        }

        private IEnumerator PrivateInvestigator()
        {
            float maxTime = 480f;

            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(480f, 2880f));
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                PoliceOfficer randomOfficer = officers[UnityEngine.Random.Range(0, officers.Length)];
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];

                randomOfficer.Movement.WalkSpeed = 3f;

                EDay currentDay = TimeManager.Instance.CurrentDay;
                if (currentDay.ToString().Contains("Saturday") || currentDay.ToString().Contains("Sunday"))
                {
                    // MelonLogger.Msg("Can not appoint PI during weekend");
                    continue;
                }

                if (currentPIs.Contains(randomOfficer))
                {
                    // MelonLogger.Msg("Officer is already a PI, skipping.");
                    continue;
                }

                currentPIs.Add(randomOfficer);

                //MelonLogger.Msg("Hired a Private Investigator");
                float elapsed = 0f;
                for (; ; )
                {
                    yield return new WaitForSeconds(5f);
                    elapsed += 5f;

                    float distance = Vector3.Distance(randomOfficer.transform.position, randomPlayer.transform.position);

                    if (!randomOfficer.Movement.CanMove())
                    {
                        //MelonLogger.Msg("Officer cant move! Cancelling.");
                        break;
                    }

                    if (elapsed >= maxTime)
                    {
                        //MelonLogger.Msg("Private Investigator session ended: Max time reached.");
                        break;
                    }

                    if (randomOfficer.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 20f)
                    {
                        //MelonLogger.Msg("Setting PI destination");
                        if (randomOfficer.Movement.IsPaused)
                            randomOfficer.Movement.ResumeMovement();

                        float xOffset = UnityEngine.Random.Range(8f, 14f);
                        float zOffset = UnityEngine.Random.Range(8f, 14f);
                        xOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                        zOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;

                        Vector3 targetPosition = randomPlayer.transform.position + new Vector3(xOffset, 0f, zOffset);
                        randomOfficer.Movement.GetClosestReachablePoint(targetPosition, out Vector3 pos);
                        randomOfficer.Movement.SetDestination(pos);
                    }
                    else if (randomOfficer.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance <= 20f)
                    {
                        //MelonLogger.Msg("Suspect In Proximity... Monitoring.");
                        if (!randomOfficer.Movement.IsPaused)
                            randomOfficer.Movement.PauseMovement();
                        randomOfficer.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.9f);
                    }
                    else
                    {

                        //MelonLogger.Msg("PI destination is outside proximity. Cancelling.");
                        break;
                    }
                }

                if (randomOfficer.Movement.IsPaused)
                    randomOfficer.Movement.ResumeMovement();
                randomOfficer.Movement.WalkSpeed = 2.4f;
                currentPIs.Remove(randomOfficer);
            }
        }

        private IEnumerator CrazyCops()
        {
            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(1440f, 2880f));

                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                if (officers.Length > 0 && players.Length > 0)
                {
                    Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                    Vector3 playerPosition = randomPlayer.transform.position;

                    PoliceOfficer nearestOfficer = null;
                    float closestDistance = float.MaxValue;

                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, playerPosition);
                        if (distance < closestDistance && !currentPIs.Contains(officer))
                        {
                            closestDistance = distance;
                            nearestOfficer = officer;
                        }
                    }

                    if (nearestOfficer != null && closestDistance < 40f)
                    {
                        try
                        {
                            nearestOfficer.BeginFootPursuit_Networked(randomPlayer.NetworkObject, true);
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Error("Error calling FootPursuit: " + ex.Message);
                        }
                    }
                    else
                    {
                        MelonLogger.Error("No officers found in the scene.");
                    }
                }
            }
        }

        private IEnumerator SetOfficers()
        {
            yield return new WaitForSeconds(10f);

            try
            {
                foreach (PoliceOfficer obj in officers)
                {
                    if (obj.ProxCircle != null)
                    {
                        obj.ProxCircle.SetRadius(10f);
                        // MelonLogger.Msg($"Set Officer proximity radius to 10f.");
                    }
                    else
                    {
                        MelonLogger.Warning($"Officer has no ProxCircle assigned.");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error changing Cop Proximity Circle: " + ex.Message);
            }

            try
            {
                Type type = Type.GetType("ScheduleOne.Police.PoliceOfficer, Assembly-CSharp");
                FieldInfo field = type.GetField("BodySearchChance", BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                {
                    foreach (PoliceOfficer obj in officers)
                    {
                        // MelonLogger.Msg("BodySearchChance Set");
                        field.SetValue(obj, 1f);
                    }
                }
                else
                {
                    MelonLogger.Error("BodySearchChance not a valid field");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error changing Cop BodySearchChance variables: " + ex.Message);
            }

            try
            {
                Type type = Type.GetType("ScheduleOne.Police.PoliceOfficer, Assembly-CSharp");
                FieldInfo field = type.GetField("BodySearchDuration", BindingFlags.Instance | BindingFlags.Public);
                if (field != null)
                {
                    foreach (PoliceOfficer obj in officers)
                    {
                        // MelonLogger.Msg("BodySearchDuration Set");

                        field.SetValue(obj, 20f);
                    }
                }
                else
                {
                    MelonLogger.Error("BodySearchChance not a valid field");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error changing Cop BodySearchChance variables: " + ex.Message);
            }

            try
            {
                foreach (PoliceOfficer officer in officers)
                {
                    officer.Leniency = 0.1f;
                    officer.Suspicion = 1f;
                    officer.OverrideAggression(1f);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error changing Cop variables: " + ex.Message);
            }

            try
            {
                foreach (PoliceOfficer officer in officers)
                {
                    BodySearchBehaviour bodySearch = officer.GetComponent<BodySearchBehaviour>();
                    OverrideBodySearchEscalation(bodySearch);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error changing Cop Body search timeouts " + ex.Message);
            }

            foreach (PoliceOfficer officer in officers)
            {
                officer.Movement.RunSpeed = 9f;
                officer.Movement.WalkSpeed = 2.4f;
            }

            yield return null;
        }

        private void OverrideBodySearchEscalation(BodySearchBehaviour bodySearch)
        {
            if (bodySearch == null) return;

            FieldInfo timeOutsideRangeField = typeof(BodySearchBehaviour).GetField("timeOutsideRange", BindingFlags.NonPublic | BindingFlags.Instance);
            if (timeOutsideRangeField != null)
            {
                timeOutsideRangeField.SetValue(bodySearch, 30f);
            }

            FieldInfo targetDistanceField = typeof(BodySearchBehaviour).GetField("targetDistanceOnStart", BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetDistanceField != null)
            {
                float currentDistance = (float)targetDistanceField.GetValue(bodySearch);
                targetDistanceField.SetValue(bodySearch, currentDistance + 30f);
            }

            // Debug.Log("Modified BodySearchBehaviour: Increased timeOutsideRange and distance.");
        }
    }
}

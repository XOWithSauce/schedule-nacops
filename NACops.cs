using MelonLoader;
using System.Collections;
using System.Reflection;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using UnityEngine;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.GameTime;
using ScheduleOne.AvatarFramework.Equipping;
using HarmonyLib;
using ScheduleOne.Product;

[assembly: MelonInfo(typeof(NACopsV1.NACops), NACopsV1.BuildInfo.Name, NACopsV1.BuildInfo.Version, NACopsV1.BuildInfo.Author, NACopsV1.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace NACopsV1
{
    public static class BuildInfo
    {
        public const string Name = "NACopsV1";
        public const string Description = "Crazyyyy cops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.4";
        public const string DownloadLink = null;
    }
    public class NACops : MelonMod
    {
        private static HarmonyLib.Harmony harmonyInstance;

        static PoliceOfficer[] officers;
        public static List<object> coros = new();
        public static HashSet<PoliceOfficer> currentPIs = new HashSet<PoliceOfficer>();
        public static HashSet<PoliceOfficer> currentDrugApprehender = new HashSet<PoliceOfficer>();

        public override void OnApplicationStart()
        {
            // MelonLogger.Msg("NACops Loaded, Enjoy getting fucked by the POPO!!");
            harmonyInstance = new HarmonyLib.Harmony(BuildInfo.Name);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(Player), "ConsumeProduct")]
        public static class Player_ConsumeProduct_Patch
        {
            public static bool Prefix(Player __instance, ProductItemInstance product)
            {
                coros.Add(MelonCoroutines.Start(DrugConsumedCoro(__instance, product)));
                return true;
            }
        }

        private static IEnumerator DrugConsumedCoro(Player player, ProductItemInstance product)
        {
            if (product is WeedInstance weed)
            {
                yield return new WaitForSeconds(2f);
                PoliceOfficer noticeOfficer = null;
                float smallestDistance = 20f;
                foreach(PoliceOfficer offc in officers)
                {
                    float distance = Vector3.Distance(offc.transform.position, player.transform.position);
                    if (distance < 20f && distance < smallestDistance && offc.Movement.CanMove() && !currentPIs.Contains(offc) && !currentDrugApprehender.Contains(offc))
                    {
                        smallestDistance = distance;
                        noticeOfficer = offc;
                    }
                }
                currentDrugApprehender.Add(noticeOfficer);

                yield return new WaitForSeconds(smallestDistance);

                MelonCoroutines.Start(ApprehenderOfficerClear(noticeOfficer));

                bool apprehending = false;
                if (noticeOfficer.awareness.VisionCone.IsPointWithinSight(player.transform.position))
                {
                    // MelonLogger.Msg("Point within immediate sight apprehend drug user");
                    noticeOfficer.BeginBodySearch_Networked(player.NetworkObject);
                    apprehending = true;
                }

                if (noticeOfficer != null && !apprehending)
                {
                    for (int i = 0; i <= 15; i++)
                    {
                        // MelonLogger.Msg($"Officer searching for drug user for {i} seconds");
                        noticeOfficer.Movement.FacePoint(player.transform.position, lerpTime: 0.1f);
                        yield return new WaitForSeconds(0.2f);
                        if (noticeOfficer.awareness.VisionCone.IsPlayerVisible(player))
                        {
                            // MelonLogger.Msg("PlayerInVision, apprehend drug user");
                            noticeOfficer.BeginBodySearch_Networked(player.NetworkObject);
                            break;
                        }

                        yield return new WaitForSeconds(0.2f);
                        noticeOfficer.Movement.GetClosestReachablePoint(player.transform.position, out Vector3 pos);
                        if (noticeOfficer.Movement.CanMove() && noticeOfficer.Movement.CanGetTo(pos))
                        {
                            noticeOfficer.Movement.SetDestination(pos);
                        }
                        else
                        {
                            // MelonLogger.Msg($"Officer cant move or go to position, cancelling.");
                            break;
                        }
                        yield return new WaitForSeconds(0.6f);
                    }
                } 
            }
            else
            {
                yield return null;
            }
        }

        private static IEnumerator ApprehenderOfficerClear(PoliceOfficer offc)
        {
            yield return new WaitForSeconds(20f);
            if (currentDrugApprehender.Contains(offc))
                currentDrugApprehender.Remove(offc);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                // MelonLogger.Msg("Start State");
                officers = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>(true);

                coros.Add(MelonCoroutines.Start(this.SetOfficers()));
                coros.Add(MelonCoroutines.Start(this.CrazyCops()));
                coros.Add(MelonCoroutines.Start(this.NearbyCrazyCop()));
                coros.Add(MelonCoroutines.Start(this.NearbyLethalCop()));
                coros.Add(MelonCoroutines.Start(this.PrivateInvestigator()));
            }
            else
            {
                // MelonLogger.Msg("Clear State");
                foreach (object coro in coros)
                {
                    MelonCoroutines.Stop(coro);
                }
                coros.Clear();
                currentPIs.Clear();
                currentDrugApprehender.Clear();
            }
        }

        private IEnumerator NearbyLethalCop()
        {
            // MelonLogger.Msg("Nearby Lethal Cop");
            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(8f, 20f));
                // MelonLogger.Msg("Nearby Lethal Cop Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                PoliceOfficer nearestOfficer = null;

                float minDistance = 6f;
                foreach (Player player in players)
                {
                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);
                        if (distance < minDistance && !currentPIs.Contains(officer) && !currentDrugApprehender.Contains(officer))
                        {
                            // MelonLogger.Msg("Nearby Lethal Cop Run");
                            nearestOfficer = officer;
                            try
                            {
                                player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Lethal);
                                officer.BeginFootPursuit_Networked(player.NetworkObject, false);
                            }
                            catch (Exception ex)
                            {
                                // MelonLogger.Error("Error while starting Lethal Cop: " + ex.Message);
                            }
                            break;
                        }
                    }
                }
            }
        }

        private IEnumerator NearbyCrazyCop()
        {
            // MelonLogger.Msg("Nearby Crazy Cop");
            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(60f, 480f));
                // MelonLogger.Msg("Nearby Crazy Cop Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                PoliceOfficer nearestOfficer = null;
                float minDistance = 30f;
                foreach (Player player in players)
                {
                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);
                        if (distance < minDistance && !currentPIs.Contains(officer) && !currentDrugApprehender.Contains(officer))
                        {
                            // MelonLogger.Msg("Nearby Crazy Cop Run");
                            nearestOfficer = officer;
                            nearestOfficer.Movement.WalkSpeed = 7f;
                            nearestOfficer.Movement.SetDestination(player.transform.position);
                            yield return new WaitForSeconds(8f);
                            nearestOfficer.BeginBodySearch_Networked(player.NetworkObject);
                            nearestOfficer.Movement.WalkSpeed = 2.4f;
                            break;
                        }
                        else { continue; }
                    }
                }
            }

        }

        private IEnumerator PrivateInvestigator()
        {
            // MelonLogger.Msg("Private Investigator");
            float maxTime = 480f;

            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(480f, 2880f));
                // MelonLogger.Msg("PI Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                PoliceOfficer randomOfficer = officers[UnityEngine.Random.Range(0, officers.Length)];
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];

                randomOfficer.Movement.WalkSpeed = 3f;

                EDay currentDay = TimeManager.Instance.CurrentDay;
                if (currentDay.ToString().Contains("Saturday") || currentDay.ToString().Contains("Sunday"))
                {
                    continue;
                }

                if (currentPIs.Contains(randomOfficer) || currentDrugApprehender.Contains(randomOfficer))
                {
                    continue;
                }

                currentPIs.Add(randomOfficer);

                float elapsed = 0f;
                for (; ; )
                {
                    yield return new WaitForSeconds(5f);
                    elapsed += 5f;

                    float distance = Vector3.Distance(randomOfficer.transform.position, randomPlayer.transform.position);

                    if (!randomOfficer.Movement.CanMove())
                    {
                        break;
                    }

                    if (elapsed >= maxTime)
                    {
                        break;
                    }

                    if (randomOfficer.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 20f)
                    {
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
                        if (!randomOfficer.Movement.IsPaused)
                            randomOfficer.Movement.PauseMovement();
                        randomOfficer.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.9f);
                    }
                    else
                    {
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
            // MelonLogger.Msg("Crazy Cops");

            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(1440f, 2880f));
                // MelonLogger.Msg("Crazy Cops Evaluate");

                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                if (officers.Length > 0 && players.Length > 0)
                {
                    // MelonLogger.Msg("Crazy Cops Run");
                    Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                    Vector3 playerPosition = randomPlayer.transform.position;

                    PoliceOfficer nearestOfficer = null;
                    float closestDistance = float.MaxValue;

                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, playerPosition);
                        if (distance < closestDistance && !currentPIs.Contains(officer) && !currentDrugApprehender.Contains(officer))
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
                            // MelonLogger.Error("Error calling FootPursuit: " + ex.Message);
                        }
                    }
                    else
                    {
                        // MelonLogger.Error("No officers found in the scene.");
                    }
                }
            }
        }

        private IEnumerator SetOfficers()
        {
            yield return new WaitForSeconds(10f);
            // MelonLogger.Msg("Officers variables override");
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
                // MelonLogger.Error("Error changing Cop Body search timeouts " + ex.Message);
            }

            foreach (PoliceOfficer officer in officers)
            {
                officer.Leniency = 0.1f;
                officer.Suspicion = 1f;
                officer.OverrideAggression(1f);
                officer.BodySearchDuration = 20f;
                officer.BodySearchChance = 1f;
                officer.Movement.RunSpeed = 10f;
                officer.Movement.WalkSpeed = 2.4f;
                officer.behaviour.CombatBehaviour.GiveUpRange = 40f;
                officer.behaviour.CombatBehaviour.GiveUpTime = 60f;
                officer.behaviour.CombatBehaviour.DefaultSearchTime = 60f;
                officer.behaviour.CombatBehaviour.DefaultMovementSpeed = 10f;
                officer.behaviour.CombatBehaviour.GiveUpAfterSuccessfulHits = 40;

                AvatarRangedWeapon rangedWeapon = officer.GunPrefab as AvatarRangedWeapon;
                if (rangedWeapon != null)
                {
                    rangedWeapon.CanShootWhileMoving = true;
                    rangedWeapon.MagazineSize = 20;
                    rangedWeapon.MaxFireRate = 0.1f;
                    rangedWeapon.MaxUseRange = 20f;
                    rangedWeapon.ReloadTime = 0.5f;
                    rangedWeapon.RaiseTime = 0.2f;
                    rangedWeapon.HitChange_MaxRange = 0.3f;
                    rangedWeapon.HitChange_MinRange = 0.8f;
                } else
                {
                    // MelonLogger.Msg("Failed to access Ranged Weapon of officer.");
                }
            }

            yield return null;
        }

        private void OverrideBodySearchEscalation(BodySearchBehaviour bodySearch)
        {
            if (bodySearch == null) return;

            try
            {
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
            } 
            catch (Exception ex) 
            {
                // MelonLogger.Warning($"Failed to change body search behaviour: {ex}");
            }
        }
    }
}

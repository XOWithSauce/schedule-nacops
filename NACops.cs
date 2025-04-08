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
using ScheduleOne.VoiceOver;
using ScheduleOne.Money;
using ScheduleOne.Law;
using ScheduleOne.AvatarFramework;
using static ScheduleOne.AvatarFramework.AvatarSettings;
using ScheduleOne.Persistence;

[assembly: MelonInfo(typeof(NACopsV1.NACops), NACopsV1.BuildInfo.Name, NACopsV1.BuildInfo.Version, NACopsV1.BuildInfo.Author, NACopsV1.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: HarmonyDontPatchAll]

namespace NACopsV1
{
    public static class BuildInfo
    {
        public const string Name = "NACopsV1";
        public const string Description = "Crazyyyy cops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.5.2";
        public const string DownloadLink = null;
    }

    public class NACops : MelonMod
    {
        private static HarmonyLib.Harmony harmonyInstance;
        static PoliceOfficer[] officers;
        public static List<object> coros = new();
        public static HashSet<PoliceOfficer> currentPIs = new HashSet<PoliceOfficer>();
        public static HashSet<PoliceOfficer> currentDrugApprehender = new HashSet<PoliceOfficer>();
        private bool registered = false;
        #region Unity
        public override void OnApplicationStart()
        {
            // // MelonLogger.Msg("NACops Loaded, Enjoy getting fucked by the POPO!!");
            harmonyInstance = new HarmonyLib.Harmony(BuildInfo.Name);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                if (LoadManager.Instance != null && !registered)
                {
                    LoadManager.Instance.onLoadComplete.AddListener(OnLoadCompleteCb);
                    registered = true;
                }
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

        public void OnLoadCompleteCb()
        {
            MelonLogger.Msg("OnLoadCompleteCb");
            officers = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>(true);
            coros.Add(MelonCoroutines.Start(this.SetOfficers()));
            coros.Add(MelonCoroutines.Start(this.CrazyCops()));
            coros.Add(MelonCoroutines.Start(this.NearbyCrazyCop()));
            coros.Add(MelonCoroutines.Start(this.NearbyLethalCop()));
            coros.Add(MelonCoroutines.Start(this.PrivateInvestigator()));
        }
        #endregion

        #region Harmony
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
                    if (distance < 20f && distance < smallestDistance && offc.Movement.CanMove() && !currentPIs.Contains(offc) && !currentDrugApprehender.Contains(offc) && currentDrugApprehender.Count < 1)
                    {
                        smallestDistance = distance;
                        noticeOfficer = offc;
                    }
                }
                if (noticeOfficer == null)
                    yield return null;

                currentDrugApprehender.Add(noticeOfficer);

                yield return new WaitForSeconds(smallestDistance);

                MelonCoroutines.Start(ApprehenderOfficerClear(noticeOfficer));

                bool apprehending = false;
                if (noticeOfficer.awareness.VisionCone.IsPointWithinSight(player.transform.position))
                {
                    // MelonLogger.Msg("Point within immediate sight apprehend drug user");
                    noticeOfficer.BeginBodySearch_Networked(player.NetworkObject);
                    player.CrimeData.AddCrime(new FailureToComply(), UnityEngine.Random.Range(1, 30));
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
                            if (UnityEngine.Random.Range(1f, 0f) > 0.8f)
                                player.CrimeData.AddCrime(new PossessingHighSeverityDrug(), UnityEngine.Random.Range(1, 4));
                            if (UnityEngine.Random.Range(1f, 0f) > 0.8f) 
                                player.CrimeData.AddCrime(new Evading());
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
        #endregion

        #region Base Coroutines
        private IEnumerator NearbyLethalCop()
        {
            // MelonLogger.Msg("Nearby Lethal Cop");
            for (; ; )
            {
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.LethalCopFreq, TimeManager.Instance.ElapsedDays);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                // MelonLogger.Msg("Nearby Lethal Cop Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                PoliceOfficer nearestOfficer = null;

                float minDistance = UnityEngine.Random.Range(4f, 8f);
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
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.NearbyCrazThres, TimeManager.Instance.ElapsedDays);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                // MelonLogger.Msg("Nearby Crazy Cop Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                PoliceOfficer nearestOfficer = null;
                float minDistance = UnityEngine.Random.Range(20f,30f);
                foreach (Player player in players)
                {
                    if (player.CurrentProperty != null)
                        continue;
                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);
                        if (distance < minDistance && !currentPIs.Contains(officer) && !currentDrugApprehender.Contains(officer))
                        {
                            // MelonLogger.Msg("Nearby Crazy Cop Run");
                            nearestOfficer = officer;
                            nearestOfficer.Movement.GetClosestReachablePoint(player.transform.position, out Vector3 pos);
                            if (nearestOfficer.Movement.CanMove() && nearestOfficer.Movement.CanGetTo(pos))
                            {
                                nearestOfficer.Movement.WalkSpeed = 7f;
                                nearestOfficer.Movement.SetDestination(pos);
                            } 
                            else
                            {
                                break;
                            }
                            yield return new WaitForSeconds(8f);
                            nearestOfficer.ChatterVO.Play(EVOLineType.PoliceChatter);
                            nearestOfficer.Movement.FacePoint(player.transform.position, lerpTime: 0.2f);
                            yield return new WaitForSeconds(0.2f);
                            if (nearestOfficer.awareness.VisionCone.IsPlayerVisible(player))
                            {
                                nearestOfficer.BeginBodySearch_Networked(player.NetworkObject);
                                if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
                                    player.CrimeData.AddCrime(new FailureToComply(), 10);
                            }

                            nearestOfficer.ChatterVO.Play(EVOLineType.PoliceChatter);
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
            float maxTime = 180f;
            //MelonLogger.Msg("PI Start");
            for (; ; )
            {
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.PIThres, (int)MoneyManager.Instance.LifetimeEarnings);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                //MelonLogger.Msg("PI Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                if (players.Length == 0)
                    continue;
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];

                if (officers.Length == 0)
                    continue;
                PoliceOfficer randomOfficer = officers[UnityEngine.Random.Range(0, officers.Length)];

                //MelonLogger.Msg("PI Select from Nearby");
                for (int i = 0; i <= 4; i++)
                {
                    float distance = Vector3.Distance(randomOfficer.transform.position, randomPlayer.transform.position);
                    if (distance < 60f)
                    {
                        break;
                    }
                    randomOfficer = officers[UnityEngine.Random.Range(0, officers.Length)];
                }

                EDay currentDay = TimeManager.Instance.CurrentDay;
                if (currentDay.ToString().Contains("Saturday") || currentDay.ToString().Contains("Sunday"))
                    continue;

                if (currentPIs.Contains(randomOfficer) || currentDrugApprehender.Contains(randomOfficer) || currentPIs.Count >= 1)
                    continue;

                //MelonLogger.Msg("PI Proceed");

                if (randomOfficer.behaviour.activeBehaviour)
                    randomOfficer.behaviour.activeBehaviour.SendEnd();

                if (randomOfficer.isInBuilding)
                    randomOfficer.ExitBuilding();

                if (randomOfficer.IsInVehicle)
                    randomOfficer.ExitVehicle();

                if (Vector3.Distance(randomOfficer.transform.position, randomPlayer.transform.position) > 30f)
                {
                    float xInitOffset = UnityEngine.Random.Range(20f, 30f);
                    float zInitOffset = UnityEngine.Random.Range(20f, 30f);
                    xInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                    zInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                    Vector3 targetWarpPosition = randomPlayer.transform.position + new Vector3(xInitOffset, 0f, zInitOffset);
                    randomOfficer.Movement.GetClosestReachablePoint(targetWarpPosition, out Vector3 warpInit);
                    randomOfficer.Movement.Warp(warpInit);
                }

                randomOfficer.Movement.WalkSpeed = 3f;
                currentPIs.Add(randomOfficer);

                //MelonLogger.Msg("Copy Avatar");
                AvatarSettings orig = DeepCopyAvatarSettings(randomOfficer.Avatar.CurrentSettings);
                var bodySettings = randomOfficer.Avatar.CurrentSettings.BodyLayerSettings;
                var accessorySettings = randomOfficer.Avatar.CurrentSettings.AccessorySettings;

                //MelonLogger.Msg("Clear Avatar");

                for (int i = 0; i < bodySettings.Count; i++)
                {
                    var layer = bodySettings[i];
                    layer.layerPath = "";
                    layer.layerTint = Color.white;
                    bodySettings[i] = layer;
                }

                for (int i = 0; i < accessorySettings.Count; i++)
                {
                    var acc = accessorySettings[i];
                    acc.path = "";
                    acc.color = Color.white;
                    accessorySettings[i] = acc;
                }

                //MelonLogger.Msg("Apply Disguise");
                var jeans = bodySettings[2];
                jeans.layerPath = "Avatar/Layers/Bottom/Jeans";
                jeans.layerTint = new Color(0.396f, 0.396f, 0.396f);
                bodySettings[2] = jeans;
                var shirt = bodySettings[3];
                shirt.layerPath = "Avatar/Layers/Top/RolledButtonUp";
                shirt.layerTint = new Color(0.326f, 0.578f, 0.896f);
                bodySettings[3] = shirt;

                var cap = accessorySettings[0];
                cap.path = "Avatar/Accessories/Head/Cap/Cap";
                cap.color = new Color(0.613f, 0.493f, 0.344f);
                accessorySettings[0] = cap;
                var blazer = accessorySettings[1];
                blazer.path = "Avatar/Accessories/Chest/Blazer/Blazer";
                blazer.color = new Color(0.613f, 0.493f, 0.344f);
                accessorySettings[1] = blazer;
                var sneakers = accessorySettings[2];
                sneakers.path = "Avatar/Accessories/Feet/Sneakers/Sneakers";
                sneakers.color = new Color(0.151f, 0.151f, 0.151f);
                accessorySettings[2] = sneakers;

                randomOfficer.Avatar.ApplyBodyLayerSettings(randomOfficer.Avatar.CurrentSettings);
                randomOfficer.Avatar.ApplyAccessorySettings(randomOfficer.Avatar.CurrentSettings);

                //MelonLogger.Msg("Fall into PI Loop");
                float elapsed = 0f;
                for (; ; )
                {
                    yield return new WaitForSeconds(5f);
                    elapsed += 5f;

                    float distance = Vector3.Distance(randomOfficer.transform.position, randomPlayer.transform.position);
                    //MelonLogger.Msg($"PI Run dist: {distance}");

                    if (!randomOfficer.Movement.CanMove() || elapsed >= maxTime)
                        break;

                    randomOfficer.behaviour.activeBehaviour = null;
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
                        //MelonLogger.Msg("PI Monitoring");
                        if (!randomOfficer.Movement.IsPaused)
                            randomOfficer.Movement.PauseMovement();
                        randomOfficer.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.9f);
                        if (UnityEngine.Random.Range(0f, 1f) > 0.95f)
                            randomPlayer.CrimeData.AddCrime(new DrugTrafficking());
                        if (UnityEngine.Random.Range(0f, 1f) > 0.95f)
                            randomPlayer.CrimeData.AddCrime(new AttemptingToSell());
                    }
                    else
                    {
                        //MelonLogger.Msg("PI Cant reach target");
                        break;
                    }
                }

                //MelonLogger.Msg("PI Finished");
                if (randomOfficer.Movement.IsPaused)
                    randomOfficer.Movement.ResumeMovement();
                randomOfficer.Movement.WalkSpeed = 2.4f;
                randomOfficer.Avatar.ApplyBodyLayerSettings(orig);
                randomOfficer.Avatar.ApplyAccessorySettings(orig);
                currentPIs.Remove(randomOfficer);
                // MelonLogger.Msg("PI Reverted");
            }
        }

        private IEnumerator CrazyCops()
        {
            // MelonLogger.Msg("Crazy Cops");

            for (; ; )
            {
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.CrazyCopsFreq, TimeManager.Instance.ElapsedDays);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                // MelonLogger.Msg("Crazy Cops Evaluate");

                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                if (officers.Length > 0 && players.Length > 0)
                {
                    // MelonLogger.Msg("Crazy Cops Run");
                    Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                    if (randomPlayer.CurrentProperty != null)
                        continue;

                    Vector3 playerPosition = randomPlayer.transform.position;

                    PoliceOfficer nearestOfficer = null;
                    float closestDistance = 100f;

                    foreach (PoliceOfficer officer in officers)
                    {
                        float distance = Vector3.Distance(officer.transform.position, playerPosition);
                        if (distance < closestDistance && !currentPIs.Contains(officer) && !currentDrugApprehender.Contains(officer))
                        {
                            closestDistance = distance;
                            nearestOfficer = officer;
                        }
                    }

                    if (nearestOfficer != null && closestDistance < UnityEngine.Random.Range(10f, 30f))
                    {
                        try
                        {
                            nearestOfficer.BeginFootPursuit_Networked(randomPlayer.NetworkObject, true);
                            randomPlayer.CrimeData.AddCrime(new PossessingHighSeverityDrug(), 60);

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
        #endregion

        #region Base Utils
        private IEnumerator SetOfficers()
        {
            // MelonLogger.Msg("Officers variables override");
            foreach (PoliceOfficer officer in officers)
            {
                yield return new WaitForSeconds(0.05f);
                BodySearchBehaviour bodySearch = officer.GetComponent<BodySearchBehaviour>();
                OverrideBodySearchEscalation(bodySearch);
            }

            foreach (PoliceOfficer officer in officers)
            {
                yield return new WaitForSeconds(0.05f);
                officer.Leniency = 0.1f;
                officer.Suspicion = 1f;
                officer.OverrideAggression(1f);
                officer.BodySearchDuration = 20f;
                officer.BodySearchChance = 1f;
                officer.Movement.RunSpeed = 9f;
                officer.Movement.WalkSpeed = 2.4f;
                officer.behaviour.CombatBehaviour.GiveUpRange = 40f;
                officer.behaviour.CombatBehaviour.GiveUpTime = 60f;
                officer.behaviour.CombatBehaviour.DefaultSearchTime = 60f;
                officer.behaviour.CombatBehaviour.DefaultMovementSpeed = 9f;
                officer.behaviour.CombatBehaviour.GiveUpAfterSuccessfulHits = 40;

                officer.Health.MaxHealth = 175f;
                officer.Health.Revive();

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
            // MelonLogger.Msg("Officer properties complete");
            yield return null;
        }
        private AvatarSettings DeepCopyAvatarSettings(AvatarSettings original)
        {
            AvatarSettings copy = new AvatarSettings();

            copy.BodyLayerSettings = original.BodyLayerSettings.Select(layer => new LayerSetting
            {
                layerPath = layer.layerPath,
                layerTint = layer.layerTint
            }).ToList();

            copy.AccessorySettings = original.AccessorySettings.Select(acc => new AccessorySetting
            {
                path = acc.path,
                color = acc.color
            }).ToList();

            return copy;
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
        #endregion

        #region Utils for progression
        public class MinMaxThreshold
        {
            public int MinOf { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }

            public MinMaxThreshold(int minOf, float min, float max)
            {
                MinOf = minOf;
                Min = min;
                Max = max;
            }
        }
        public static class ThresholdUtils
        {
            public static (float min, float max) Evaluate(List<MinMaxThreshold> thresholds, int value)
            {
                MinMaxThreshold bestMatch = thresholds[0];
                foreach (var threshold in thresholds)
                {
                    if (value >= threshold.MinOf)
                        bestMatch = threshold;
                    else
                        break;
                }

                return (bestMatch.Min, bestMatch.Max);
            }
        }

        public static class ThresholdMappings
        {
            // Days total
            public static readonly List<MinMaxThreshold> LethalCopFreq = new()
            {
                new(0,   20f, 50f),
                new(5,   14f, 40f),
                new(10,  10f, 30f),
                new(20,  9f, 30f),
                new(30,  8f, 20f),
                new(40,  8f, 20f),
                new(50,  8f, 20f),
            };
            // Days total
            public static readonly List<MinMaxThreshold> CrazyCopsFreq = new()
            {
                new(0,   400f, 800f),
                new(5,   350f, 700f),
                new(10,  300f, 600f),
                new(20,  200f, 600f),
                new(30,  200f, 500f),
                new(40,  150f, 450f),
                new(50,  150f, 400f),
            };
            // Days total
            public static readonly List<MinMaxThreshold> NearbyCrazThres = new()
            {
                new(0,   600f, 960f),
                new(5,   400f, 960f),
                new(10,  100f, 600f),
                new(20,  90f, 600f),
                new(30,  90f, 500f),
                new(40,  90f, 450f),
                new(50,  60f, 300f),
            };
            // Networth
            public static readonly List<MinMaxThreshold> PIThres = new()
            {
                new(100,     300f, 400f),
                new(1000,    200f, 400f),
                new(10000,   100f, 400f),
                new(30000,   90f, 300f),
                new(60000,   90f, 200f),
                new(100000,  90f, 200f),
                new(300000,  90f, 200f),
            };
        }

        #endregion
    }
}

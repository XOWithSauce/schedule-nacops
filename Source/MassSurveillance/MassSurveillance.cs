


using MelonLoader;
using System.Collections;
using UnityEngine;
using HarmonyLib;

using static NACops.DebugModule;
using static NACops.NACops;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Law;
using ScheduleOne.Map;
using ScheduleOne.ObjectScripts;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.Vision;
using ScheduleOne.Levelling;
using ScheduleOne.Money;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Vision;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Money;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

namespace NACops
{

    public static class SurveillanceCameraPaths
    {
        public static readonly List<string> unidirectional = new()
        {
            "Map/Hyland Point/Region_Downtown/Casino/casino/Security Camera (Barrel) (1)",
            "Map/Hyland Point/Region_Downtown/Diner/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Storage warehouse/Storage warehouse/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Small warehouse/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/ChemicalPlant/Chemical Plant A/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Pawn shop/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/Casino/casino/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/RE Office/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Uptown/Medical Practice/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Hardware Store/Small hardware store/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/HardwardStore/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/Slums Gas Station/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/North apartments/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/ChemicalPlant/Warehouse01/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Pawn shop/Interior/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/Dealership/Dealership/Security Camera (Barrel)",
            "@Properties/Sweatshop/Chinese Restaurant/Security Camera (Barrel)",
            "@Properties/Sweatshop/Chinese Restaurant/Security Camera (Barrel) (1)",
            "Map/Hyland Point/Region_Northtown/North apartments/Security Camera (Barrel) (1)",
            "Map/Hyland Point/Region_Northtown/Arcade (1)/UpperBlankWall (3)/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/Corner Store/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Arcade (1)/arcade/Overhang/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/Police station/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Shooting range/Shooting range/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Northtown/Storage warehouse/Storage warehouse/Security Camera (Barrel) (1)",
            "Map/Hyland Point/Region_Northtown/Industrial Building A/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/Restaurant/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/Gas Station/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/Slums Gas Station/slums gas station/Shop (Content Disabler)/Security Camera (Barrel) (1)",
            "Map/Hyland Point/Region_Downtown/TownCenter/Bank/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/Gas Station/gas station/Interior/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/Slums Gas Station/slums gas station/Shop (Content Disabler)/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/Tattoo Parlour New/Interior/GameObject/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Westville/Cabin/Security Camera (Barrel)",
            "Map/Hyland Point/Region_Downtown/GroceryStore/Security Camera (Barrel)",
            "@Businesses/Taco Ticklers/Security Camera (Barrel)",
            "@Businesses/Taco Ticklers/Security Camera (Barrel) (1)",
            "@Businesses/Taco Ticklers/Security Camera (Barrel) (2)",
            "@Businesses/Car Wash/Security Camera (Barrel)",
        };

        public static readonly List<string> omnidirectional = new()
        {
            "Map/Hyland Point/Region_Westville/Slums Gas Station/Security Camera (Round)",
            "Map/Hyland Point/Region_Downtown/Courthouse/Security Camera (Round)",
            "Map/Hyland Point/Region_Downtown/Police station/Security Camera (Round)",
            "Map/Hyland Point/Region_Docks/Fish Warehouse/Security Camera (Round)",
            "Map/Hyland Point/Region_Downtown/TownCenter/Bank/Security Camera (Round)",
            "Map/Hyland Point/Region_Northtown/Construction yard/Fence/Construction Warehouse/Security Camera (Round)",
            "@Businesses/Laundromat/Security Camera (Round)",
            "@Businesses/PostOffice/Security Camera (Round)",
        };
    }
    public static class MassSurveillance
    {
        private static bool _surveilSeenStatesEvaluating= false;
        private static bool _surveilCrimeStatusEvaluating = false;
        private static bool _hasDispatchedNearby = false;

        public static Material lineRendererMaterial;
        public static GameObject fxSparksTemplate;

        public static int playerLayer;
        public static int obstacleLayer; 
        public static int activationZoneLayer;
        public static int raycastIgnoreZone;

        public static LayerMask visibilityBlockingLayers;

        public static List<HylandFlockInstance> allCameras = new();
        public static List<HylandFlockInstance> activeCameras = new();

        public static Transform activationZoneParent;

        // All flock instances use same raycast buffer and there can only be 1 instance casting its result
        public static bool isEvaluatingRaycast = false;
        public static HitComparer raycastCompare = new HitComparer();
#if MONO
        public static RaycastHit[] raycastHitBuffer = new RaycastHit[2];
#else
        // Because NonAlloc Raycast return is of type Il2CppStructArray and not array
        public static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit> raycastHitBuffer = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit>(2);
#endif
        public static IEnumerator SetupMassSurveillance()
        {

            Shader lineRendererShader = Shader.Find("Universal Render Pipeline/Lit");
            lineRendererMaterial = new Material(lineRendererShader);
            lineRendererMaterial.color = Color.blue;
            playerLayer = LayerMask.NameToLayer("Player");
            // obstacleLayer = LayerMask.GetMask("Terrain", "Default", "Vehicle") | visibilityBlockingLayers;
            obstacleLayer = UnityEngine.Object.FindObjectOfType<VisionCone>(true).VisibilityBlockingLayers;
            activationZoneLayer = LayerMask.NameToLayer("Invisible");
            raycastIgnoreZone = ~LayerMask.GetMask("Invisible", "Ignore Raycast");

            VendingMachine vendingMachine = UnityEngine.Object.FindObjectOfType<VendingMachine>(true);
            Transform fxSparks = vendingMachine.transform.Find("FX_Sparks_01");
            fxSparksTemplate = UnityEngine.Object.Instantiate(fxSparks.gameObject);
            fxSparksTemplate.SetActive(false);

            activationZoneParent = new GameObject("FlockActivationZones").transform;

            // instantiate to each camera to be ready to activate
            foreach (string transformPath in SurveillanceCameraPaths.unidirectional)
            {
                GameObject target = GameObject.Find(transformPath);
                if (target)
                {
                    GameObject flockInstanceObj = new("FlockCamera");
                    flockInstanceObj.transform.SetParent(target.transform);
                    HylandFlockInstance instance = flockInstanceObj.AddComponent<HylandFlockInstance>();
                    instance.type = ECameraType.Unidirectional;

                    if (transformPath.Contains("@Business"))
                        instance.disableType = ECameraDisableAccess.BusinessComputer;
                    else
                        instance.disableType = ECameraDisableAccess.None;

                    allCameras.Add(instance);
                }
                else
                {
                    Log($"Expected to find camera at transform path and failed to find:\n{transformPath}");
                }
            }

            foreach (string transformPath in SurveillanceCameraPaths.omnidirectional)
            {
                GameObject target = GameObject.Find(transformPath);
                if (target)
                {
                    GameObject flockInstanceObj = new("FlockCamera");
                    flockInstanceObj.transform.SetParent(target.transform);
                    HylandFlockInstance instance = flockInstanceObj.AddComponent<HylandFlockInstance>();
                    instance.type = ECameraType.Omnidirectional;

                    if (transformPath.Contains("@Business"))
                        instance.disableType = ECameraDisableAccess.BusinessComputer;
                    else
                        instance.disableType = ECameraDisableAccess.None;

                    allCameras.Add(instance);
                }
                else
                {
                    Log($"Expected to find camera at transform path and failed to find:\n{transformPath}");
                }
            }

            foreach (HylandFlockInstance cam in allCameras)
            {
                cam.Initialize();
                if (!cam.gameObject.activeSelf)
                    cam.gameObject.SetActive(true);
                if (!cam.enabled)
                    cam.enabled = true;
            }

#if MONO
            NetworkSingleton<TimeManager>.Instance.onSleepEnd += RotateCameraActivity;
#else
            NetworkSingleton<TimeManager>.Instance.onSleepEnd += (Il2CppSystem.Action)RotateCameraActivity;
#endif

#if MONO
            Player.Local.onArrested += OnPlayerArrestedClearCache;
#else
            Player.Local.onArrested += (Il2CppSystem.Action)OnPlayerArrestedClearCache;
#endif

            // Then build crime table to allow for config support for the crime fines
            PenaltyHandler_ProcessCrimeList_Patch.BuildCrimeTable();

            Log("Done setting up mass surveillance");

            // Wait cops init before enabling this days cameras
#if MONO
            yield return new WaitUntil(() => hasInitiatedAllOfficers);
#else
            yield return new WaitUntil((Il2CppSystem.Func<bool>)(() => hasInitiatedAllOfficers));
#endif
            RotateCameraActivity();
            yield break;
        }

        public static void ResetMassSurveillance()
        {
            _surveilSeenStatesEvaluating = false;
            _surveilCrimeStatusEvaluating = false;
            _hasDispatchedNearby = false;
            lineRendererMaterial = null;
            fxSparksTemplate = null;
            allCameras.Clear();
            activeCameras.Clear();
            activationZoneParent = null;
            isEvaluatingRaycast = false;
            return;
        }

        // Logic for rotating the camera activattion daily
        public static void RotateCameraActivity()
        {
            if (!currentConfig.MassSurveillance) return;

            Log("Rotating camera activity");

            int camerasToday = surveillanceConfig.ActiveCamerasPerDay;
            List<HylandFlockInstance> newCameras = new();
            List<Vector3> newCamerasPositions = new();
            float minDistFromOtherCamera = surveillanceConfig.CameraActivationRange;
            allCameras.Shuffle();
            
            foreach (HylandFlockInstance inst in allCameras)
            {
                if (newCameras.Count >= camerasToday) 
                    break;

                // ensure new active camera is not tied to business and set offline by player
                // TODO logic?
                if (inst.isOffline)
                    continue;

                // ensure new active camera is not the previous camera
                if (activeCameras.Count > 0)
                {
                    if (activeCameras.Contains(inst))
                        continue;
                }

                // ensure new camera is not in ActivationRange from other selected camera
                if (newCamerasPositions.Count > 0)
                {
                    bool isCloseToNewCamera = false;
                    foreach (Vector3 pos in newCamerasPositions)
                    {
                        if (Vector3.Distance(pos, inst.transform.position) < minDistFromOtherCamera)
                            isCloseToNewCamera = true;
                    }
                    if (isCloseToNewCamera)
                        continue;
                }
                newCameras.Add(inst);
                newCamerasPositions.Add(inst.transform.position);
                Log($"Selected {inst.type} camera at {inst.transform.position} for todays cameras");
            }
            newCamerasPositions.Clear();

            // Deactive previous
            if (activeCameras.Count > 0)
            {
                foreach (HylandFlockInstance activeInst in activeCameras)
                {
                    activeInst.DeactivateInstance();
                }
                activeCameras.Clear();
            }

            // Active new ones
            foreach (HylandFlockInstance inst in newCameras)
            {
                Log("Enable cam");
                inst.ActivateInstance();
                activeCameras.Add(inst);
            }
            newCameras.Clear();

            Log("Rotated daily camera activity");
            return;
        }

        // When any instance finds player submit its cache for evaluation when not on cooldown
        public static void OnCameraFullyNoticed(List<string> seenStateCache, float cacheEvidenceRatio = -1f)
        {
            string seenCacheStr = "";
            seenStateCache.ForEach(x => seenCacheStr += x + " ");
            Log($"Running camera notice events for: {seenCacheStr}");
            Log($"SeenStates evaluating: {_surveilSeenStatesEvaluating}");
            Log($"Crime status evaluating: {_surveilCrimeStatusEvaluating}");

            if (surveillanceConfig.SurveilBaseCrimes && !_surveilSeenStatesEvaluating)
                coros.Add(MelonCoroutines.Start(SurveilSeenStates(seenStateCache, cacheEvidenceRatio)));

            if (surveillanceConfig.SurveilCrimeStatus && !_surveilCrimeStatusEvaluating)
                coros.Add(MelonCoroutines.Start(SurveilCrimeStatus()));

            return;
        }
        public static IEnumerator SurveilSeenStates(List<string> seenStateCache, float cacheEvidenceRatio = -1f)
        {
            _surveilSeenStatesEvaluating = true;
            Log("Evaluate seen states start");
            // So during curfew if the camera notices player what happens?
            // Maybe check nearby officers or dispatch?
            float mult = 1f + Mathf.Clamp01(cacheEvidenceRatio);
            float accumulatedSeverity = 0f;
            foreach (string stateLabel in seenStateCache)
            {
                switch (stateLabel)
                {
                    case "Suspicious":
                        accumulatedSeverity += 0.01f;
                        break;

                    case "DisobeyingCurfew":
                        accumulatedSeverity += 0.15f;
                        Player.Local.CrimeData.AddCrime(new ViolatingCurfew());
                        break;

                    case "Vandalizing":
                        accumulatedSeverity += 0.20f;
                        Player.Local.CrimeData.AddCrime(new Vandalism());
                        break;

                    case "PettyCrime":
                        accumulatedSeverity += 0.05f;
                        break;

                    case "DrugDealing":
                        accumulatedSeverity += 0.30f;
                        Player.Local.CrimeData.AddCrime(new AttemptingToSell());
                        Player.Local.CrimeData.AddCrime(new DrugTrafficking());
                        Player.Local.CrimeData.AddCrime(new TransportingIllicitItems());
                        break;

                    case "Wanted":
                        accumulatedSeverity += 0.75f;
                        Player.Local.CrimeData.AddCrime(new Evading());
                        Player.Local.CrimeData.AddCrime(new FailureToComply());
                        break;

                    case "Pickpocketing":
                        accumulatedSeverity += 0.20f;
                        Player.Local.CrimeData.AddCrime(new Theft());
                        break;

                    case "DischargingWeapon":
                        accumulatedSeverity += 0.45f;
                        Player.Local.CrimeData.AddCrime(new BrandishingWeapon());
                        Player.Local.CrimeData.AddCrime(new DischargeFirearm());
                        break;

                    case "Brandishing":
                        accumulatedSeverity += 0.15f;
                        Player.Local.CrimeData.AddCrime(new BrandishingWeapon());
                        break;

                    default:
                        break;
                }
                accumulatedSeverity = Mathf.Clamp01(accumulatedSeverity * mult);
            }

            Log($"Accumulated severity: {accumulatedSeverity} (x{mult})");
            if (UnityEngine.Random.Range(0f, 0.85f) <= accumulatedSeverity)
            {
                Log("Chance hits!");
                DispatchNearby();
                Log("Dispatch call finished");
            }

            Log("Finished surveil seen states evaluation");
            _surveilSeenStatesEvaluating = false;
            yield break;
        }
        public static IEnumerator SurveilCrimeStatus()
        {
            _surveilCrimeStatusEvaluating = true; 

            if (Player.Local.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
            {
                Log("Record last known position and Reset duration");
                Player.Local.RecordLastKnownPosition(true);
                Player.Local.CrimeData.CurrentPursuitLevelDuration = 0f;
                Player.Local.CrimeData.TimeSincePursuitStart = 0f;
            }
            _surveilCrimeStatusEvaluating = false;
            yield break;
        }

        // When not on cooldown try to find officers to dispatch prioritizing nearby positions
        public static void DispatchNearby()
        {
            if (_hasDispatchedNearby)
            {
                Log("Dispatch is still on cooldown!");
                return;
            }
            coros.Add(MelonCoroutines.Start(WaitDispatchCooldown()));
            Log("Dispatch nearby proceed!");

            bool stationHasOfficers = PoliceStation.PoliceStations[0].OfficerPool.Count > 1;
            // if player is within the vicinity of police station, check that first
            if (Vector3.Distance(Player.Local.CenterPointTransform.position, PoliceStation.PoliceStations[0].transform.position) < 20f)
            {
                // Ensure that it has officers
                if (stationHasOfficers)
                {
                    Log("Dispatch nearby from police station");
                    if (Player.Local.CrimeData.CurrentPursuitLevel == PlayerCrimeData.EPursuitLevel.None)
                        Player.Local.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Investigating);
                    PoliceStation.PoliceStations[0].Dispatch(1, Player.Local, PoliceStation.EDispatchType.OnFoot, true);
                    return;
                }
                else
                {
                    Log("Station has no officers in pool, check nearby");
                }
            }
            Log("Dispatch check nearby");
            // Else check nearby officers take max 2
            float minDistanceToPlayer = 50f;
            float nearestDist = 100f;
            List<PoliceOfficer> selected = new();
            int maxSelectedCount = 2;
            foreach (PoliceOfficer offc in allActiveOfficers)
            {
                if (offc == null || !offc.IsConscious || offc.isInBuilding)
                    continue;

                float distToPlayer = Vector3.Distance(offc.transform.position, Player.Local.CenterPointTransform.position);
                if (distToPlayer > minDistanceToPlayer)
                    continue;

                if (distToPlayer < nearestDist)
                {
                    nearestDist = distToPlayer;
                    if (!selected.Contains(offc))
                        selected.Add(offc);
                    if (selected.Count >= maxSelectedCount)
                        break;
                }
            }
            Log($"Selected count: {selected.Count}");
            if (selected.Count > 0)
            {
                if (Player.Local.CrimeData.CurrentPursuitLevel == PlayerCrimeData.EPursuitLevel.None)
                    Player.Local.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Investigating);

                foreach (PoliceOfficer selectedOffc in selected)
                {
                    if (selectedOffc.Behaviour.activeBehaviour == null) continue;

                    if (selectedOffc.Behaviour.activeBehaviour == selectedOffc.VehiclePatrolBehaviour && selectedOffc.CurrentVehicle != null)
                    {
                        Log("Begin vehicle pursuit of noticed player");
                        selectedOffc.BeginVehiclePursuit_Networked(Player.Local.PlayerCode, selectedOffc.CurrentVehicle.NetworkObject, true);
                    }
                    else if (selectedOffc.Behaviour.activeBehaviour == selectedOffc.PursuitBehaviour)
                    {
                        Log("Reset foot pursuit of noticed player");
                        // update current coord + reset pursuit dur??=
                        selectedOffc.PursuitBehaviour.currentPursuitLevelDuration = 0f;
                        selectedOffc.PursuitBehaviour.currentSearchDestination = Player.Local.Avatar.CenterPoint;
                    }
                    else if (selectedOffc.Behaviour.activeBehaviour == selectedOffc.VehiclePursuitBehaviour)
                    {
                        Log("Reset vehicle pursuit of noticed player");
                        // update current coord + reset pursuit dur??=
                        selectedOffc.VehiclePursuitBehaviour.timeSincePursuitStart = 0f;
                        selectedOffc.VehiclePursuitBehaviour.timeSinceLastSighting = 0f;
                    }
                    else
                    {
                        Log("Begin foot pursuit of noticed player");
                        // sentry, footpatrol behs, follow schedule
                        selectedOffc.BeginFootPursuit_Networked(Player.Local.PlayerCode, true);
                    }
                }

                Log("Finished dispatching nearby");
            }
            else
            {
                Log("Could not find officers nearby to attend to noticed player");
                if (stationHasOfficers && PoliceStation.PoliceStations[0].AvailableVehicleCount > 0)
                {
                    Log("Try dispatch vehicle from station");
                    PoliceStation.PoliceStations[0].Dispatch(1, Player.Local, PoliceStation.EDispatchType.UseVehicle, true);
                }
            }
        }
        public static IEnumerator WaitDispatchCooldown()
        {
            if (_hasDispatchedNearby) yield break;
            _hasDispatchedNearby = true;
            yield return Wait30;
            if (!registered) yield break;
            _hasDispatchedNearby = false;
            Log("Dispatch finished cooldown");
            yield break;
        }

        public static void OnPlayerArrestedClearCache()
        {
            if (!currentConfig.MassSurveillance) return;

            if (activeCameras.Count > 0)
            {
                foreach (HylandFlockInstance cam in activeCameras)
                    cam.ClearSeenCache();
            }
        }
    }

    public class HitComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit x, RaycastHit y)
        {
            bool xHasCollider = x.collider != null;
            bool yHasCollider = y.collider != null;
            if (xHasCollider && !yHasCollider)
            {
                return -1;
            }
            if (!xHasCollider && yHasCollider)
            {
                return 1;
            }
            if (!xHasCollider && !yHasCollider)
            {
                return 0;
            }
            return x.distance.CompareTo(y.distance);
        }
    }


    // Helper class for Overriding the crime fine payments and logic
    [Serializable]
    public class CrimeOverride
    {
        public float fineAmount;
        public string description = string.Empty;

        public CrimeOverride() { }
        public CrimeOverride(Crime crime)
        {
            // Fines based on constants to make them default on read but modifiable by config
            string tableKey = crime.GetType().Name;
            switch (tableKey)
            {
                case "Assault": 
                    fineAmount = PenaltyHandler.ASSAULT_FINE; 
                    break;

                case "AttemptingToSell": 
                    fineAmount = PenaltyHandler.ATTEMPT_TO_SELL_FINE;
                    break;

                case "BrandishingWeapon":
                    fineAmount = PenaltyHandler.BRANDISHING_FINE;
                    break;

                case "DeadlyAssault":
                    fineAmount = PenaltyHandler.DEADLY_ASSAULT_FINE;
                    break;

                case "DischargeFirearm":
                    fineAmount = PenaltyHandler.DISCHARGE_FIREARM_FINE;
                    break;

                case "DrugTrafficking":
                    // None by default, assign one
                    fineAmount = 50f;
                    break;

                case "Evading":
                    fineAmount = PenaltyHandler.EVADING_ARREST_FINE;
                    break;

                case "FailureToComply":
                    fineAmount = PenaltyHandler.FAILURE_TO_COMPLY_FINE;
                    break;

                case "PossessingControlledSubstances":
                    fineAmount = PenaltyHandler.CONTROLLED_SUBSTANCE_FINE;
                    description = "controlled substances";
                    break;

                case "PossessingHighSeverityDrug":
                    fineAmount = PenaltyHandler.HIGH_SEVERITY_DRUG_FINE;
                    description = "high-severity drugs";
                    break;

                case "PossessingLowSeverityDrug":
                    fineAmount = PenaltyHandler.LOW_SEVERITY_DRUG_FINE;
                    description = "low-severity drugs";
                    break;

                case "PossessingModerateSeverityDrug":
                    fineAmount = PenaltyHandler.MED_SEVERITY_DRUG_FINE;
                    description = "moderate-severity drugs";
                    break;

                case "Theft":
                    fineAmount = PenaltyHandler.THEFT_FINE;
                    break;

                case "TransportingIllicitItems":
                    // None by default, assign one
                    fineAmount = 50f;
                    break;

                case "Vandalism":
                    fineAmount = PenaltyHandler.VANDALISM_FINE;
                    break;

                case "VehicularAssault":
                    // None by default, assign one, same as in deadly assault
                    fineAmount = 150f;
                    break;

                case "ViolatingCurfew":
                    fineAmount = PenaltyHandler.VIOLATING_CURFEW_TIME;
                    break;

                default:
                    Log("Failed to find fine amount for " + tableKey, "CrimeOverride");
                    break;
            }
        }
    }

    /// <summary>
    /// Override function for PayFinesFromBank, GrowPaymentWithProgression and CrimePaymentMultiplier config values
    /// </summary>
    [HarmonyPatch(typeof(PenaltyHandler), "ProcessCrimeList")]
    public static class PenaltyHandler_ProcessCrimeList_Patch
    {
        private static readonly string name = "ProcessCrimeList";
        // So that the function doesnt need to use casting, instead string comparison
        public static Dictionary<string, CrimeOverride> crimeTable = new();
        public static void BuildCrimeTable()
        {
            if (crimeTable.Count > 0) return;
            BuildCrimeOverrideOfType(new Assault());
            BuildCrimeOverrideOfType(new AttemptingToSell());
            BuildCrimeOverrideOfType(new BrandishingWeapon());
            BuildCrimeOverrideOfType(new DeadlyAssault());
            BuildCrimeOverrideOfType(new DischargeFirearm());
            BuildCrimeOverrideOfType(new DrugTrafficking());
            BuildCrimeOverrideOfType(new Evading());
            BuildCrimeOverrideOfType(new FailureToComply());
            BuildCrimeOverrideOfType(new PossessingControlledSubstances());
            BuildCrimeOverrideOfType(new PossessingHighSeverityDrug());
            BuildCrimeOverrideOfType(new PossessingLowSeverityDrug());
            BuildCrimeOverrideOfType(new PossessingModerateSeverityDrug());
            BuildCrimeOverrideOfType(new Theft());
            BuildCrimeOverrideOfType(new TransportingIllicitItems());
            BuildCrimeOverrideOfType(new Vandalism());
            BuildCrimeOverrideOfType(new VehicularAssault());
            BuildCrimeOverrideOfType(new ViolatingCurfew());
            Log("Finished building crime table overrides");
        }

        public static void BuildCrimeOverrideOfType(Crime crime)
        {
            CrimeOverride newOverride = new CrimeOverride(crime);
            string tableKey = crime.GetType().Name;
            if (!crimeTable.ContainsKey(tableKey))
                crimeTable.Add(tableKey, newOverride);
            else
                Log("Failed to build crime override with name: " + tableKey);
        }

        // return -> List<string>
#if MONO
        public static bool Prefix(Dictionary<Crime, int> crimes, ref List<string> __result)
#else
        public static bool Prefix(Il2CppSystem.Collections.Generic.Dictionary<Crime, int> crimes, ref Il2CppSystem.Collections.Generic.List<string> __result)
#endif
        {
            Log("Evaluate conditions", name);
            // If the config has nothing enabled then run the original function
            if (crimeTable == null || crimeTable.Count == 0) 
            {
                Log("Crime table is unassigned or empty!", name);
                return true;
            }
            if (surveillanceConfig.CrimePaymentMultiplier == 1 && 
                surveillanceConfig.GrowPaymentsWithProgression == false &&
                surveillanceConfig.PayFinesFromBank == false
            )
            {
                Log("Surveillance config has disabled crime payment modifications, return.", name);
                return true;
            }

            Log("Proceed", name);
            // Else one of the wanted features is active

            __result = new();

            float payment = 0f;
            float multiplier = (float)surveillanceConfig.CrimePaymentMultiplier;
            // if grow with progression override the mult
            if (surveillanceConfig.GrowPaymentsWithProgression)
            {
                // Based on player tier and earnings increase payment mult
                float earningsNorm3mil = Mathf.Clamp01(MoneyManager.Instance.LifetimeEarnings / 3000000f);
                float tierNorm100 = Mathf.Clamp01((float)LevelManager.Instance.Tier / 100f);
                multiplier = Mathf.Lerp(1f, 10f, earningsNorm3mil);
                multiplier += Mathf.Lerp(0f, 10f, tierNorm100);
            }
            Log($"Crime fine Multiplier: {multiplier}", name);

            // Basically same as in the source code but bound to config values
#if MONO
            List<Crime> keys = crimes.Keys.ToList();
#else
            List<Crime> keys = new();
            // Because iterating over the keys in il2cpp is trivial, copy to the array base first
            Il2CppReferenceArray<Il2CppSystem.Collections.Generic.KeyValuePair<Crime, int>> temp = new(crimes.Count);
            crimes.CopyTo(temp, 0);
            for (int i = 0; i < temp.Count; i++)
                keys.Add(temp[i].Key);
#endif
            foreach (Crime crime in keys)
            {
                string tableKey = crime.GetType().Name;
                int crimeCount = crimes[crime];
                if (crimeTable.TryGetValue(tableKey, out CrimeOverride crimeData))
                {
                    // For confiscated items it shows by default the description too
                    // fine amount * crime count * config multiplier
                    if (crimeData.description != string.Empty)
                    {
                        __result.Add($"{crimeCount} {crimeData.description} confiscated");
                        float crimePayment = Mathf.Round(crimeData.fineAmount * crimeCount * multiplier);
                        Log($"{crimeCount} {crimeData.description} confiscated: {crimePayment}");
                        payment += crimePayment;
                    }
                    else
                    {
                        // Else its a crime that can be read into fines only once (e.g. doesnt allow for count)
                        float crimePayment = Mathf.Round(crimeData.fineAmount * multiplier);
                        Log($"{tableKey}: {crimePayment}");
                        payment += crimePayment;
                    }
                }
            }

            // if user config indicates that there should be no payment
            if (surveillanceConfig.CrimePaymentMultiplier == 0 || payment == 0f)
            {
                __result.Add("(No fines issued)");
                return false;
            }

            // Else crime needs to be paid, 
            Log($"Crime payment: {payment}", name);

            // By default game checks for cash balance only
            // Added so that after cash balance, bank balance can be deducted if not sufficient

            float cash = NetworkSingleton<MoneyManager>.Instance.cashBalance;
            float bank = NetworkSingleton<MoneyManager>.Instance.onlineBalance;

            bool useCash = false;
            bool useBank = false;
            float bankRemainder = 0f;

            if (cash >= payment)
                useCash = true;
            else
            {
                useCash = true;
                useBank = true;
                bankRemainder = payment - cash;
            }

            if (useCash && useBank)
            {
                Log("Cash not sufficient");
                if (cash > 0f)
                {
                    NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-cash, true, false);
                    __result.Add($"{MoneyManager.FormatAmount(cash, true, false)} fine (paid in cash)");
                }
                else
                    __result.Add($"{MoneyManager.FormatAmount(payment, true, false)} fine (insufficient cash)");

                if (surveillanceConfig.PayFinesFromBank && bank > 0f)
                {
                    if (bank > 0f)
                    {
                        float bankDeduction = 0f;
                        if (bank >= bankRemainder) // mathf min instead?
                            bankDeduction = bankRemainder;
                        else
                            bankDeduction = bank;

                        NetworkSingleton<MoneyManager>.Instance.CreateOnlineTransaction("Hyland Point Police", -bankDeduction, 1f, "Crime charge");
                        __result.Add($"{MoneyManager.FormatAmount(bankDeduction, true, false)} fine (paid from bank)");
                    }
                    else
                        __result.Add($"{MoneyManager.FormatAmount(bankRemainder, true, false)} fine (insufficient bank balance)");
                }
            }
            else
            {
                Log("Cash sufficient");
                if (cash > 0f)
                {
                    NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(-payment, true, false);
                    __result.Add($"{MoneyManager.FormatAmount(payment, true, false)} fine (paid in cash)");
                }

            }
            // Dont run original as the function handled everything in identical fashion
            return false;
        }


    }

}
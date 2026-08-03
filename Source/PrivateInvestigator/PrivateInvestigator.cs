
using System.Collections;
using MelonLoader;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

using static NACops.BaseUtility;
using static NACops.NACops;
using static NACops.DebugModule;
using static NACops.AvatarUtility;
using static NACops.RuntimeImpostor;
using static NACops.CopInitHelper;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vision;
using ScheduleOne.Map;
using ScheduleOne.DevUtilities;
using ScheduleOne.VoiceOver;
using ScheduleOne.Vehicles.AI;
using ScheduleOne.Police;
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.Audio;
using Pathfinding;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Vision;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.VoiceOver;
using Il2CppScheduleOne.Vehicles.AI;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.Audio;
using Il2CppPathfinding;
#endif

namespace NACops
{
    public static class PrivateInvestigator
    {
        public static readonly List<string> randomMaleNames = new()
        {
            "Danny", "Andrew", "Christopher", "Davey", "Jonathan", "Justin"
        };
        public static readonly List<string> randomFemaleNames = new()
        {
            "Sophie", "Katie", "Holly", "Lucy", "Emily", "Charlotte"
        };
        public static readonly List<EVisualState> PIdisabledVisualStates = new()
        {
            EVisualState.Brandishing, EVisualState.DisobeyingCurfew, EVisualState.DrugDealing, EVisualState.Pickpocketing, EVisualState.Suspicious
        };

        // For when player is inside a property use preset coordinates to make the pi stand near better positions
        public static readonly Dictionary<string, List<(Vector3 Position, Vector3 FacePoint)>> PropertyPreferPositions = new()
        {
            { "motelroom", new List<(Vector3, Vector3)>()
                {
                    (new(-66.52f, 1.69f, 86.2f), new(-68.7f, 1.69f, 85.0f)),
                }
            },
            { "sweatshop", new List<(Vector3, Vector3)>()
                {
                    (new(-61.5f -3.04f, 151.2f), new(-61.9f, -3.04f, 149.2f)),
                }
            },
            { "bungalow", new List<(Vector3, Vector3)>()
                {
                    (new(-177.3f, -3.04f, 110.5f), new(-175.1f, -2.74f, 112.1f)),
                }
            },
            { "storageunit", new List<(Vector3, Vector3)>()
                {
                    (new(-2.2f, 1.07f, 94.7f), new(-2.9f, 1.07f, 95.9f))
                }
            },
            { "dockswarehouse", new List<(Vector3, Vector3)>()
                {
                    (new(-82.2f, -1.5f, -36.2f), new(-84.0f, -1.5f, -37.7f)),
                    (new(-79.0f, -1.5f, -57.7f), new(-83.9f, -1.3f, -55.4f))
                }
            },
            { "barn", new List<(Vector3, Vector3)>()
                {
                    (new(174.5f, 0.98f, -15.6f), new(176.4f, 0.97f, -13.7f)),
                    (new(173.4f, 0.98f, -4.6f), new(177.5f, 0.96f, -6.6f)),
                }
            },
            { "manor", new List<(Vector3, Vector3)>()
                {
                    (new(151.9f, 10.98f, -61.1f), new(153.8f, 11.5f, -60.7f)),
                    (new(175.4f, 10.96f, -52.7f), new(173.0f, 11.5f, -53.3f)),
                }
            },
        };


        public static float maxInvestigationTime = 240f;

        private static float minWait;
        private static float maxWait;

        private static List<WaitForSeconds> randWaits;
        private static WaitForSeconds currentAwait;

        private static int playerLayer = -1;
        private static int obstacleLayerMask = -1;


        public static bool investigatorActive = false;

        public static bool isTakingPhotos = false;
        // Zap clip for pphotos
        public static AudioClip cameraZapClip;
        public static AudioSource PICameraAudio;
        public static Light PICameraLight;
        public static LensFlareDataElementSRP PILensFlare;
        // DestroyImmediate on scriptable after
        public static LensFlareDataSRP scriptableFlareData;

        public static IEnumerator RunInvestigator()
        {

            Log("Private Investigator evaluating");
            playerLayer = LayerMask.NameToLayer("Player");
            obstacleLayerMask = LayerMask.GetMask("Terrain", "Default", "Vehicle");
            // zap clip setup
            AvatarRangedWeapon wep = null;
#if MONO
            wep = PoliceOfficer.Officers[0].TaserPrefab as AvatarRangedWeapon;
#else
            wep = PoliceOfficer.Officers[0].TaserPrefab.TryCast<AvatarRangedWeapon>();
#endif
            if (wep != null)
            {
                RandomizedAudioSourceController audioController = null;
#if MONO
                audioController = wep.FireSound as RandomizedAudioSourceController;
#else
                audioController = wep.FireSound.TryCast<RandomizedAudioSourceController>();
#endif
                if (audioController != null)
                {
                    cameraZapClip = audioController.Clips[0];
                }
            }

            (minWait, maxWait) = ThresholdUtils.Evaluate(thresholdConfig.PIFrequency, (int)MoneyManager.Instance.LifetimeEarnings);
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
                if (!currentConfig.PrivateInvestigator) continue;

                // if threshold has changed update awaits now
                float newMin;
                float newMax;
                (newMin, newMax) = ThresholdUtils.Evaluate(thresholdConfig.PIFrequency, NetworkSingleton<TimeManager>.Instance.ElapsedDays);
                if (newMin != minWait || newMax != maxWait)
                {
                    randWaits.Clear();
                    for (int i = 0; i < 3; i++)
                        randWaits.Add(new WaitForSeconds(UnityEngine.Random.Range(newMin, newMax)));

                    minWait = newMin;
                    maxWait = newMax;
                }

                Log("PI Evaluate");

                EDay currentDay = NetworkSingleton<TimeManager>.Instance.CurrentDay;
                if (currentDay.ToString().Contains("Saturday") || currentDay.ToString().Contains("Sunday"))
                    continue;

                if (investigatorActive)
                    continue;

                Log("PI Proceed");

                coros.Add(MelonCoroutines.Start(HandlePIMonitor()));
            }
        }

        public static bool IsNearRoadNode(Vector3 pos = default)
        {
            Log("Check road graph");
            Vector3 checkedPos = Vector3.zero;
            if (pos != default)
                checkedPos = pos;
            else
                checkedPos = investigator.transform.position;
#if MONO
            List<NodeLink> links = NodeLink.GetClosestLinks(checkedPos, 1);
#else
            Il2CppSystem.Collections.Generic.List<NodeLink> links = NodeLink.GetClosestLinks(checkedPos, 1);
#endif
            float dist = 0f;
            if (links != null && links.Count > 0)
            {
                Vector3 start = new(links[0].Start2D.x, links[0].midPosition.y, links[0].Start2D.y);
                Vector3 end = new(links[0].End2D.x, links[0].midPosition.y, links[0].End2D.y);
                Vector3 closest = NavigationUtility.GetClosestPointOnFiniteLine(checkedPos, start, end);
                dist = Vector3.Distance(checkedPos, closest);
                Log($"Closest dist: {dist} | too close: {dist < 3f} ");
            }
            return dist < 3f;
        }

        public static IEnumerator HandlePIMonitor()
        {
            investigatorActive = true;
            investigator.gameObject.SetActive(true);
            investigator.transform.Find("Avatar").gameObject.SetActive(true);
            investigator.GetComponent<NavMeshAgent>().enabled = true;
            if (!investigator.Movement.IsPaused)
                investigator.Movement.PauseMovement();
            investigator.Awareness.SetAwarenessActive(true);

            yield return PIAvatar(investigator);
            SetVOEmitter(investigator);
            yield return AttemptWarp(investigator, Player.Local.CenterPointTransform);
            investigator.Movement.ResumeMovement();
            investigator.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, 0.15f));

            float elapsed = 0f;
            int proximityDelta = 0;
            int sightedAmount = 0;
            float maxWarpCd = 15f;
            float lastWarp = 0f;
            bool canSeePlayerCurrently = false;

            float timeSinceLastTurn = 0f;

            bool isMakingCall = false;
            float phoneHeldInHandSecs = 0f;
            bool didFinishMakingACall = false;

            float photosTakenInSecs = 0f;
            bool didFinishTakingPhotos = false;

            bool isMovingPrioPos = false;
            bool isInPrioPos = false;
            Vector3 prioPos = default;
            Vector3 prioRot = default;
            float timeSpentMovingToPrio = 0f;

            float timeSinceMonitorReposition = 0f;

            // When the investigator initiates combat
            // the visual state starts bugging
            // visual cone needs to be disabled
            UnityEngine.Events.UnityAction afterAction = null;
            void AfterInvestigation()
            {
                if (afterAction != null)
                    investigator.PursuitBehaviour.onBegin.RemoveListener(afterAction);
                else
                    return;
                investigator.Awareness.SetAwarenessActive(false);
                afterAction = null;
                if (!(investigator.Health.IsDead || investigator.Health.IsKnockedOut))
                {
                    if (investigator.Awareness.VisionCone.enabled)
                        investigator.Awareness.SetAwarenessActive(false);

                    // if active beh is combat
                    if (investigator.Behaviour.activeBehaviour != null && (investigator.Behaviour.activeBehaviour == investigator.Behaviour.CombatBehaviour || investigator.Behaviour.activeBehaviour == investigator.PursuitBehaviour))
                    {
                        investigator.PursuitBehaviour.Disable();
                    }

                    investigator.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, 0.85f));
                    investigator.Movement.SetDestination(PoliceStation.PoliceStations[0].Doors[0].AccessPoint);
                    if (investigator.Movement.IsPaused)
                        investigator.Movement.ResumeMovement();
                }
            }
            afterAction = (UnityEngine.Events.UnityAction)AfterInvestigation;

            investigator.PursuitBehaviour.onBegin.AddListener(afterAction);

            // str propertycode , int investigation Delta in prperty
            Dictionary<string, int> sightedProperties = new();
            bool shouldWaitRandom = false;
            for (; ; )
            {
                if (!shouldWaitRandom)
                    yield return Wait2;
                else if (shouldWaitRandom)
                    yield return Wait5;

                if (!registered) yield break;

                float distance = Vector3.Distance(investigator.transform.position, Player.Local.transform.position);


                float distanceRelativeSpeed = Mathf.Lerp(0.1f, 0.55f, Mathf.Clamp01(distance / 80f));
                investigator.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, distanceRelativeSpeed));
                Log($"({distance}m) PI Speed now: " + investigator.Movement.SpeedController.ActiveSpeedControl.speed);
                // check exit condition
                if (!CanPIProceed(elapsed, distance))
                    break;

                float waitedAmount = 2f;
                if (shouldWaitRandom)
                {
                    waitedAmount = 5f;
                    shouldWaitRandom = false;
                }
                lastWarp += waitedAmount;
                elapsed += waitedAmount;
                if (investigator.Movement.IsPaused)
                    timeSinceLastTurn += waitedAmount;
                if (isMakingCall)
                    phoneHeldInHandSecs += waitedAmount;
                if (isTakingPhotos)
                    photosTakenInSecs += waitedAmount;
                if (distance <= 26f && !isMakingCall && !isTakingPhotos && !isMovingPrioPos)
                    timeSinceMonitorReposition += waitedAmount;
                if (isMovingPrioPos)
                    timeSpentMovingToPrio += waitedAmount;

                if (didFinishMakingACall)
                    didFinishMakingACall = false;
                if (didFinishTakingPhotos)
                    didFinishTakingPhotos = false;

                if (investigator.Awareness.VisionCone.enabled && investigator.Awareness.VisionCone.IsPlayerVisible(Player.Local))
                {
                    canSeePlayerCurrently = true;
                    sightedAmount += 1;
                }
                else
                    canSeePlayerCurrently = false;

                if (isMovingPrioPos)
                {
                    Log("Move to Prio pos!");

                    if (timeSpentMovingToPrio >= 20f)
                    {
                        isInPrioPos = false;
                        isMovingPrioPos = false;
                        timeSpentMovingToPrio = 0f;
                    }
                    else if (investigator.Movement.HasDestination && investigator.Movement.CurrentDestination == prioPos)
                    {
                        Log("Still travelling to prio pos...");
                    }
                    else if ((!investigator.Movement.HasDestination || !investigator.Movement.IsMoving) && Vector3.Distance(investigator.CenterPoint, prioPos) > 2f)
                    {
                        Log("Reset traverse to the prio pos");
                        investigator.Movement.SetDestination(prioPos);
                        if (investigator.Movement.IsPaused)
                            investigator.Movement.ResumeMovement();
                    }
                    else if (!investigator.Movement.HasDestination && !investigator.Movement.IsMoving)
                    {
                        if (Vector3.Distance(investigator.CenterPoint, prioPos) < 1.85f)
                        {
                            Log("Now in priority position!");
                            investigator.Movement.FacePoint(prioRot, lerpTime: 0.5f);
                            isInPrioPos = true;
                            isMovingPrioPos = false;
                            timeSpentMovingToPrio = 0f;
                            Log("Start taking photos");
                            isTakingPhotos = true;
                            MelonCoroutines.Start(StartTakePhotos());
                        }
                        else
                        {
                            Log("Not nearby prio pos!");
                        }
                    }
                    continue;
                }

                if (isMakingCall)
                {
                    Log("Attending call...");
                    proximityDelta += 1;
                    if (phoneHeldInHandSecs >= UnityEngine.Random.Range(10f, 20f) || distance > 35f)
                    {
                        Log("End taking a call");
                        didFinishMakingACall = true;
                        isMakingCall = false;
                        investigator.SetEquippable_Client(null, string.Empty);
                        phoneHeldInHandSecs = 0f;
                    }
                    else if (phoneHeldInHandSecs <= 5f)
                    {
                        investigator.PlayVO(EVOLineType.Greeting);
                    }
                    else
                    {
                        int randVoice = UnityEngine.Random.Range(0, 4);
                        if (UnityEngine.Random.Range(0, 4) == 0)
                        {
                            switch (randVoice)
                            {
                                case 0:
                                    investigator.PlayVO(EVOLineType.Surprised);
                                    break;

                                case 1:
                                    investigator.PlayVO(EVOLineType.Acknowledge);
                                    break;

                                case 2:
                                    investigator.PlayVO(EVOLineType.No);
                                    break;

                                case 3:
                                    investigator.PlayVO(EVOLineType.Question);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    if (isMakingCall)
                        continue;
                }

                if (isTakingPhotos)
                {
                    Log("Taking video...");
                    sightedAmount += 1;
                    if (!investigator.Movement.IsPaused)
                        investigator.Movement.PauseMovement();

                    if (isInPrioPos)
                    {
                        investigator.Movement.FacePoint(prioRot, lerpTime: 2f);
                        if (Player.Local.CurrentProperty != null && distance < 26f)
                        {
                            Log("++Evidence!");
                            if (sightedProperties.ContainsKey(Player.Local.CurrentProperty.PropertyCode))
                                sightedProperties[Player.Local.CurrentProperty.PropertyCode]++;
                            else
                                sightedProperties.Add(key: Player.Local.CurrentProperty.PropertyCode, value: 1);
                        }
                    }
                    else
                    {
                        investigator.Movement.FacePoint(Player.Local.transform.position, lerpTime: 2f);
                    }

                    if (UnityEngine.Random.Range(0, 3) == 0)
                        MelonCoroutines.Start(SnapPhotoSimulated());

                    float randTime = isInPrioPos ? UnityEngine.Random.Range(20f, 40f) : UnityEngine.Random.Range(12f, 20f);

                    if (photosTakenInSecs >= randTime || distance > 35f)
                    {
                        Log("End taking Photos");
                        isTakingPhotos = false;
                        didFinishTakingPhotos = true;
                        investigator.Avatar.Animation.SetBool("UseSprayCan", false);
                        yield return Wait05;

                        investigator.SetEquippable_Client(null, string.Empty);
                        UnityEngine.Object.DestroyImmediate(scriptableFlareData);
                        PICameraAudio = null;
                        PICameraLight = null;
                        PILensFlare = null;
                        scriptableFlareData = null;
                        investigator.Avatar.LookController.OverrideIKWeight(0.2f);
                        photosTakenInSecs = 0f;

                        if (isInPrioPos)
                        {
                            isInPrioPos = false;
                            prioPos = default;
                            prioRot = default;
                        }
                    }
                    if (isTakingPhotos)
                        continue;
                }

                if (distance >= 80f && distance < 120f)
                {
                    Log("PI Should Warp - dist " + distance);
                    if (lastWarp < maxWarpCd)
                    {
                        if (!investigator.Movement.HasDestination)
                        {
                            investigator.Movement.GetClosestReachablePoint(Player.Local.CenterPointTransform.position, out Vector3 pos);
                            if (pos != Vector3.zero)
                                investigator.Movement.SetDestination(pos);
                        }
                        continue;
                    }
                    Log("PI Try Warp - dist " + distance);
                    investigator.Movement.PauseMovement();
                    yield return AttemptWarp(investigator, Player.Local.CenterPointTransform);
                    investigator.Movement.ResumeMovement();
                    Log("PI New dist " + distance);
                    lastWarp = 0f;
                }
                else if (distance >= 26f && distance < 80f)
                {
                    Vector3 targetPosition = SampleNearby(Player.Local.CenterPointTransform.position);
                    investigator.Movement.GetClosestReachablePoint(targetPosition, out Vector3 pos);
                    if (pos == Vector3.zero || IsNearRoadNode(pos)) continue;
                    // At larger distances disregard the sight, simply travel
                    Vector3 currentDestination = Vector3.zero;
                    if (investigator.Movement.HasDestination)
                        currentDestination = investigator.Movement.CurrentDestination;
                    else
                        currentDestination = investigator.CenterPoint;
                    float oldDestToPlayer = Vector3.Distance(currentDestination, Player.Local.CenterPointTransform.position);
                    float newDestToPlayer = Vector3.Distance(pos, Player.Local.CenterPointTransform.position);
                    bool isBetterPos = oldDestToPlayer > newDestToPlayer && oldDestToPlayer - newDestToPlayer > 8f;

                    if (distance > 55f)
                    {
                        Log("PI Traverse - dist " + distance);
                        if (investigator.Movement.IsPaused || !investigator.Movement.HasDestination)
                        {
                            investigator.Movement.SetDestination(pos);
                            investigator.Movement.ResumeMovement();
                        }
                        else
                        {
                            // refresh pos only when its closer to player
                            if (isBetterPos)
                                investigator.Movement.SetDestination(pos);
                        }
                        shouldWaitRandom = true;
                        continue;
                    }

                    // pos is valid for monitoring
                    bool canSee = CanSeeFromPosition(pos, Player.Local.CenterPointTransform.position, newDestToPlayer);
                    // If the player was not visible and
                    // the new proposed locations distance to player is smaller than current distance to player
                    // OR
                    // the new proposed location has guaranteed sightline to player
                    
                    if ((isBetterPos && UnityEngine.Random.Range(0, 4) == 0) || !investigator.Movement.HasDestination || canSee)
                    {
                        Log($"PI Traversing Better Distance:{isBetterPos} | noDest: {!investigator.Movement.HasDestination} | Can See:{canSee}");
                        if (investigator.Movement.IsPaused)
                            investigator.Movement.ResumeMovement();
                        investigator.Movement.SetDestination(pos);
                        shouldWaitRandom = UnityEngine.Random.Range(0, 2) == 0;
                    }
                }
                else if (distance <= 26f)
                {
                    Log("PI Monitoring");
                    // If player is visible or random player is in property or distance small enough
                    // pause movement while nearby. Should allow it to get closer to player and relocate for sightline.
                    bool shouldStop = false;
                    if (canSeePlayerCurrently || Player.Local.CurrentProperty != null || (distance <= 8f && UnityEngine.Random.Range(0, 3) == 0))
                    {
                        if (!investigator.Movement.IsPaused || investigator.Movement.HasDestination)
                        {
                            shouldStop = UnityEngine.Random.Range(0, 5) == 0;
                            if (shouldStop)
                            {
                                if (!IsNearRoadNode())
                                {
                                    Log("Pause nearby!");
                                    investigator.Movement.PauseMovement();
                                }
                                else
                                {
                                    Log("Pause: Cant stop near a road node!");
                                    shouldStop = false;
                                }
                            }
                        }
                    }

                    proximityDelta += 1;
                    if (Player.Local.CurrentProperty != null)
                    {
                        if (sightedProperties.ContainsKey(Player.Local.CurrentProperty.PropertyCode))
                            sightedProperties[Player.Local.CurrentProperty.PropertyCode]++;
                        else
                            sightedProperties.Add(key: Player.Local.CurrentProperty.PropertyCode, value: 1);
                    }

                    // For when current property that has priority positions
                    if (Player.Local.CurrentProperty != null && !shouldStop && UnityEngine.Random.Range(0, 100) == 0)
                    {
                        if (PropertyPreferPositions.ContainsKey(Player.Local.CurrentProperty.PropertyCode))
                        {
                            Log("Start moving priority pos");
                            isMovingPrioPos = true;
                            List<(Vector3, Vector3)> randomPositions = PropertyPreferPositions[Player.Local.CurrentProperty.PropertyCode];
                            (Vector3, Vector3) randomTuple = randomPositions[UnityEngine.Random.Range(0, randomPositions.Count)];
                            prioPos = randomTuple.Item1;
                            prioRot = randomTuple.Item2;
                            investigator.Movement.SetDestination(prioPos);
                            if (investigator.Movement.IsPaused)
                                investigator.Movement.ResumeMovement();
                            continue;
                        }
                    }

                    // For when the investigator stands too close or in camera with chance, but not on the same eval cycle as it has stopped
                    bool shouldReposition = false;
                    if (!shouldStop)
                    {
                        if ((Player.Local.IsPointVisibleToPlayer(investigator.CenterPoint, 12f, 0.1f) && UnityEngine.Random.Range(0, 3) == 0) || distance < 6f)
                        {
                            shouldReposition = true;
                            Log("reposition now");
                        }
                    }

                    shouldWaitRandom = UnityEngine.Random.Range(0, 4) == 0;
                    bool shouldFacePlayer = ((distance > 6f && UnityEngine.Random.Range(0, 2) == 0) || timeSinceLastTurn >= 6f) && !investigator.Movement.IsMoving;
                    if (shouldFacePlayer && !shouldWaitRandom)
                    {
                        timeSinceLastTurn = 0f;
                        investigator.Movement.FacePoint(Player.Local.transform.position, lerpTime: 0.7f);
                    }
                    else if (shouldFacePlayer && shouldWaitRandom)
                    {
                        investigator.Movement.FacePoint(Player.Local.transform.position, lerpTime: 1.5f);
                        timeSinceLastTurn = 0f;
                    }

                    if (!shouldReposition && !investigator.Movement.IsMoving)
                    {
                        if (!isMakingCall && !isTakingPhotos && !didFinishMakingACall)
                        {
                            bool shouldMakeACall = UnityEngine.Random.Range(0, 80) == 0;
                            if (shouldMakeACall)
                            {
                                Log("Start making a call");
                                isMakingCall = true;
                                investigator.SetEquippable_Client(null, investigator.Behaviour.CallPoliceBehaviour.PhonePrefab.AssetPath);
                                continue;
                            }
                        }

                        if (!isMakingCall && !isTakingPhotos && !didFinishTakingPhotos)
                        {
                            bool shouldTakeVideo = UnityEngine.Random.Range(0, 100) == 0;
                            if (shouldTakeVideo)
                            {
                                Log("Start taking a video");
                                isTakingPhotos = true;
                                MelonCoroutines.Start(StartTakePhotos());
                                continue;
                            }
                        }
                    }

                    // If player not in building OR 20% chance (while player in building to relocate)
                    // check if a random position would have smaller distance OR Vision to target (If in building, never vision only distance)
                    bool condition1 = !canSeePlayerCurrently && (Player.Local.CurrentProperty == null || UnityEngine.Random.Range(0, 10) == 0);
                    if (condition1 || shouldReposition)
                    {
                        Vector3 targetPosition = SampleNearby(Player.Local.CenterPointTransform.position);
                        investigator.Movement.GetClosestReachablePoint(targetPosition, out Vector3 pos);
                        if (pos == Vector3.zero || IsNearRoadNode(pos)) continue;

                        float newDistance = Vector3.Distance(pos, Player.Local.CenterPointTransform.position);
                        bool canSee = CanSeeFromPosition(pos, Player.Local.CenterPointTransform.position, newDistance);
                        bool playerCantSeePos = Player.Local.IsPointVisibleToPlayer(pos, 30f, 0.1f);
                        bool conditionalReposition = shouldReposition && newDistance > 4f && (playerCantSeePos || UnityEngine.Random.Range(0, 4) == 0);
                        if (conditionalReposition || timeSinceMonitorReposition > 20f)
                        {
                            Log($"PI Repositioning");
                            if (investigator.Movement.IsPaused)
                                investigator.Movement.ResumeMovement();
                            investigator.Movement.SetDestination(pos);
                            shouldWaitRandom = true;
                            timeSinceMonitorReposition = 0f;
                        }
                        else if (canSee || (!investigator.Movement.HasDestination && !investigator.Movement.IsMoving && UnityEngine.Random.Range(0, 10) == 0))
                        {
                            Log($"PI Traversing Better position.");
                            if (investigator.Movement.IsPaused)
                                investigator.Movement.ResumeMovement();
                            investigator.Movement.SetDestination(pos);
                            timeSinceMonitorReposition = 0f;
                        }
                    }
                }
            }

            if (isMakingCall)
                investigator.SetEquippable_Client(null, string.Empty);

            if (isTakingPhotos)
            {
                investigator.Avatar.Animation.SetBool("UseSprayCan", false);
                investigator.SetEquippable_Client(null, string.Empty);
                investigator.Avatar.LookController.OverrideIKWeight(0.2f);
                isTakingPhotos = false;
            }

            if (sightedProperties.Count > 0)
            {
                lock (heatConfigLock)
                {
                    foreach (PropertyHeat propHeat in heatConfig)
                    {
                        if (sightedProperties.ContainsKey(propHeat.propertyCode))
                        {
                            PlayerCrimeData.EPursuitLevel lastPursuitLevel = Player.Local.CrimeData.CurrentPursuitLevel;
                            int investigationDelta = sightedProperties[propHeat.propertyCode];
                            float investigationMultiplier = 1f;
                            switch(lastPursuitLevel)
                            {
                                case PlayerCrimeData.EPursuitLevel.Arresting:
                                    investigationMultiplier = 1.2f;
                                    break;
                                case PlayerCrimeData.EPursuitLevel.NonLethal:
                                    investigationMultiplier = 1.45f;
                                    break;
                                case PlayerCrimeData.EPursuitLevel.Lethal:
                                    investigationMultiplier = 1.7f;
                                    break;
                            }
                            // If player spent major time in building and was sighted outside atleast once
                            // And is still inside the same building at the end
                            if (investigationDelta >= 30 && proximityDelta > 35 && sightedAmount >= 10 && Player.Local.CurrentProperty != null && Player.Local.CurrentProperty.PropertyCode == propHeat.propertyCode)
                            {
                                Log("Property heat increased +++");
                                propHeat.propertyHeat += Mathf.RoundToInt((UnityEngine.Random.Range(6f, 9f) * investigationMultiplier));
                            }

                            // else if player spent time inside, and was sighted outside atleast once
                            // and PI has sighted outside, and is still in the same property
                            else if (investigationDelta >= 16 && proximityDelta > 20 && sightedAmount >= 10 && Player.Local.CurrentProperty != null && Player.Local.CurrentProperty.PropertyCode == propHeat.propertyCode)
                            {
                                propHeat.propertyHeat += Mathf.RoundToInt((UnityEngine.Random.Range(4f, 6f) * investigationMultiplier));
                                Log("Property heat increased ++");
                            }

                            // else if the property heat is low enough,
                            // Player was nearby in property atleast twice,
                            // And player was sighted atleast 2 times
                            else if (propHeat.propertyHeat < 8 && investigationDelta >= 10 && proximityDelta >= 20 && sightedAmount >= 10)
                            {
                                propHeat.propertyHeat += Mathf.RoundToInt((UnityEngine.Random.Range(2f, 4f) * investigationMultiplier));
                                Log("Property heat increased +");
                            }

                            // else if the property heat is high enough, PI was alive for atleast 1min, player was nearby atleast 4 times 
                            // so the meta is to not be sighted by the PI if you were inside a building
                            // or kill the PI after 1min?
                            else if (propHeat.propertyHeat > 5 && elapsed > 60f && proximityDelta > 20 && sightedAmount >= 8)
                            {
                                propHeat.propertyHeat -= Mathf.RoundToInt((UnityEngine.Random.Range(1f, 5f) * investigationMultiplier));
                                Log("Property heat decreased");
                            }
                        }
                    }
                }
            }


            Log("PI Finished");
            Log("Sighted amnt: " + sightedAmount);
            Log("Proximity delta: " + proximityDelta);

            Log("Investigation:");
            foreach (KeyValuePair<string, int> kvp in sightedProperties)
            {
                Log($"{kvp.Key} - Investigation delta: {kvp.Value}");
            }

            AfterInvestigation();

            yield return Wait30;
            if (!registered) yield break;
            Log("Despawning PI");

            // Also revive when needed but does it return it back to station by default need to test
            // Also does need awareness?

            if (!investigator.IsConscious)
                investigator.Health.Revive();

            investigator.Awareness.SetAwarenessActive(false);

            investigator.gameObject.SetActive(false);

            investigator.transform.Find("Avatar").gameObject.SetActive(false);

            if (!investigator.Movement.IsPaused)
                investigator.Movement.PauseMovement();
            investigator.GetComponent<NavMeshAgent>().enabled = false;


            try
            {
                // If impostor texture exists destroy texture
                if (createdTextures.ContainsKey(investigatorID))
                {
                    if (createdTextures[investigatorID] != null)
                        UnityEngine.Object.Destroy(createdTextures[investigatorID]);
                    createdTextures.Remove(investigatorID);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error(ex);
            }
            isTakingPhotos = false;
            investigatorActive = false;
        }


        public static IEnumerator StartTakePhotos()
        {
            Log("Equip phone");
            investigator.SetEquippable_Client(null, "Avatar/Equippables/Phone_Lowered");
            yield return Wait01;
            if (!registered)
                yield break;

            investigator.Avatar.Animation.SetBool("RightArm_HoldPhone_Lowered", false);

            yield return Wait01;
            if (!registered)
                yield break;

            investigator.Avatar.Animation.SetBool("UseSprayCan", true);

            investigator.Avatar.CurrentEquippable.transform.localPosition = new(0.0001f, 0.0009f, 0.0008f);
            investigator.Avatar.CurrentEquippable.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Log("Equip done, setup components");

            GameObject cameraSoundObj = new("Sound");
            cameraSoundObj.transform.SetParent(investigator.Avatar.CurrentEquippable.transform);
            cameraSoundObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            PICameraAudio = cameraSoundObj.AddComponent<AudioSource>();
            PICameraAudio.maxDistance = 7f;
            PICameraAudio.minDistance = 1f;
            PICameraAudio.pitch = 3f;
            PICameraAudio.spatialize = true;
            PICameraAudio.spatialBlend = 1f;
            PICameraAudio.spread = 0.23f;
            PICameraAudio.rolloffMode = AudioRolloffMode.Linear;
            PICameraAudio.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
            PICameraAudio.volume = 0.07f;
            PICameraAudio.clip = cameraZapClip;
            Log("Camera Light");

            Transform phoneCamera = investigator.Avatar.CurrentEquippable.transform.Find("phone/Camera");
            GameObject cameraLightObj = new("CamLight");
            cameraLightObj.transform.SetParent(phoneCamera);
            cameraLightObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            PICameraLight = cameraLightObj.AddComponent<Light>();
            PICameraLight.innerSpotAngle = 70f;
            PICameraLight.intensity = 0f; // default 2f at max
            PICameraLight.range = 12f;
            PICameraLight.shadows = LightShadows.Soft;
            PICameraLight.spotAngle = 140f;
            PICameraLight.type = LightType.Spot;

            Log("Lens Flare ");

            LensFlareComponentSRP flareComp = cameraLightObj.AddComponent<LensFlareComponentSRP>();

            scriptableFlareData = ScriptableObject.CreateInstance<LensFlareDataSRP>();

            PILensFlare = new LensFlareDataElementSRP();
            PILensFlare.count = 5;
            PILensFlare.edgeOffset = 1f;
            PILensFlare.fallOff = 0.3f;
            PILensFlare.intensityVariation = 0.1f;
            PILensFlare.localIntensity = 0f; // lerp up log max 3f
            PILensFlare.uniformScale = 0f; // lerp up log max 2f
            PILensFlare.sdfRoundness = 0.3f;
            PILensFlare.sideCount = 6;
            PILensFlare.lengthSpread = 3f;

            PILensFlare.enableRadialDistortion = true;
            PILensFlare.flareType = SRPLensFlareType.Polygon;
            PILensFlare.tint = new Color(1f, 1f, 1f, 0.5f);
            PILensFlare.sizeXY = new Vector2(5f, 5f);
            PILensFlare.targetSizeDistortion = new Vector2(12f, 12f);
            PILensFlare.blendMode = SRPLensFlareBlendMode.Additive;
            scriptableFlareData.elements = new LensFlareDataElementSRP[1] { PILensFlare };

            flareComp.lensFlareData = scriptableFlareData;
            flareComp.intensity = 1f;
            flareComp.maxAttenuationDistance = 12f;
            flareComp.maxAttenuationScale = 3f;
            flareComp.useOcclusion = true;
            flareComp.volumetricCloudOcclusion = true;
            flareComp.scale = 1.0f;

            investigator.Avatar.LookController.OverrideIKWeight(0.3f);
            Log("Done Setup");
            yield break;
        }

        public static IEnumerator SnapPhotoSimulated()
        {
            if (PICameraLight == null || PILensFlare == null || PICameraAudio == null) yield break;

            Log("Light start");
            float elapsed = 0f;
            float lightUpTime = 0.2f;

            float camMaxLight = 2f;
            float flareMaxIntensity = 3f;
            float flareMaxScale = 2f;

            float t = 0f;

            while (elapsed < lightUpTime && registered)
            {
                if (!isTakingPhotos) break;

                elapsed += Time.deltaTime;
                t = elapsed / lightUpTime;
                PICameraLight.intensity = Mathf.Lerp(0f, camMaxLight, t * t);
                PILensFlare.localIntensity = Mathf.Lerp(0f, flareMaxIntensity, t * t);
                PILensFlare.uniformScale = Mathf.Lerp(0f, flareMaxScale, t * t);
                yield return frameEnd;
            }
            if (isTakingPhotos)
            {
                PICameraAudio.Play();
                PICameraLight.intensity = 0f;
                PILensFlare.localIntensity = 0f;
                PILensFlare.uniformScale = 0f;
            }
            Log("Photo taken");
            yield break;
        }

        public static bool CanSeeFromPosition(Vector3 pos, Vector3 target, float distance)
        {
            Vector3 origin = pos + Vector3.up * 1.75f; // so its not at the feet level
            Vector3 direction = target - origin;
            RaycastHit hit;
            if (Physics.Raycast(origin, direction.normalized, out hit, distance + 2f))
            {
                if ((obstacleLayerMask & 1 << hit.collider.gameObject.layer) != 0)
                {
                    Log("New Destination cannot see");
                    return false;
                }
                else if (hit.collider.gameObject.layer == playerLayer)
                {
                    Log("New Destination can see");
                    return true;
                }
            }
            else
            {
                Log("No Raycast hits for sightline check");
            }
            return false;
        }

        public static bool CanPIProceed(float timeElapsed, float distance)
        {
            if (!investigator.Movement.CanMove() || timeElapsed >= maxInvestigationTime || Player.Local.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
                return false;

            if (distance >= 120f || !investigator.Movement.CanGetTo(Player.Local.transform.position, proximityReq: 120f))
                return false;

            return true;
        }
        public static Vector3 SampleNearby(Vector3 target)
        {
            float xOffset = UnityEngine.Random.Range(6f, 24f);
            float zOffset = UnityEngine.Random.Range(6f, 24f);
            xOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
            zOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
            return target + new Vector3(xOffset, 0f, zOffset);
        }
    }

}
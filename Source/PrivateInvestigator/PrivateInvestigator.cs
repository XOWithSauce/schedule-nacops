
using System.Collections;
using MelonLoader;
using UnityEngine;

using static NACopsV1.BaseUtility;
using static NACopsV1.NACops;
using static NACopsV1.DebugModule;
using static NACopsV1.OfficerOverrides;
using static NACopsV1.AvatarUtility;
using static NACopsV1.RuntimeImpostor;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.Vision;
using ScheduleOne.Map;
using ScheduleOne.DevUtilities;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Vision;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.DevUtilities;
#endif

namespace NACopsV1
{
    public static class PrivateInvestigator
    {
        public static readonly List<string> randomMaleNames = new()
        {
            "James", "William", "David", "Richard", "John", "Robert"
        };
        public static readonly List<string> randomFemaleNames = new()
        {
            "Jessica", "Susan", "Linda", "Mary"
        };
        public static readonly List<EVisualState> PIdisabledVisualStates = new()
        {
            EVisualState.Brandishing, EVisualState.DisobeyingCurfew, EVisualState.DrugDealing, EVisualState.Pickpocketing, EVisualState.Suspicious
        };


        public static float maxInvestigationTime = 240f;

        private static float minWait;
        private static float maxWait;

        private static List<WaitForSeconds> randWaits;
        private static WaitForSeconds currentAwait;

        private static int playerLayer = -1;
        private static int obstacleLayerMask = -1;

        public static List<int> investigatorObjectIDs = new();

        public static IEnumerator RunInvestigator()
        {
            Log("Private Investigator Enabled");
            playerLayer = LayerMask.NameToLayer("Player");
            obstacleLayerMask = LayerMask.GetMask("Terrain", "Default", "Vehicle");

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

                // if threshold has changed update awaits now
                float newMin;
                float newMax;
                (newMin, newMax) = ThresholdUtils.Evaluate(thresholdConfig.PIFrequency, NetworkSingleton<TimeManager>.Instance.ElapsedDays);
                if (newMin != minWait || newMax != maxWait)
                {
                    randWaits.Clear();
                    for (int i = 0; i < 3; i++)
                        randWaits.Add(new WaitForSeconds(UnityEngine.Random.Range(newMin, newMax)));
                }

                Log("PI Evaluate");

                EDay currentDay = NetworkSingleton<TimeManager>.Instance.CurrentDay;
                if (currentDay.ToString().Contains("Saturday") || currentDay.ToString().Contains("Sunday"))
                    continue;

                if (currentPICount >= 1)
                    continue;

                Log("PI Proceed");

                coros.Add(MelonCoroutines.Start(HandlePIMonitor()));
            }
        }

        public static IEnumerator HandlePIMonitor()
        {
            Player randomPlayer = Player.GetRandomPlayer();
            currentPICount += 1;

            PoliceOfficer offc = SpawnOfficerRuntime(false);
            offc.Behaviour.ScheduleManager.DisableSchedule();
            offc.Movement.PauseMovement();
            currentSummoned.Add(offc);
            investigatorObjectIDs.Add(offc.transform.root.gameObject.GetInstanceID());
            offc.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, 0.55f));
            offc.ChatterEnabled = false;

            yield return PIAvatar(offc);

            yield return AttemptWarp(offc, randomPlayer.CenterPointTransform);
            offc.Movement.ResumeMovement();

            float elapsed = 0f;
            int proximityDelta = 0;
            int sightedAmount = 0;
            float maxWarpCd = 15f;
            float lastWarp = 0f;
            bool canSeePlayerCurrently = false;

            // When the investigator initiates combat
            // the visual state starts bugging
            // visual cone needs to be disabled
            UnityEngine.Events.UnityAction afterAction = null;
            void AfterInvestigation()
            {
                if (afterAction != null)
                    offc.PursuitBehaviour.onBegin.RemoveListener(afterAction);
                else
                    return;
                offc.Awareness.SetAwarenessActive(false);
                afterAction = null;
                if (!(offc.Health.IsDead || offc.Health.IsKnockedOut))
                {
                    if (offc.Awareness.VisionCone.enabled)
                        offc.Awareness.SetAwarenessActive(false);

                    // if active beh is combat
                    if (offc.Behaviour.activeBehaviour != null && (offc.Behaviour.activeBehaviour == offc.Behaviour.CombatBehaviour || offc.Behaviour.activeBehaviour == offc.PursuitBehaviour))
                    {
                        offc.PursuitBehaviour.Disable();
                    }

                    offc.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, 0.85f));
                    offc.Movement.SetDestination(PoliceStation.PoliceStations[0].Doors[0].AccessPoint);
                    if (offc.Movement.IsPaused)
                        offc.Movement.ResumeMovement();
                }
            }
            afterAction = (UnityEngine.Events.UnityAction)AfterInvestigation;

            offc.PursuitBehaviour.onBegin.AddListener(afterAction);

            // Remove the required visual states
            offc.Awareness.enabled = false;
#if MONO
            ISightable sightable = (ISightable)randomPlayer;
            Dictionary<EVisualState, VisionCone.StateContainer> newStates = new();
#else
            ISightable sightable = randomPlayer.TryCast<ISightable>();
            Il2CppSystem.Collections.Generic.Dictionary<EVisualState, VisionCone.StateContainer> newStates = new();
#endif
            if (sightable == null)
            {
                Log("Warning sightable is null");
            }
            else
            {
                if (offc.Awareness.VisionCone.stateSettings.ContainsKey(sightable))
                {
                    foreach (var kvp in offc.Awareness.VisionCone.stateSettings[sightable])
                    {
                        if (PIdisabledVisualStates.Contains(kvp.Key))
                            continue;
                        else
                            newStates.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            if (newStates.Count == 0)
            {
                Log("Something failed while applying visual state modification");
            }
            else if (sightable != null)
            {
                offc.Awareness.VisionCone.stateSettings[sightable] = newStates;
            }
            offc.Awareness.enabled = true;
            // str propertycode , int investigation Delta in prperty
            Dictionary<string, int> sightedProperties = new();
            for (; ; )
            {
                yield return Wait5;
                if (!registered) yield break;

                float distance = Vector3.Distance(offc.transform.position, randomPlayer.transform.position);

                if (!offc.Movement.CanMove() || elapsed >= maxInvestigationTime || randomPlayer.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
                    break;

                elapsed += 5f;
                lastWarp += 5f;

                if (offc.Awareness.VisionCone.enabled && offc.Awareness.VisionCone.IsPlayerVisible(randomPlayer))
                {
                    canSeePlayerCurrently = true;
                    sightedAmount += 1;
                }
                else
                    canSeePlayerCurrently = false;

                if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 90f && distance < 150f)
                {
                    Log("PI Should Warp - dist " + distance);
                    if (lastWarp < maxWarpCd) continue;
                    Log("PI Try Warp - dist " + distance);
                    offc.Movement.PauseMovement();
                    yield return AttemptWarp(offc, randomPlayer.CenterPointTransform);
                    offc.Movement.ResumeMovement();
                    Log("PI New dist " + distance);
                    lastWarp = 0f;
                }
                else if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 25f && distance < 110f)
                {
                    Vector3 targetPosition = SampleNearby(randomPlayer.CenterPointTransform.position);
                    offc.Movement.GetClosestReachablePoint(targetPosition, out Vector3 pos);
                    if (pos == Vector3.zero) continue;
                    // At larger distances disregard the sight, simply travel
                    if (distance > 55f)
                    {
                        Log("PI Traverse - dist " + distance);
                        if (offc.Movement.IsPaused)
                            offc.Movement.ResumeMovement();
                        offc.Movement.SetDestination(pos);
                        continue;
                    }
                    // pos is valid for monitoring
                    float newDistance = Vector3.Distance(pos, randomPlayer.CenterPointTransform.position);
                    bool canSee = CanSeeFromPosition(pos, randomPlayer.CenterPointTransform.position, newDistance);
                    // If the player was not visible and
                    // the new proposed locations distance to player is smaller than current distance to player
                    // OR
                    // the new proposed location has guaranteed sightline to player
                    if ((!canSeePlayerCurrently && newDistance < distance) || canSee)
                    {
                        Log($"PI Traversing Better Distance:{(!canSeePlayerCurrently && newDistance < distance)} | Can See:{canSee}");
                        if (offc.Movement.IsPaused)
                            offc.Movement.ResumeMovement();
                        offc.Movement.SetDestination(pos);
                    }
                }
                else if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance <= 25f)
                {
                    Log("PI Monitoring");
                    // If player is visible or random player is in property or distance small enough
                    // pause movement while nearby. Should allow it to get closer to player and relocate for sightline.
                    if (canSeePlayerCurrently || randomPlayer.CurrentProperty != null || distance <= 15f)
                        if (!offc.Movement.IsPaused)
                            offc.Movement.PauseMovement();

                    offc.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.3f);

                    if (UnityEngine.Random.Range(0f, 1f) > 0.95f)
                        coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 1, player: randomPlayer)));

                    proximityDelta += 1;
                    if (randomPlayer.CurrentProperty != null)
                    {
                        if (sightedProperties.ContainsKey(randomPlayer.CurrentProperty.PropertyCode))
                            sightedProperties[randomPlayer.CurrentProperty.PropertyCode]++;
                        else
                            sightedProperties.Add(key: randomPlayer.CurrentProperty.PropertyCode, value: 1);
                    }

                    // If player not in building OR 20% chance (while player in building to relocate)
                    // check if a random position would have smaller distance OR Vision to target (If in building, never vision only distance)
                    if (!canSeePlayerCurrently && (randomPlayer.CurrentProperty == null || UnityEngine.Random.Range(0f, 1f) > 0.8f))
                    {
                        Vector3 targetPosition = SampleNearby(randomPlayer.CenterPointTransform.position);
                        offc.Movement.GetClosestReachablePoint(targetPosition, out Vector3 pos);
                        if (pos == Vector3.zero) continue;

                        float newDistance = Vector3.Distance(pos, randomPlayer.CenterPointTransform.position);
                        bool canSee = CanSeeFromPosition(pos, randomPlayer.CenterPointTransform.position, newDistance);
                        if (newDistance < distance || canSee)
                        {
                            Log($"PI Traversing Better Distance:{newDistance < distance} | Can See:{canSee}");
                            if (offc.Movement.IsPaused)
                                offc.Movement.ResumeMovement();
                            offc.Movement.SetDestination(pos);
                        }
                    }
                }
                else
                {
                    Log("PI Exit condition");
                    break;
                }
            }

            if (sightedProperties.Count > 0)
            {
                lock (heatConfigLock)
                {
                    foreach (PropertyHeat propHeat in heatConfig)
                    {
                        if (sightedProperties.ContainsKey(propHeat.propertyCode))
                        {
                            PlayerCrimeData.EPursuitLevel lastPursuitLevel = randomPlayer.CrimeData.CurrentPursuitLevel;
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
                            if (investigationDelta >= 12 && proximityDelta > 4 && sightedAmount >= 1 && randomPlayer.CurrentProperty != null && randomPlayer.CurrentProperty.PropertyCode == propHeat.propertyCode)
                            {
                                Log("Property heat increased +++");
                                propHeat.propertyHeat += Mathf.RoundToInt((UnityEngine.Random.Range(6f, 9f) * investigationMultiplier));
                            }

                            // else if player spent time inside, and was sighted outside atleast once
                            // and PI has sighted outside, and is still in the same property
                            else if (investigationDelta >= 6 && proximityDelta > 2 && sightedAmount >= 1 && randomPlayer.CurrentProperty != null && randomPlayer.CurrentProperty.PropertyCode == propHeat.propertyCode)
                            {
                                propHeat.propertyHeat += Mathf.RoundToInt((UnityEngine.Random.Range(4f, 6f) * investigationMultiplier));
                                Log("Property heat increased ++");
                            }

                            // else if the property heat is low enough,
                            // Player was nearby in property atleast twice,
                            // And player was sighted atleast 2 times
                            else if (propHeat.propertyHeat < 8 && investigationDelta >= 3 && proximityDelta >= 2 && sightedAmount >= 2)
                            {
                                propHeat.propertyHeat += Mathf.RoundToInt((UnityEngine.Random.Range(2f, 4f) * investigationMultiplier));
                                Log("Property heat increased +");
                            }

                            // else if the property heat is high enough, PI was alive for atleast 1min, player was nearby atleast 4 times 
                            // so the meta is to not be sighted by the PI if you were inside a building
                            // or kill the PI after 1min?
                            else if (propHeat.propertyHeat > 5 && elapsed > 60f && proximityDelta > 4)
                            {
                                propHeat.propertyHeat += Mathf.RoundToInt((UnityEngine.Random.Range(1f, 5f) * investigationMultiplier));
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
            try
            {
                // If impostor texture exists destroy texture
                int id = offc.GetInstanceID();
                if (createdTextures.ContainsKey(id))
                {
                    if (createdTextures[id] != null)
                        UnityEngine.Object.Destroy(createdTextures[id]);
                    createdTextures.Remove(id);
                }

                if (currentSummoned.Contains(offc))
                    currentSummoned.Remove(offc);
                currentPICount -= 1;
                NPC npc = offc.gameObject.GetComponent<NPC>();
                if (npc != null && NPCManager.NPCRegistry.Contains(npc))
                    NPCManager.NPCRegistry.Remove(npc);
                if (npc != null && npc.gameObject != null)
                    UnityEngine.Object.Destroy(npc.gameObject);
                
            }
            catch (Exception ex)
            {
                MelonLogger.Error(ex);
            }
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

using System.Collections;
using MelonLoader;
using UnityEngine;

using static NACopsV1.BaseUtility;
using static NACopsV1.NACops;
using static NACopsV1.DebugModule;
using static NACopsV1.OfficerOverrides;
using static NACopsV1.AvatarUtility;

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
        public static float maxInvestigationTime = 240f;

        private static float minWait;
        private static float maxWait;

        private static List<WaitForSeconds> randWaits;
        private static WaitForSeconds currentAwait;

        private static int playerLayer = -1;
        private static int obstacleLayerMask = -1;
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

                currentPICount += 1;
                Log("PI Proceed");

                coros.Add(MelonCoroutines.Start(HandlePIMonitor()));
            }
        }

        public static IEnumerator HandlePIMonitor()
        {
            Player randomPlayer = Player.Local; // todo network?

            PoliceOfficer offc = SpawnOfficerRuntime(false);
            offc.Behaviour.ScheduleManager.DisableSchedule();
            offc.Movement.PauseMovement();
            offc.Movement.Warp(Singleton<Map>.Instance.PoliceStation.Doors[0].AccessPoint);
            currentSummoned.Add(offc);
            offc.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, 0.55f));
            offc.ChatterEnabled = false;
            coros.Add(MelonCoroutines.Start(PIAvatar(offc)));

            yield return AttemptWarp(offc, randomPlayer.CenterPointTransform);
            offc.Movement.ResumeMovement();

            float elapsed = 0f;
            int proximityDelta = 0;
            int sightedAmount = 0;
            float maxWarpCd = 15f;
            float lastWarp = 0f;
            bool sightableDisabled = false;
            bool canSeePlayerCurrently = false;
            // if open carry weapons is enabled then must prevent brandishing from triggering
            if (currentConfig.NoOpenCarryWeapons)
                offc.Awareness.VisionCone.SetSightableStateEnabled(randomPlayer.GetComponent<ISightable>(), EVisualState.Brandishing, false);

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

                // During curfew disable so it doesnt immeaditely set arrested
                if (NetworkSingleton<TimeManager>.Instance.CurrentTime > 2100 || NetworkSingleton<TimeManager>.Instance.CurrentTime < 0500) 
                {
                    offc.Awareness.VisionCone.SetSightableStateEnabled(randomPlayer.GetComponent<ISightable>(), EVisualState.DisobeyingCurfew, false);
                    sightableDisabled = true;
                }
                else if (sightableDisabled)
                    offc.Awareness.VisionCone.SetSightableStateEnabled(randomPlayer.GetComponent<ISightable>(), EVisualState.DisobeyingCurfew, true);

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
                            int investigationDelta = sightedProperties[propHeat.propertyCode];

                            // If player spent major time in building and was sighted outside atleast once
                            // And is still inside the same building at the end
                            if (investigationDelta >= 12 && randomPlayer.CurrentProperty != null && randomPlayer.CurrentProperty.PropertyCode == propHeat.propertyCode && sightedAmount >= 1)
                            {
                                Log("Property heat increased majorly");
                                propHeat.propertyHeat += UnityEngine.Random.Range(6, 9);
                            }

                            // else if player spent time inside, not in any property at end
                            // and PI has sighted twice outside
                            else if (investigationDelta >= 4 && randomPlayer.CurrentProperty == null && sightedAmount >= 5)
                            {
                                propHeat.propertyHeat += UnityEngine.Random.Range(4, 6);
                                Log("Property heat increased");
                            }

                            // else if the property heat is low enough,
                            // Player was nearby in property atleast twice,
                            // And player was sighted atleast 10 times
                            else if (propHeat.propertyHeat < 8 && investigationDelta >= 2 && sightedAmount >= 10)
                            {
                                propHeat.propertyHeat += UnityEngine.Random.Range(2, 4);
                                Log("Property heat increased");
                            }

                            // else if the property heat is high enough, PI was alive for atleast 1min, player was nearby atleast 4 times 
                            else if (propHeat.propertyHeat > 5 && elapsed > 60f && proximityDelta > 4)
                            {
                                propHeat.propertyHeat -= UnityEngine.Random.Range(1, 5);
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
                Log($"{kvp.Key} - {kvp.Value}");
            }

            if (!(offc.Health.IsDead || offc.Health.IsKnockedOut))
            {
                // if active beh is combat
                if (offc.Behaviour.activeBehaviour != null && (offc.Behaviour.activeBehaviour == offc.Behaviour.CombatBehaviour || offc.Behaviour.activeBehaviour == offc.PursuitBehaviour))
                {
                    offc.PursuitBehaviour.EndCombat();
                }
            }

            if (offc.Movement.CanMove())
            {
                offc.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, 0.85f));
                offc.Movement.SetDestination(PoliceStation.PoliceStations[0].Doors[0].AccessPoint);
            }

            yield return Wait30;
            Log("Despawning PI");
            try
            {
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
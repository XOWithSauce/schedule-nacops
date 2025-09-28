
using System.Collections;
using MelonLoader;
using UnityEngine;
using UnityEngine.AI;

using static NACopsV1.BaseUtility;
using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Money;
using ScheduleOne.NPCs;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Police;
using ScheduleOne.VoiceOver;
using static ScheduleOne.AvatarFramework.AvatarSettings;
using FishNet.Object;

#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.VoiceOver;
using static Il2CppScheduleOne.AvatarFramework.AvatarSettings;
using Il2CppFishNet.Object;
#endif



namespace NACopsV1
{
    public static class PrivateInvestigator
    {

        private static float minWait;
        private static float maxWait;

        private static List<WaitForSeconds> randWaits;
        private static WaitForSeconds currentAwait;

        public static IEnumerator RunInvestigator()
        {
            Log("Private Investigator Enabled");
            float maxTime = 120f;

            (minWait, maxWait) = ThresholdUtils.Evaluate(ThresholdMappings.PIThres, (int)MoneyManager.Instance.LifetimeEarnings);
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
                Log("PI Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                if (players.Length == 0)
                    continue;

                EDay currentDay = TimeManager.Instance.CurrentDay;
                if (currentDay.ToString().Contains("Saturday") || currentDay.ToString().Contains("Sunday"))
                    continue;

                if (currentPICount >= 1)
                    continue;

                currentPICount += 1;
                Log("PI Proceed");

                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];

                NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(policeBase);
                NPC myNpc = copNet.gameObject.GetComponent<NPC>();
                NavMeshAgent nav = copNet.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (nav != null && nav.enabled == false) nav.enabled = true;

                myNpc.ID = $"NACop_{Guid.NewGuid()}";
                myNpc.FirstName = $"NACop_{Guid.NewGuid()}";
                myNpc.LastName = "";
                myNpc.transform.parent = NPCManager.Instance.NPCContainer;

                NPCManager.NPCRegistry.Add(myNpc);
                yield return Wait01;
                networkManager.ServerManager.Spawn(copNet);
                copNet.gameObject.SetActive(true);
                PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
                currentSummoned.Add(offc);
                offc.FootPatrolBehaviour.SendEnable();
                offc.Movement.WalkSpeed = 5f;
                offc.Movement.RunSpeed = 8f;

                coros.Add(MelonCoroutines.Start(PIAvatar(offc)));

                Vector3 warpInit = Vector3.zero;
                int maxWarpAttempts = 14;
                for (int i = 0; i < maxWarpAttempts; i++)
                {
                    float xInitOffset = UnityEngine.Random.Range(8f, 30f);
                    float zInitOffset = UnityEngine.Random.Range(8f, 30f);
                    xInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                    zInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                    Vector3 targetWarpPosition = randomPlayer.transform.position + new Vector3(xInitOffset, 0f, zInitOffset);
                    offc.Movement.GetClosestReachablePoint(targetWarpPosition, out warpInit);
                    if (warpInit != Vector3.zero)
                    {
                        yield return Wait01;
                        offc.Movement.Warp(warpInit);
                        break;
                    }
                    yield return Wait01;
                }

                float elapsed = 0f;
                int investigationDelta = 0;
                int proximityDelta = 0;
                int sightedAmount = 0;
                float maxWarpCd = 30f;
                float lastWarp = 0f;
                for (; ; )
                {
                    float distance = Vector3.Distance(offc.transform.position, randomPlayer.transform.position);

                    if (offc.Behaviour.activeBehaviour != null && offc.Behaviour.activeBehaviour.ToString().ToLower().Contains("schedule"))
                        offc.FootPatrolBehaviour.SendEnable();

                    if (!offc.Movement.CanMove() || elapsed >= maxTime || randomPlayer.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
                        break;

                    lastWarp += Time.deltaTime;

                    if (UnityEngine.Random.Range(0f, 1f) >= 0.98f)
                        offc.PlayVO(EVOLineType.PoliceChatter);

                    if (TimeManager.Instance.CurrentTime > 2100 || TimeManager.Instance.CurrentTime < 0500) // During curfew
                    {
                        if (offc.Awareness.VisionCone.VisionEnabled)
                            offc.Awareness.VisionCone.VisionEnabled = false;
                        // Based on prog, roll random chance for enable
                        (float minRang, float maxRang) = ThresholdUtils.Evaluate(ThresholdMappings.PICurfewAttn, TimeManager.Instance.ElapsedDays);
                        if (UnityEngine.Random.Range(minRang, maxRang) < 0.5f)
                            offc.Awareness.VisionCone.VisionEnabled = true;
                    }
                    else if (!offc.Awareness.VisionCone.VisionEnabled)
                        offc.Awareness.VisionCone.VisionEnabled = true;

                    if (offc.Awareness.VisionCone.VisionEnabled && offc.Awareness.VisionCone.IsPlayerVisible(randomPlayer))
                        sightedAmount += 1;

                    if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 90f && distance < 140)
                    {
                        Log("PI Warp - dist " + distance);
                        if (lastWarp < maxWarpCd) continue;

                        coros.Add(MelonCoroutines.Start(AttemptWarp(offc, randomPlayer.transform)));
                        lastWarp = 0f;
                    }
                    else if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 25f && distance < 140)
                    {
                        Log("PI Traverse - dist " + distance);

                        if (offc.Movement.IsPaused)
                            offc.Movement.ResumeMovement();
                        float xOffset = UnityEngine.Random.Range(6f, 24f);
                        float zOffset = UnityEngine.Random.Range(6f, 24f);
                        xOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                        zOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;

                        Vector3 targetPosition = randomPlayer.transform.position + new Vector3(xOffset, 0f, zOffset);
                        offc.Movement.GetClosestReachablePoint(targetPosition, out Vector3 pos);
                        if (pos != Vector3.zero && Vector3.Distance(pos, randomPlayer.transform.position) < distance)
                            offc.Behaviour.activeBehaviour.SetDestination(pos);
                    }
                    else if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance <= 25f)
                    {
                        Log("PI Monitoring");
                        if (!offc.Movement.IsPaused)
                            offc.Movement.PauseMovement();
                        offc.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.9f);

                        if (UnityEngine.Random.Range(0f, 1f) > 0.95f)
                            coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 1, player: randomPlayer)));

                        proximityDelta += 1;
                        if (randomPlayer.CurrentProperty != null && UnityEngine.Random.Range(0f, 1f) > 0.1f && randomPlayer.CurrentProperty.PropertyName == "Docks Warehouse")
                            investigationDelta += 1;
                    }
                    else
                    {
                        Log("PI Exit condition");
                        break;
                    }

                    yield return Wait5;
                    if (!registered) yield break;
                    elapsed += 5f;
                }

                // Evaluate result, if 40 sec spent observing in docks, and player still in property add 6-8
                if (investigationDelta >= 8 && randomPlayer.CurrentProperty != null && randomPlayer.CurrentProperty.PropertyName == "Docks Warehouse" && sightedAmount >= 1)
                    sessionPropertyHeat += UnityEngine.Random.Range(6, 9);
                // else if player spent major amount of time inside warehouse, not in any property, and PI has sighted twice outside, add 1-3
                else if (investigationDelta >= 12 && randomPlayer.CurrentProperty == null && sightedAmount > 2)
                    sessionPropertyHeat += UnityEngine.Random.Range(1, 4);
                // else if the property heat is high enough, PI was alive for atleast 1min, player was nearby atleast 4 times and was sighted atleast once, reduce 1-3
                else if (sessionPropertyHeat > 3 && elapsed > 60f && proximityDelta > 4 && sightedAmount >= 1)
                    sessionPropertyHeat -= UnityEngine.Random.Range(1, 4);

                Log("PI Finished");
                Log("Investigation delta: " + investigationDelta);
                Log("Sighted amnt: " + sightedAmount);
                Log("Proximity delta: " + proximityDelta);
                Log("Session Heat ->" + sessionPropertyHeat);
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

        }

        public static IEnumerator PIAvatar(PoliceOfficer offc)
        {
            var originalBodySettings = offc.Avatar.CurrentSettings.BodyLayerSettings;
#if MONO
            List<LayerSetting> bodySettings = new();
#else
            Il2CppSystem.Collections.Generic.List<LayerSetting> bodySettings = new();
#endif
            foreach (var layer in originalBodySettings)
            {
                bodySettings.Add(new LayerSetting
                {
                    layerPath = layer.layerPath,
                    layerTint = layer.layerTint
                });
            }

            var originalAccessorySettings = offc.Avatar.CurrentSettings.AccessorySettings;
#if MONO
            List<AccessorySetting> accessorySettings = new();
#else
            Il2CppSystem.Collections.Generic.List<AccessorySetting> accessorySettings = new();
#endif
            foreach (var acc in originalAccessorySettings)
            {
                accessorySettings.Add(new AccessorySetting
                {
                    path = acc.path,
                    color = acc.color
                });
            }

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

            offc.Avatar.CurrentSettings.BodyLayerSettings = bodySettings;
            offc.Avatar.CurrentSettings.AccessorySettings = accessorySettings;
            offc.Avatar.ApplyBodyLayerSettings(offc.Avatar.CurrentSettings);
            offc.Avatar.ApplyAccessorySettings(offc.Avatar.CurrentSettings);
            if (offc.Avatar.onSettingsLoaded != null)
                offc.Avatar.onSettingsLoaded.Invoke();

            yield return null;
        }

    }
}
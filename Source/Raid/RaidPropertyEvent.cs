using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using static NACopsV1.DebugModule;
using static NACopsV1.NACops;
using static NACopsV1.OfficerOverrides;
using static NACopsV1.AvatarUtility;

#if MONO
using ScheduleOne.Core.Items.Framework;
using ScheduleOne.Vision;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Employees;
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.DevUtilities;
using ScheduleOne.EntityFramework;
using ScheduleOne.Persistence;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.UI;
using ScheduleOne.ObjectScripts;
using ScheduleOne.VoiceOver;
using ScheduleOne.Police;
using ScheduleOne.Property;
using ScheduleOne.Storage;
using ScheduleOne.NPCs.Behaviour;
using TMPro;
#else
using Il2CppScheduleOne.Core.Items.Framework;
using Il2CppScheduleOne.Vision;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.VoiceOver;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppTMPro;
using Il2CppInterop.Runtime;
#endif

namespace NACopsV1
{
    public static class RaidPropertyEvent 
    {
        public static readonly List<EVisualState> raiderDisabledVisualStates = new()
        {
            EVisualState.Suspicious, EVisualState.DrugDealing, EVisualState.Pickpocketing
        };

        public static readonly int maxSearchAttempts = 8; // Search from buildable items attempts
        public static readonly int maxActionIters = 12; // for each time the role tries to start action
        public static Sprite m1911Sprite;
        public static Sprite homeSprite;

        public static Slider raidSlider;
        public static TextMeshProUGUI raidText;
        public static RectTransform fillRt;
        public static RectTransform handleRt;
        public static Image sliderFillImage;
        public static Image homeImage;
        public static Image handleGunImage;
        public static CanvasGroup notificationGroup;

        // During active track state
        public static bool raidActive = false;
        private static int raidOfficersAlive = 3;
        public static List<int> raidOfficerObjIDs = new();
        private static List<RaidOfficer> currentRaidOfficers = new();
        private static List<RaidOfficer> deadRaidOfficers = new();
        private static List<RaidOfficer> officersArrived = new();
        private static bool officersReady = false;
        private static bool employeesScared = false;
        private static Dictionary<RaidOfficer, float> distancesToProperty = new();

        // Ensure no 2 raiders pick the same item for destruction
        private static object toBeDestroyedLock = new object();
        // Hashset instance id
        private static HashSet<int> toBeDestroyed = new();
        private static HashSet<int> searchedContainers = new();

        public enum ERaidType
        {
            PropertyRaid, BusinessRaid, Other
        }
        public enum EOfficerRaidRole
        {
            Undecided, DestroyGrowEquipment, DestroyLabEquipment, SearchContainer
        }
        public enum ERaidDestroyType
        {
            None, DestroyPot, DestroyShroomBed, DestroyDryingRack, DestroyLabOven, DestroyChemistryStation, DestroyCauldron, DestroyMixing
        }

        public static readonly List<string> raidOfficerLines = new()
        {
            "We are shutting this business down!", 
            "Get on the ground!", 
            "Hyland Point Police! Drop your weapons!",
            "We are raiding this property! Do not resist!",
            "Put your hands in the air!",
            "We have a cease and desist order!"
        };

        public class RaidOfficer
        {
            public string Name;
            public PoliceOfficer officer;
            public EOfficerRaidRole role;
            public Property targetProperty;
            public BuildableItem currentTargetObj;
            public int destroyedItems = 0; // built if destroy and in containers cleared if search
            public int currentActionIter = 0; // for all action iters, prevent infinite
        }

        public static void OnDayPassEvaluateRaid()
        {
            coros.Add(MelonCoroutines.Start(WaitDayPass()));
        }

        public static IEnumerator WaitDayPass()
        {
            yield return Wait5;
            if (!registered) yield break;
#if MONO
            yield return new WaitUntil(() => !isSaving && !SaveManager.Instance.IsSaving);
#else
            yield return new WaitUntil((Il2CppSystem.Func<bool>)(() => !isSaving && !SaveManager.Instance.IsSaving));
#endif
            if (!registered) yield break;
            yield return Wait5;
            if (!registered) yield break;

            Log("Sleep ended Evaluate Raid");
            List<PropertyHeat> currentHeats;

            lock (heatConfigLock)
            {
                foreach (Property property in Property.OwnedProperties)
                {
                    PropertyHeat heat = heatConfig.Find(x => x.propertyCode == property.propertyCode);
                    if (heat != null)
                    {
                        heat.daysSinceLastRaid++;
                        // over 12 heat decreases
                        if (heat.propertyHeat > 12)
                            heat.propertyHeat--;
                    }
                }
                currentHeats = new(heatConfig);
                if (currentHeats == null)
                {
                    Log("Failed to update heats");
                    yield break;
                }
#if MONO
                currentHeats.Shuffle();
#else
                // il2cpp cant shuffle so no randomness there
#endif
                string selectedCode = string.Empty;
                // Check all properties for heat and days
                for (int i = currentHeats.Count - 1; i >= 0; i--)
                {
                    if (!(currentHeats[i].daysSinceLastRaid >= raidConfig.DaysUntilCanRaid && currentHeats[i].propertyHeat >= raidConfig.PropertyHeatThreshold))
                        continue;
#if MONO
                    Property selected = Property.OwnedProperties.Find(x => x.PropertyCode == currentHeats[i].propertyCode);
#else
                    Property selected = Property.OwnedProperties.Find((Il2CppSystem.Predicate<Property>)(x => x.PropertyCode == currentHeats[i].propertyCode));
#endif
                    if (IsPropertyValidForRaid(selected))
                    {
                        selectedCode = selected.propertyCode;
                        coros.Add(MelonCoroutines.Start(BeginRaidEvent(selected)));
                        break;
                    }
                }
                // If raid started reset the values in persistent data
                if (selectedCode != string.Empty)
                {
                    PropertyHeat heat = heatConfig.Find(x => x.propertyCode == selectedCode);
                    if (heat != null)
                    {
                        heat.daysSinceLastRaid = 0;
                        heat.propertyHeat = 0;
                    }
                }
            }
            yield return null;
        }

        public static IEnumerator BeginRaidEvent(Property property)
        {
            if (!raidActive)
            {
                raidActive = true;
                SpawnRaidCops(property);
                yield return Wait1;
                Log($"Sending raid to {property.propertyName} ({property.propertyCode})");
                coros.Add(MelonCoroutines.Start(TraverseCurrentToProperty(property)));
            }
            else
            {
                Log("Raid is already active");
            }
            yield return null;
        }
        public static void ResetRaidEvent(bool resetUI = false)
        {
            raidActive = false;
            currentRaidOfficers.Clear();
            deadRaidOfficers.Clear();
            raidOfficersAlive = raidConfig.RaidCopsCount;
            officersArrived.Clear();
            officersReady = false;
            employeesScared = false;
            distancesToProperty.Clear();
            raidOfficerObjIDs.Clear();

            if (resetUI) // Reusable while in scene
            {
                raidText = null;
                homeSprite = null;
                m1911Sprite = null;
                raidSlider = null;
                fillRt = null;
                handleRt = null;
                sliderFillImage = null;
                homeImage = null;
                handleGunImage = null;
                notificationGroup = null;
            }

            lock (toBeDestroyedLock)
            {
                toBeDestroyed.Clear();
                searchedContainers.Clear();
            }
        }

        #region Linear Logic for Raid
        // Police station spawn group
        public static void SpawnRaidCops(Property targetProperty)
        {
            // Spawn multiple to the station
            for (int i = 0; i < raidConfig.RaidCopsCount; i++)
            {
                PoliceOfficer offc = SpawnOfficerRuntime(autoDeactivate: false);
                currentSummoned.Add(offc);
                RaidOfficer raidOfficer = new RaidOfficer()
                {
                    Name = offc.name,
                    officer = offc,
                    role = EOfficerRaidRole.Undecided,
                    targetProperty = targetProperty
                };
                offc.Behaviour.ScheduleManager.DisableSchedule();
                offc.Movement.PauseMovement();
                offc.Movement.Agent.areaMask = 57;
                offc.Movement.Warp(Singleton<Map>.Instance.PoliceStation.Doors[0].AccessPoint);
                coros.Add(MelonCoroutines.Start(SetRaiderAvatar(offc)));
                currentRaidOfficers.Add(raidOfficer);
               

                offc.Health.MaxHealth = 240f;
                offc.Health.Health = 240f;
                offc.Behaviour.CombatBehaviour.SetWeapon(offc.GunPrefab != null ? offc.GunPrefab.AssetPath : string.Empty);
#if MONO
                AvatarRangedWeapon wep = offc.Behaviour.CombatBehaviour.currentWeapon as AvatarRangedWeapon;
#else
                AvatarRangedWeapon wep = offc.Behaviour.CombatBehaviour.currentWeapon.TryCast<AvatarRangedWeapon>();
#endif
                wep.CanShootWhileMoving = true;
                wep.Damage = 65f;
                wep.CooldownDuration = 0.6f;
                wep.MinUseRange = 0.1f;
                wep.MaxFireRate = 0.8f;

                offc.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, raidConfig.TraverseToPropertySpeed));

                // Remove the required visual states
                raidOfficerObjIDs.Add(offc.transform.root.gameObject.GetInstanceID());

                offc.Awareness.enabled = false;
#if MONO
                ISightable sightable = (ISightable)Player.Local;
                Dictionary<EVisualState, VisionCone.StateContainer> newStates = new();
#else
                ISightable sightable = Player.Local.TryCast<ISightable>();
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
                            if (raiderDisabledVisualStates.Contains(kvp.Key))
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
            }
            AssignRaidOfficerRole(targetProperty);
            raidOfficersAlive = raidConfig.RaidCopsCount;
        }
        // Assign roles
        public static void AssignRaidOfficerRole(Property property)
        {
            // When officer count is divisible by 3 then increase officers max per role
            int maxOfficersPerRole = 2 * Mathf.Max(1, Mathf.RoundToInt((float)raidConfig.RaidCopsCount / 3f));

            Dictionary<EOfficerRaidRole, int> assignedCounts = new()
            {
                { EOfficerRaidRole.SearchContainer, 0 },
                { EOfficerRaidRole.DestroyGrowEquipment, 0 },
                { EOfficerRaidRole.DestroyLabEquipment, 0 },
            };

            Dictionary<EOfficerRaidRole, int> totalForRole = GetTotalForRoles(property);

            Log("Role Select");
            foreach (RaidOfficer offc in currentRaidOfficers)
            {
                List<EOfficerRaidRole> availableRoles = assignedCounts
                    .Where(role => role.Value < maxOfficersPerRole)
                    .Select(role => role.Key)
                    .ToList();

                if (availableRoles == null || availableRoles.Count == 0)
                {
                    Log("All roles have been fulfilled for raid, cant assign role");
                    break;
                }

                EOfficerRaidRole selectedRole = totalForRole
                        .Where(x => assignedCounts[x.Key] < maxOfficersPerRole)
                        .OrderByDescending(x => x.Value)
                        .FirstOrDefault().Key;

                offc.role = selectedRole;
                totalForRole[selectedRole] -= 4;
                assignedCounts[selectedRole]++;
                Log($"Assigned role {selectedRole} to {offc.Name}");
            }
            return;
        }
        // Walk from station to property
        public static IEnumerator TraverseCurrentToProperty(Property property)
        {
            coros.Add(MelonCoroutines.Start(RaidNotification(property)));

            // Exit Building Set destination
            Log("Traverse Raid Cops");
            float offsetFromCenter = 1.35f;
            int i = 0;

            Transform targetLocation = null;
            // Based on property sometimes it changes where the officers need to go first
            if (property.propertyCode == "manor")
            {
                targetLocation = property.transform.Find("Manor Gate");
            }
            else
            {
                targetLocation = property.NPCSpawnPoint;
            }

            foreach (RaidOfficer offc in currentRaidOfficers)
            {
                Vector3 dir = Vector3.zero;
                switch (i % 4)
                {
                    case 0: dir = Vector3.forward; break;
                    case 1: dir = Vector3.right; break;
                    case 2: dir = Vector3.left; break;
                    case 3: dir = Vector3.back; break;
                }

                offc.officer.Movement.GetClosestReachablePoint(targetLocation.position + dir * offsetFromCenter, out Vector3 groupPosition);
                Log(groupPosition.ToString());

                yield return Wait1;
                if (!registered) yield break;

                offc.officer.Movement.ObstacleAvoidanceEnabled = true;
                offc.officer.Movement.Agent.avoidancePriority = 10;
                offc.officer.Movement.ResumeMovement();
                offc.officer.Movement.SetDestination(pos: groupPosition, maximumDistanceForSuccess: 4f, cacheMaxDistSqr: 5f);
                coros.Add(MelonCoroutines.Start(MonitorTraversal(offc, groupPosition)));
                i++;
            }
            yield return null;
        }

        // Wait for traversal to property to finish
        public static IEnumerator MonitorTraversal(RaidOfficer offc, Vector3 groupPosition)
        {
            NPCMovement movement = offc.officer.Movement;
            if (!distancesToProperty.ContainsKey(offc))
            {
                distancesToProperty.Add(offc, Vector3.Distance(movement.FootPosition, groupPosition));
            }

            float maxTraversalTime = 130f;
            float current = 0f;
            for (; ; )
            {
                yield return Wait05;
                if (!registered) yield break;
                current += 0.5f;

                if (officersArrived.Contains(offc)) break;

                if (movement.CurrentDestination != null && movement.CurrentDestination == Vector3.zero)
                {
                    // just for debug?
                    Log("Raid Cop path is failing to V3.zero");
                }

                if (!CanContinue(offc))
                {
                    Log("Officer cant continue while traversal");
                    if (distancesToProperty.ContainsKey(offc))
                        distancesToProperty.Remove(offc);
                    break;
                }

                if (distancesToProperty.ContainsKey(offc))
                {
                    distancesToProperty[offc] = Vector3.Distance(movement.FootPosition, groupPosition);
                }

                if (Vector3.Distance(movement.FootPosition, groupPosition) < 5f || current >= maxTraversalTime)
                {
                    Log("Officer arrived");
                    movement.EndSetDestination(NPCMovement.WalkResult.Success);
                    officersArrived.Add(offc);
                    OnArrivedAtProperty(offc);
                    if (officersArrived.Count >= raidOfficersAlive)
                    {
                        Log("Last Officer arrived");
                    }

                    break;
                }

                if (!movement.HasDestination && movement.CanMove())
                {
                    Log("Officer does not have destination but can continue, reset");
                    offc.officer.Movement.SetDestination(pos: groupPosition);
                    if (movement.IsPaused)
                        movement.ResumeMovement();
                    continue;
                }
                // anything else to check?
            }
            yield break;
        }

        public static void OnArrivedAtProperty(RaidOfficer offc)
        {
            coros.Add(MelonCoroutines.Start(WaitForAllArrived(offc)));
        }
        // Wait for each member thats alive
        public static IEnumerator WaitForAllArrived(RaidOfficer offc)
        {
            yield return Wait2;
            if (!registered) yield break;
            offc.officer.Movement.PauseMovement();

            Log("Wait for Arrival");
            int maxWait = 30;
            int time = 0;
            while (registered)
            {
                time++;
                if (officersArrived.Count >= raidOfficersAlive || time >= maxWait) break;
                if (!CanContinue(offc))  // auto despawn
                {
                    Log("RaidOfficer died during arrival wait");
                    yield break;
                } 
                yield return Wait2;
            }
            if (!CanContinue(offc)) //autodespawn
            {
                Log("RaidOfficer cant continue after arrival");
                yield break;
            }

            //offc.officer.Behaviour.CombatBehaviour.SetWeapon(offc.officer.GunPrefab != null ? offc.officer.GunPrefab.AssetPath : string.Empty);
            offc.officer.Movement.SpeedController.AddSpeedControl(new NPCSpeedController.SpeedControl("combat", 5, raidConfig.ClearPropertySpeed));

            // Set raise weapon rarely
            if (UnityEngine.Random.Range(0f, 1f) > 0.80f)
            {
#if MONO
                AvatarRangedWeapon wep = offc.officer.Behaviour.CombatBehaviour.currentWeapon as AvatarRangedWeapon;
#else
                AvatarRangedWeapon wep = offc.officer.Behaviour.CombatBehaviour.currentWeapon.TryCast<AvatarRangedWeapon>();
#endif
                if (wep != null)
                {
                    wep.SetIsRaised(true);
                }
            }

            officersReady = true;
            BeginRoleAction(offc);
            coros.Add(MelonCoroutines.Start(LateScareEmployees(offc)));
            yield return null; 
        }

        public static IEnumerator LateScareEmployees(RaidOfficer offc)
        {
            if (employeesScared)
                yield break;
            employeesScared = true;

            yield return Wait5;
            if (!registered || !raidActive) yield break;
#if MONO
            List<Employee> employees = new List<Employee>(offc.targetProperty.Employees);
#else
            Il2CppSystem.Collections.Generic.List<Employee> employees = offc.targetProperty.Employees;
#endif
            if (employees.Count == 0) yield break;
            foreach (Employee employee in employees)
            {
                yield return Wait05;
                if (!registered) yield break;
                employee.Behaviour.FleeBehaviour.SetPointToFlee(employee.AssignedProperty.EmployeeIdlePoints[0].position);
                employee.Behaviour.FleeBehaviour.Activate();
            }
            yield return Wait30;
            if (!registered) yield break;
            foreach (Employee employee in employees)
            {
                yield return Wait05;
                if (!registered) yield break;
                if (employee.Behaviour.activeBehaviour != null && employee.Behaviour.activeBehaviour == employee.Behaviour.FleeBehaviour)
                {
                    employee.Behaviour.FleeBehaviour.Deactivate();
                    employee.Movement.SetDestination(employee.WaitOutside.IdlePoint);
                }
            }

            yield return null;
        }

        // Then here these 4 functions run in a loop to handle the beh
        public static void BeginRoleAction(RaidOfficer offc)
        {
            offc.currentActionIter++;
            if (offc.currentActionIter >= maxActionIters)
            {
                Log("Officer has exhausted action attempts, despawn", offc.Name);
                if (!deadRaidOfficers.Contains(offc))
                    coros.Add(MelonCoroutines.Start(Despawn(offc)));
                return;
            }

            if (offc.role == EOfficerRaidRole.Undecided)
            {
                Log("Officer has no raid role, despawn", offc.Name);
                if (!deadRaidOfficers.Contains(offc))
                    coros.Add(MelonCoroutines.Start(Despawn(offc)));
                return;
            }

            if (offc.destroyedItems >= raidConfig.MaxDestroyIters)
            {
                Log("Officer reached max destroy iters, despawn", offc.Name);
                if (!deadRaidOfficers.Contains(offc))
                    coros.Add(MelonCoroutines.Start(Despawn(offc)));
                return;
            }

            if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
            {
                offc.officer.DialogueHandler.ShowWorldspaceDialogue_5s(raidOfficerLines[UnityEngine.Random.Range(0, raidOfficerLines.Count)]);
                offc.officer.PlayVO(EVOLineType.Command, true);
            }

            coros.Add(MelonCoroutines.Start(DestroyEquipment(offc)));
        }

        public static IEnumerator DestroyEquipment(RaidOfficer offc)
        {
            ERaidDestroyType destroyType = ERaidDestroyType.None;
            BuildableItem destroyTarget = null;
            if (offc.role == EOfficerRaidRole.DestroyGrowEquipment)
            {
#if MONO
                List<Pot> pots = offc.targetProperty.GetBuildablesOfType<Pot>();
                List<MushroomBed> shroomBeds = offc.targetProperty.GetBuildablesOfType<MushroomBed>();
                List<DryingRack> dryingRacks = offc.targetProperty.GetBuildablesOfType<DryingRack>();
#else
                Il2CppSystem.Collections.Generic.List<Pot> pots = offc.targetProperty.GetBuildablesOfType<Pot>();
                Il2CppSystem.Collections.Generic.List<MushroomBed> shroomBeds = offc.targetProperty.GetBuildablesOfType<MushroomBed>();
                Il2CppSystem.Collections.Generic.List<DryingRack> dryingRacks = offc.targetProperty.GetBuildablesOfType<DryingRack>();
#endif

                // Selection logic
                if (pots != null && pots.Count > 0 && pots.Count >= shroomBeds.Count && pots.Count >= dryingRacks.Count)
                {
                    Log("select pots", offc.Name);
                    destroyType = ERaidDestroyType.DestroyPot;
                    destroyTarget = GetValidBuildable<Pot>(pots);
                }
                else if (shroomBeds != null && shroomBeds.Count > 0 && shroomBeds.Count >= pots.Count && shroomBeds.Count >= dryingRacks.Count)
                {
                    Log("select shroombeds", offc.Name);
                    destroyType = ERaidDestroyType.DestroyShroomBed;
                    destroyTarget = GetValidBuildable<MushroomBed>(shroomBeds);
                }
                else if (dryingRacks != null && dryingRacks.Count > 0)
                {
                    Log("select drying racks", offc.Name);
                    destroyType = ERaidDestroyType.DestroyDryingRack;
                    destroyTarget = GetValidBuildable<DryingRack>(dryingRacks);
                }
                else
                {
                    Log("no selectable item types left", offc.Name);
                    offc.currentActionIter = maxActionIters;
                    BeginRoleAction(offc); // back to start and despawn
                    yield break;
                }

            }
            else if (offc.role == EOfficerRaidRole.DestroyLabEquipment)
            {
#if MONO
                List<MixingStationMk2> mixingStations = offc.targetProperty.GetBuildablesOfType<MixingStationMk2>();
                List<LabOven> labOvens = offc.targetProperty.GetBuildablesOfType<LabOven>();
                List<ChemistryStation> chemStations = offc.targetProperty.GetBuildablesOfType<ChemistryStation>();
                List<Cauldron> cauldrons = offc.targetProperty.GetBuildablesOfType<Cauldron>();
#else
                Il2CppSystem.Collections.Generic.List<MixingStationMk2> mixingStations = offc.targetProperty.GetBuildablesOfType<MixingStationMk2>();
                Il2CppSystem.Collections.Generic.List<LabOven> labOvens = offc.targetProperty.GetBuildablesOfType<LabOven>();
                Il2CppSystem.Collections.Generic.List<ChemistryStation> chemStations = offc.targetProperty.GetBuildablesOfType<ChemistryStation>();
                Il2CppSystem.Collections.Generic.List<Cauldron> cauldrons = offc.targetProperty.GetBuildablesOfType<Cauldron>();
#endif

                // Selection logic
                // Prefer destroying mixing stations first (most expensive)
                // Then check based on quantity from lab ovens, chem stations and cauldrons
                if (mixingStations != null && mixingStations.Count > 0)
                {
                    Log("select mixing station mk2", offc.Name);
                    destroyType = ERaidDestroyType.DestroyMixing;
                    destroyTarget = GetValidBuildable<MixingStationMk2>(mixingStations);
                }
                else if (labOvens != null && labOvens.Count > 0 && labOvens.Count >= chemStations.Count && labOvens.Count >= cauldrons.Count)
                {
                    Log("select lab ovens", offc.Name);
                    destroyType = ERaidDestroyType.DestroyLabOven;
                    destroyTarget = GetValidBuildable<LabOven>(labOvens);
                }
                else if (chemStations != null && chemStations.Count > 0 && chemStations.Count >= labOvens.Count && chemStations.Count >= cauldrons.Count)
                {
                    Log("select chem stations", offc.Name);
                    destroyType = ERaidDestroyType.DestroyChemistryStation;
                    destroyTarget = GetValidBuildable<ChemistryStation>(chemStations);
                }
                else if (cauldrons != null && cauldrons.Count > 0)
                {
                    Log("select cauldrons", offc.Name);
                    destroyType = ERaidDestroyType.DestroyCauldron;
                    destroyTarget = GetValidBuildable<Cauldron>(cauldrons);
                }
                else
                {
                    Log("no selectable item types left", offc.Name);
                    offc.currentActionIter = maxActionIters;
                    BeginRoleAction(offc); // back to start and despawn
                    yield break;
                }
            }
            else if (offc.role == EOfficerRaidRole.SearchContainer)
            {
#if MONO
                List<PlaceableStorageEntity> storage = offc.targetProperty.GetBuildablesOfType<PlaceableStorageEntity>();
#else
                Il2CppSystem.Collections.Generic.List<PlaceableStorageEntity> storage = offc.targetProperty.GetBuildablesOfType<PlaceableStorageEntity>();
#endif
                if (storage != null && storage.Count > 0)
                {
                    Log("select storage entities", offc.Name);
                    destroyTarget = GetValidBuildable<PlaceableStorageEntity>(storage, containers: true);
                }
                else
                {
                    Log("no selectable item types left", offc.Name);
                    offc.currentActionIter = maxActionIters;
                    BeginRoleAction(offc); // back to start and despawn
                    yield break;
                }
            }

            if (destroyTarget == null)
            {
                Log("Destroy Target is null after search", offc.Name);
                yield return Wait05;
                if (!registered) yield break;

                BeginRoleAction(offc); // back to start
                yield break;
            }

            ITransitEntity entity = null;
            switch (destroyType)
            {
                case ERaidDestroyType.DestroyPot: 
                    Casted<Pot>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>())); 
                    break;
                case ERaidDestroyType.DestroyShroomBed: 
                    Casted<MushroomBed>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>())); 
                    break;
                case ERaidDestroyType.DestroyDryingRack: 
                    Casted<DryingRack>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>())); 
                    break;
                case ERaidDestroyType.DestroyLabOven: 
                    Casted<LabOven>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>())); 
                    break;
                case ERaidDestroyType.DestroyChemistryStation: 
                    Casted<ChemistryStation>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>())); 
                    break;
                case ERaidDestroyType.DestroyCauldron: 
                    Casted<Cauldron>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>())); 
                    break;
                case ERaidDestroyType.DestroyMixing:
                    Casted<MixingStationMk2>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>()));
                    break;
                case ERaidDestroyType.None:
                    Casted<PlaceableStorageEntity>(destroyTarget, ((t) => entity = t.GetComponent<ITransitEntity>()));
                    break;
            }

            Transform ap = null;
            if (entity != null)
                ap = NavMeshUtility.GetReachableAccessPoint(entity, offc.officer);

            if (ap != null)
                Log("Reachable Access Point found", offc.Name);
            else
            {
                Log($"No reachable access point found for {entity.Name}", offc.Name);
                yield return Wait05;
                if (!registered) yield break;

                BeginRoleAction(offc); // back to start
                yield break;
            }

            offc.currentTargetObj = destroyTarget;
            int targetInstanceID = destroyTarget.GetInstanceID();
            Log($"Set {offc.Name} {offc.role} {destroyTarget.name} ({targetInstanceID})");
            // Traverse
#if MONO
            Action<NPCMovement.WalkResult> walkCallback = null;
#else
            Il2CppSystem.Action<NPCMovement.WalkResult> walkCallback = null;
#endif
            bool callbackConsumed = false;
            void OnTraverseToEntityEnded(NPCMovement.WalkResult result)
            {
                if (walkCallback != null && !callbackConsumed) // incase the callback gets called more than once
                {
                    walkCallback = null;
                    callbackConsumed = true;
                    coros.Add(MelonCoroutines.Start(WaitRunCallback(offc, result, destroyType, destroyTarget, targetInstanceID)));
                }
                else
                {
                    Log("Walk callback was null when traverse to entity ended!", offc.Name);
                }
            }
#if MONO
            walkCallback = (Action<NPCMovement.WalkResult>)OnTraverseToEntityEnded;
#else
            walkCallback = (Il2CppSystem.Action<NPCMovement.WalkResult>)OnTraverseToEntityEnded;
#endif
            
            if (offc.officer.Movement.IsPaused)
                offc.officer.Movement.ResumeMovement();


            if (!offc.officer.Movement.CanGetTo(ap.position))
            {
                yield return Wait05;
                Log("Officer cant pathfind to target position", offc.Name);
                yield return RemoveToBeDestroyed(destroyTarget, destroyType);
                BeginRoleAction(offc); // back to start
                yield break;
            }
            if (!offc.officer.Movement.CanMove())
            {
                yield return Wait05;
                Log("Officer cant move!", offc.Name);
                yield return RemoveToBeDestroyed(destroyTarget, destroyType);
                BeginRoleAction(offc); // back to start
                yield break;
            }

            offc.officer.Movement.SetDestination(
                pos: ap.position,
                interruptExistingCallback: true,
                callback: walkCallback,
                successThreshold: 2f
            );

            // Because occasionally the npc might get stuck standing still and not trigger the callback
            bool IsWalkCallbackConsumed()
            {
                return callbackConsumed;
            }
            coros.Add(MelonCoroutines.Start(WaitEndSetDestination(offc, IsWalkCallbackConsumed)));

            yield break;
        }

#if MONO
        public static BuildableItem GetValidBuildable<T>(List<T> buildables, bool containers = false) where T : BuildableItem
#else
        public static BuildableItem GetValidBuildable<T>(Il2CppSystem.Collections.Generic.List<T> buildables, bool containers = false) where T : BuildableItem
#endif
        {
            if (buildables == null || buildables.Count == 0)
            {
                Log("Buildables list is null or empty");
                return null;
            }

            BuildableItem selected = null;
            int attempts = 0;
            bool isValidItem = false;
            lock (toBeDestroyedLock)
            {
                do
                {
                    attempts++;
                    if (attempts >= maxSearchAttempts)
                    {
                        //Log("Max Buildables Search depth reached, break");
                        break;
                    }
                    selected = buildables[UnityEngine.Random.Range(0, buildables.Count)];
                    if (selected == null)
                    {
                        //Log("Item in buildables list was null, continue next");
                        continue;
                    }
                    if (toBeDestroyed.Contains(selected.GetInstanceID()))
                    {
                        //Log("Item is already marked for destruction, continue next");
                        continue;
                    }

                    if (!containers)
                    {
                        isValidItem = true;
                        break;
                    }

                    else if (containers) 
                    {
                        string id = selected.ItemInstance.ID;
                        if (id.IndexOf("safe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            id.IndexOf("bed", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;

                        if (searchedContainers.Contains(selected.GetInstanceID()))
                            continue;
#if MONO
                        if ((selected as PlaceableStorageEntity).StorageEntity.ItemCount > 0)
                            isValidItem = true;

#else
                        PlaceableStorageEntity temp = selected.TryCast<PlaceableStorageEntity>();
                        if (temp != null && temp.StorageEntity.ItemCount > 0)
                            isValidItem = true;
#endif
                    }
                    if (!isValidItem)
                        selected = null;

                } while (!isValidItem);

                if (selected != null && !containers)
                    toBeDestroyed.Add(selected.GetInstanceID());
                else if (selected != null && containers)
                    searchedContainers.Add(selected.GetInstanceID());
            }
            return selected;
        }

        public static IEnumerator WaitRunCallback(RaidOfficer offc, NPCMovement.WalkResult result, ERaidDestroyType type, BuildableItem destroyTarget, int targetInstanceID)
        {
            yield return Wait1;
            if (!registered) yield break;
            coros.Add(MelonCoroutines.Start(OnWalkToEntity(offc, result, type, destroyTarget, targetInstanceID)));
            yield break;
        }
        public static IEnumerator OnWalkToEntity(RaidOfficer offc, NPCMovement.WalkResult result, ERaidDestroyType type, BuildableItem destroyTarget, int targetInstanceID)
        {
            Log($"Walk to entity Ended: {result}", offc.Name);
            if (result != NPCMovement.WalkResult.Success)
            {
                // Check dead or knocked out despawn??
                if (!CanContinue(offc))
                {
                    Log("Officer interrupted during traverse to entity");
                    RemoveToBeDestroyed(destroyTarget, type);
                    yield break;
                }
                else
                {
                    //?? Not dead or knocked out or in combat
                    Log("Officer failed traverse and can continue", offc.Name);
                    RemoveToBeDestroyed(destroyTarget, type);
                    BeginRoleAction(offc); // back to start or despawn if ended
                }
                yield break;
            }
            yield return Wait2;
            if (!CanContinue(offc))
            {
                RemoveToBeDestroyed(destroyTarget, type);
                Log("Officer cant continue after waiting at entity", offc.Name);
                yield break;
            }

            // if player picks it up its null?
            if (destroyTarget == null || destroyTarget.IsDestroyed)
            {
                Log($"Destroy Target is null: {destroyTarget == null}\nor IsDestroyed: {destroyTarget.IsDestroyed}", offc.Name);
                BeginRoleAction(offc); // back to start or despawn if ended
                yield break;
            }

            string name = destroyTarget.ItemInstance.Name;
            
            switch (type)
            {
                case ERaidDestroyType.None:
                    //ResetEntityConfig((destroyTarget as PlaceableStorageEntity).Configuration);
                    // no need for reset here?
                    break;

                case ERaidDestroyType.DestroyPot:
                    Casted<Pot>(destroyTarget, _ => ResetEntityConfig(_.Configuration));
                    break;

                case ERaidDestroyType.DestroyShroomBed:
                    Casted<MushroomBed>(destroyTarget, _ => ResetEntityConfig(_.Configuration));
                    break;

                case ERaidDestroyType.DestroyDryingRack:
                    Casted<DryingRack>(destroyTarget, _ => ResetEntityConfig(_.Configuration));
                    break;

                case ERaidDestroyType.DestroyLabOven:
                    Casted<LabOven>(destroyTarget, _ => ResetEntityConfig(_.Configuration));
                    break;

                case ERaidDestroyType.DestroyChemistryStation:
                    Casted<ChemistryStation>(destroyTarget, _ => ResetEntityConfig(_.Configuration));
                    break;

                case ERaidDestroyType.DestroyCauldron:
                    Casted<Cauldron>(destroyTarget, _ => ResetEntityConfig(_.Configuration));
                    break;

                case ERaidDestroyType.DestroyMixing:
                    Casted<MixingStationMk2>(destroyTarget, _ => ResetEntityConfig(_.Configuration));
                    break;

                default:
                    break;
            }

            if (offc.role == EOfficerRaidRole.SearchContainer)
            {
#if MONO
                StorageEntity storage = (destroyTarget as PlaceableStorageEntity).StorageEntity;
#else
                PlaceableStorageEntity temp = destroyTarget.TryCast<PlaceableStorageEntity>();
                StorageEntity storage = temp != null ? temp.StorageEntity : null;
#endif
                if (storage == null)
                {
                    Log("Storage is null after arriving!", offc.Name);
                    BeginRoleAction(offc); // back to start
                    yield break;
                }
                StorageEntity.EAccessSettings originalSettings = storage.AccessSettings;
                storage.AccessSettings = StorageEntity.EAccessSettings.Closed;
                for (int i = 0; i < storage.ItemSlots.Count; i++)
                {
                    yield return Wait05;
                    if (!CanContinue(offc))
                    {
                        storage.AccessSettings = originalSettings;
                        Log("Officer interrupted during storage emptying", offc.Name);
                        yield break;
                    }
                    if (storage.ItemSlots[i].IsLocked) continue;
                    if (storage.ItemSlots[i].ItemInstance == null) continue;
                    if (storage.ItemSlots[i].ItemInstance.Definition.legalStatus == ELegalStatus.Legal) continue;
                    yield return Wait05;
                    if (!CanContinue(offc))
                    {
                        storage.AccessSettings = originalSettings;
                        Log("Officer interrupted during storage emptying", offc.Name);
                        yield break;
                    }
                    offc.officer.SetAnimationTrigger("GrabItem");
                    storage.ItemSlots[i].ClearItemInstanceRequested();
                }
                storage.AccessSettings = originalSettings;
                offc.destroyedItems++;
                offc.currentTargetObj = null;
                Log($"Succesfully Emptied Storage: {storage.StorageEntityName} | Total: {offc.destroyedItems}", offc.Name);
                BeginRoleAction(offc); // back to start
                yield break;
            }
            else if (offc.role != EOfficerRaidRole.Undecided)
            {
                offc.officer.SetAnimationTrigger("GrabItem");
                DestroyBuiltItem(destroyTarget, targetInstanceID);
                offc.destroyedItems++;
                offc.currentTargetObj = null;
                Log($"Succesfully Destroyed Target: {name} | Total: {offc.destroyedItems}", offc.Name);
                BeginRoleAction(offc); // back to start
                yield break;
            }
                
            yield return null;
        }
        // -----
#endregion

        #region Helper functions for Raid logic
        
        public static IEnumerator WaitEndSetDestination(RaidOfficer offc, Func<bool> checkConsumed)
        {
            int maxWaitSecs = 15;
            for (int i = 0; i < maxWaitSecs; i += 5)
            {
                yield return Wait5;
                if (checkConsumed()) yield break;
                if (!registered) yield break;
            }
            if (!checkConsumed())
            {
                if (offc.currentTargetObj != null && Vector3.Distance(offc.officer.CenterPoint, offc.currentTargetObj.transform.position) < 2f)
                    offc.officer.Movement.EndSetDestination(NPCMovement.WalkResult.Success);
                else
                    offc.officer.Movement.EndSetDestination(NPCMovement.WalkResult.Interrupted);
            }
        }
        public static void Casted<T>(BuildableItem item, Action<T> callback) where T : BuildableItem
        {
#if MONO
            callback((item as T));
#else
            var casted = item.TryCast<T>();
            if (casted != null)
            {
                callback(casted);
            }
#endif
        }
        public static void ResetEntityConfig(EntityConfiguration conf)
        {
            conf.Reset();
        }
        public static IEnumerator RemoveToBeDestroyed(BuildableItem item, bool containerNotSearched = false)
        {
            lock(toBeDestroyedLock)
            {
                if (item != null)
                {
                    int id = item.GetInstanceID();
                    if (toBeDestroyed.Contains(id))
                        toBeDestroyed.Remove(id);

                    if (containerNotSearched)
                        if (searchedContainers.Contains(id))
                            searchedContainers.Remove(id);
                }
            }
            yield return null;
        }
        public static IEnumerator RemoveToBeDestroyed(BuildableItem item, ERaidDestroyType type = ERaidDestroyType.None)
        {
            if (type != ERaidDestroyType.None)
                yield return RemoveToBeDestroyed(item, containerNotSearched: false);
            else
                yield return RemoveToBeDestroyed(item, containerNotSearched: true);
        }

        public static void DestroyBuiltItem(BuildableItem item, int targetInstanceID)
        {
            item.Destroy_Server();
        }

        public static bool CanContinue(RaidOfficer offc)
        {
            if (!registered) return false;

            if (offc.officer.Health.IsDead || offc.officer.Health.IsKnockedOut)
            {
                Log("Cant continue due to being dead");
                if (!deadRaidOfficers.Contains(offc))
                    coros.Add(MelonCoroutines.Start(Despawn(offc)));
                return false;
            }
            // if active beh is combat
            if (offc.officer.Behaviour.activeBehaviour != null && (offc.officer.Behaviour.activeBehaviour == offc.officer.Behaviour.CombatBehaviour || offc.officer.Behaviour.activeBehaviour == offc.officer.PursuitBehaviour))
            {
                Log("Cant continue due to combat or crime status");
                if (offc.officer.Movement.IsPaused)
                    offc.officer.Movement.ResumeMovement();
                coros.Add(MelonCoroutines.Start(WaitCombatEnd(offc)));
                return false;
            }

            // how to handle combat state it doesnt start despawn?
            return true;
        }
        public static IEnumerator Despawn(RaidOfficer offc)
        {
            if (offc == null || offc.officer == null || offc.officer.gameObject == null || !currentRaidOfficers.Contains(offc))
            {
                Log("Raid Officer already marked for despawn");
                yield break;
            }

            if (raidOfficersAlive > 0)
                raidOfficersAlive--;
            deadRaidOfficers.Add(offc);
            if (offc.officer.Movement.CanMove())
                offc.officer.Movement.SetDestination(PoliceStation.PoliceStations[0].Doors[0].AccessPoint);

            yield return Wait30;
            if (!registered) yield break;
            if (currentSummoned.Contains(offc.officer))
                currentSummoned.Remove(offc.officer);
            Log("Despawning " + offc.Name);
            NPC npc = offc.officer.gameObject.GetComponent<NPC>();
            if (npc != null && NPCManager.NPCRegistry.Contains(npc))
                NPCManager.NPCRegistry.Remove(npc);
            if (npc != null && npc.gameObject != null)
                UnityEngine.Object.Destroy(npc.gameObject);

            if (currentRaidOfficers.Contains(offc))
                currentRaidOfficers.Remove(offc);

            if (currentRaidOfficers.Count == 0)
            {
                Log("Last raid cop despawned");
                // Todo what?
                if (raidActive)
                    ResetRaidEvent();
            }
        }
        public static IEnumerator WaitCombatEnd(RaidOfficer offc)
        {
#if MONO
            yield return new WaitUntil(() =>
                !registered ||
                offc == null ||
                offc.officer == null ||
                offc.officer.Health.IsDead ||
                offc.officer.Health.IsKnockedOut ||
                offc.officer.Behaviour.activeBehaviour == null ||
                (offc.officer.Behaviour.activeBehaviour != offc.officer.Behaviour.CombatBehaviour &&
                 offc.officer.Behaviour.activeBehaviour != offc.officer.PursuitBehaviour)
            );
#else
            yield return new WaitUntil((Il2CppSystem.Func<bool>) (() =>
                !registered ||
                offc == null ||
                offc.officer == null ||
                offc.officer.Health.IsDead ||
                offc.officer.Health.IsKnockedOut ||
                offc.officer.Behaviour.activeBehaviour == null ||
                (offc.officer.Behaviour.activeBehaviour != offc.officer.Behaviour.CombatBehaviour &&
                 offc.officer.Behaviour.activeBehaviour != offc.officer.PursuitBehaviour)
            ));
#endif
            if (!registered) yield break;
            Log("Combat ended or dead, despawn", offc.Name);
            if (offc != null)
            {
                if (!deadRaidOfficers.Contains(offc))
                    coros.Add(MelonCoroutines.Start(Despawn(offc)));
            }
            yield break;
        } 
        public static Dictionary<EOfficerRaidRole, int> GetTotalForRoles(Property property)
        {
#if MONO
            List<Pot> pots = property.GetBuildablesOfType<Pot>();
            List<MushroomBed> shroomBeds = property.GetBuildablesOfType<MushroomBed>();
            List<DryingRack> dryingRacks = property.GetBuildablesOfType<DryingRack>();
            List<LabOven> labOvens = property.GetBuildablesOfType<LabOven>();
            List<ChemistryStation> chemStations = property.GetBuildablesOfType<ChemistryStation>();
            List<Cauldron> cauldrons = property.GetBuildablesOfType<Cauldron>();
            List<MixingStationMk2> mixingStations = property.GetBuildablesOfType<MixingStationMk2>();
            List<PlaceableStorageEntity> storage = property.GetBuildablesOfType<PlaceableStorageEntity>();
#else
            Il2CppSystem.Collections.Generic.List<Pot> pots = property.GetBuildablesOfType<Pot>();
            Il2CppSystem.Collections.Generic.List<MushroomBed> shroomBeds = property.GetBuildablesOfType<MushroomBed>();
            Il2CppSystem.Collections.Generic.List<DryingRack> dryingRacks = property.GetBuildablesOfType<DryingRack>();
            Il2CppSystem.Collections.Generic.List<LabOven> labOvens = property.GetBuildablesOfType<LabOven>();
            Il2CppSystem.Collections.Generic.List<ChemistryStation> chemStations = property.GetBuildablesOfType<ChemistryStation>();
            Il2CppSystem.Collections.Generic.List<Cauldron> cauldrons = property.GetBuildablesOfType<Cauldron>();
            Il2CppSystem.Collections.Generic.List<MixingStationMk2> mixingStations = property.GetBuildablesOfType<MixingStationMk2>();
            Il2CppSystem.Collections.Generic.List<PlaceableStorageEntity> storage = property.GetBuildablesOfType<PlaceableStorageEntity>();
#endif
            int totalGrow = pots.Count + shroomBeds.Count + dryingRacks.Count;
            int totalLab = labOvens.Count + chemStations.Count + cauldrons.Count + mixingStations.Count;
            int totalStorage = storage.Count;
            Log("Total valid raidable in Property: " + (totalGrow + totalLab + totalStorage));
            Log($"GrowTot: {totalGrow} - Pots:{pots.Count}, Shroom:{shroomBeds.Count}, Drying:{dryingRacks.Count}");
            Log($"LabTot: {totalLab} - Ovens:{labOvens.Count}, Chem:{chemStations.Count}, Cauldron:{cauldrons.Count}, Mix: {mixingStations.Count}");
            Log($"Storage: {totalStorage}");

            Dictionary<EOfficerRaidRole, int> totalForRole = new()
            {
                { EOfficerRaidRole.DestroyGrowEquipment, totalGrow },
                { EOfficerRaidRole.DestroyLabEquipment, totalLab },
                { EOfficerRaidRole.SearchContainer, totalStorage }
            };

            return totalForRole;
        }
        public static bool IsPropertyValidForRaid(Property property)
        {
            if (!property.IsOwned)
            {
                Log("Cant raid unowned properties");
                return false;
            }

            if (property.NPCSpawnPoint == null)
            {
                Log("Cant raid property without a spawnpoint");
                return false;
            }
#if MONO
            if (property is Business)
#else
            Business temp = property.TryCast<Business>();
            if (temp != null)
#endif
            {
                Log("Cant start raid on a business");
                return false;
            }

            Dictionary<EOfficerRaidRole, int> totalForRoles = GetTotalForRoles(property);
            int sumTotal = 0;
            int totalValid = 0;

            // Ensure that for any given officer count that can destroy any given item count
            // That there are enough items for all officers to be given a role...
            // By default a fully built sweatshop can barely have a raid with this and then bigger properties have it more easily

            foreach (KeyValuePair<EOfficerRaidRole, int> kvp in totalForRoles)
            {
                sumTotal += kvp.Value;
                if (kvp.Value > (Mathf.Max(2, Mathf.RoundToInt(((float)raidConfig.MaxDestroyIters*0.5f)) * raidConfig.RaidCopsCount)))
                    totalValid++;
            }

            if (totalValid < (2 * Mathf.Max(1, Mathf.RoundToInt((float)raidConfig.RaidCopsCount / 3f))))
            {
                Log("Property does not have enough built items for raid");
                return false;
            }

            return true;
        }
        
        #endregion

        #region UI Alert for notification
        public static IEnumerator RaidNotification(Property property)
        {
            if (raidSlider == null)
            {
                HUD hud = UnityEngine.Object.FindObjectOfType<HUD>();
                GameObject sliderGo = new GameObject("RaidSlider");
                sliderGo.SetActive(false);
                notificationGroup = sliderGo.AddComponent<CanvasGroup>();
                sliderGo.transform.SetParent(hud.canvas.transform, false);
                raidSlider = sliderGo.AddComponent<Slider>();
                raidSlider.maxValue = 1f;
                raidSlider.minValue = 0f;

                RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
                sliderRt.anchoredPosition = new Vector2(0f, 350f);
                sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
                sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
                sliderRt.sizeDelta = new Vector2(600f, 30f);

                // Spawn the sliders objects
#if MONO
                GameObject newFillObj = new GameObject("Fill", typeof(Image));
                GameObject newHandleObj = new GameObject("Handle", typeof(Image));
                GameObject newHomeObj = new GameObject("HomeIcon", typeof(Image));
                GameObject newTextObj = new GameObject("Text", typeof(TextMeshProUGUI));
#else
                GameObject newFillObj = new GameObject("Fill", [Il2CppType.Of<Image>()]);
                GameObject newHandleObj = new GameObject("Handle", [Il2CppType.Of<Image>()]);
                GameObject newHomeObj = new GameObject("HomeIcon", [Il2CppType.Of<Image>()]);
                GameObject newTextObj = new GameObject("Text", [Il2CppType.Of<TextMeshProUGUI>()]);
#endif

                newFillObj.transform.SetParent(raidSlider.transform);
                newHandleObj.transform.SetParent(raidSlider.transform);
                newHomeObj.transform.SetParent(raidSlider.transform);
                newTextObj.transform.SetParent(raidSlider.transform);

                fillRt = newFillObj.GetComponent<RectTransform>();
                raidSlider.fillRect = fillRt;
                sliderFillImage = newFillObj.GetComponent<Image>();
                fillRt.anchoredPosition3D = new Vector3(0f, 0f, 0f);
                fillRt.anchoredPosition = new Vector2(0f, 0f);
                fillRt.offsetMax = new Vector2(0f, -7.5f);
                fillRt.offsetMin = new Vector2(0f, 7.5f);
                fillRt.pivot = new Vector2(0.5f, 0.5f);
                fillRt.sizeDelta = new Vector2(0f, -15f);

                handleRt = newHandleObj.GetComponent<RectTransform>();
                raidSlider.handleRect = handleRt;
                handleGunImage = newHandleObj.GetComponent<Image>();
                handleGunImage.overrideSprite = m1911Sprite;
                handleRt.anchoredPosition3D = new Vector3(25f, 0f, 0f);
                handleRt.anchoredPosition = new Vector2(25f, 0f);
                handleRt.offsetMax = new Vector2(50f, 12.5f);
                handleRt.offsetMin = new Vector2(0f, -12.5f);
                handleRt.sizeDelta = new Vector2(50f, 25f);

                RectTransform homeTr = newHomeObj.GetComponent<RectTransform>();
                homeTr.anchoredPosition = new Vector2(-350f, 0f);
                homeTr.anchorMax = new Vector2(0.5f, 0.5f);
                homeTr.anchorMin = new Vector2(0.5f, 0.5f);
                homeTr.offsetMax = new Vector2(-325f, 25f);
                homeTr.offsetMin = new Vector2(-375f, -25f);
                homeTr.sizeDelta = new Vector2(50f, 50f);
                homeImage = newHomeObj.GetComponent<Image>();
                homeImage.overrideSprite = homeSprite;

                RectTransform textTr = newTextObj.GetComponent<RectTransform>();
                textTr.anchoredPosition = new Vector2(0f, 30f);
                textTr.anchorMax = new Vector2(0.5f, 0.5f);
                textTr.anchorMin = new Vector2(0.5f, 0.5f);
                textTr.offsetMax = new Vector2(300f, 55f);
                textTr.offsetMin = new Vector2(-300f, 5f);
                textTr.sizeDelta = new Vector2(600f, 50f);
                raidText = newTextObj.GetComponent<TextMeshProUGUI>();

                raidText.fontSize = 20f;
                raidText.alignment = TextAlignmentOptions.Top;
                raidText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            } // Reusable while in scene

            raidSlider.value = 1f;
            sliderFillImage.color = Color.white;
            raidText.text = $"The police are raiding {property.PropertyName}!";

            coros.Add(MelonCoroutines.Start(FadeUI(4f, 2.5f, true)));

            float originalDistance = Vector3.Distance(currentRaidOfficers[0].officer.CenterPoint, currentRaidOfficers[0].targetProperty.NPCSpawnPoint.position);

            for (; ; )
            {
                yield return Wait05;
                if (!registered) yield break;
                if (officersReady || !raidActive)
                    break;

                // Because these get set to NaN,0 force the update
                fillRt.anchoredPosition = new Vector2(0f, 0f);
                handleRt.anchoredPosition = new Vector2(25f, 0f);

                // Update 
                float sumOfDistRemaining = 0f;
                float totalMaximumDistances = 0f;
                if (distancesToProperty.Count > 0)
                {
                    totalMaximumDistances = originalDistance * distancesToProperty.Count;

                    foreach (float value in distancesToProperty.Values)
                        sumOfDistRemaining += value;
                }

                // This value decreases towards 0.0 based on distance to property
                float travelRemainingNorm = Mathf.Clamp01(sumOfDistRemaining / totalMaximumDistances);

                if (travelRemainingNorm < 0.05f)
                {
                    raidSlider.value = 0f;
                    sliderFillImage.color = Color.red;
                    Log("Detected all arrived");
                    coros.Add(MelonCoroutines.Start(HighlightHomeIcon(homeImage)));
                    coros.Add(MelonCoroutines.Start(FadeScaleIcon(handleGunImage)));
                    break;
                }
                else
                {
                    sliderFillImage.color = new Color(r: 1f, g: travelRemainingNorm, b: travelRemainingNorm);
                    raidSlider.value = travelRemainingNorm;
                }
            }
            coros.Add(MelonCoroutines.Start(FadeUI(2f, 2.5f, false)));
            distancesToProperty.Clear();
            yield break;
        }

        public static IEnumerator HighlightHomeIcon(Image image)
        {
            // Fade to Red, alpha0 simultaneously while scaling up
            float dur = 2f;
            float current = 0f;
            float xOrig = image.rectTransform.sizeDelta.x;
            float yOrig = image.rectTransform.sizeDelta.y;
            Color origColor = image.color;
            while (current < dur)
            {
                current += Time.deltaTime;
                float t = current / dur;
                image.color = new Color(
                    r: Mathf.SmoothStep(origColor.r, 1f, t),
                    g: Mathf.SmoothStep(origColor.g, 0f, t),
                    b: Mathf.SmoothStep(origColor.b, 0f, t),
                    a: Mathf.SmoothStep(origColor.a, 0f, t)
                );
                image.rectTransform.sizeDelta = new Vector2(
                    x: Mathf.SmoothStep(xOrig, xOrig*1.5f, t), 
                    y: Mathf.SmoothStep(yOrig, yOrig*1.5f, t)
                );
                yield return null;
            }
            yield return Wait5;
            if (!registered) yield break;

            image.rectTransform.sizeDelta = new Vector2(xOrig, yOrig);
            image.color = origColor;
            yield break;
        }

        public static IEnumerator FadeScaleIcon(Image image)
        {
            float dur = 1f;
            float current = 0f;
            float xOrig = image.rectTransform.sizeDelta.x;
            float yOrig = image.rectTransform.sizeDelta.y;
            Color origColor = image.color;
            while (current < dur)
            {
                current += Time.deltaTime;
                float t = current / dur;
                image.color = SetAlpha(image.color, Mathf.SmoothStep(1f, 0f, t));
                image.rectTransform.sizeDelta = new Vector2(x:Mathf.SmoothStep(xOrig, 0f, t), y:Mathf.SmoothStep(yOrig, 0f, t));
                yield return null;
            }
            yield return Wait5;
            if (!registered) yield break;

            image.rectTransform.sizeDelta = new Vector2(xOrig, yOrig);
            image.color = origColor;
            yield break;
        }

        public static Color SetAlpha(Color c, float a) => new Color(c.r,c.g,c.b, a);

        public static IEnumerator FadeUI(float delayUntilStart, float durInOut, bool fadeIn)
        {
            yield return new WaitForSeconds(delayUntilStart);
            float current = 0f;
            if (fadeIn)
            {
                notificationGroup.alpha = 0f;
                raidSlider.gameObject.SetActive(true);
            }
            while (current < durInOut)
            {
                current += Time.deltaTime;
                float t = current / durInOut;
                if (fadeIn)
                    notificationGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                else
                    notificationGroup.alpha = Mathf.SmoothStep(1f, 0f, t);
                yield return null;
            }
            if (fadeIn)
                notificationGroup.alpha = 1f;
            else
                notificationGroup.alpha = 0f;

            yield return Wait01;
            if (!fadeIn)
            {
                raidSlider.gameObject.SetActive(false);
                notificationGroup.alpha = 1f;
            }
            yield break;
        }

        public static void SetRaidSprite()
        {
            Func<string, ItemDefinition> GetItem;
#if MONO
            GetItem = ScheduleOne.Registry.GetItem;
#else
            GetItem = Il2CppScheduleOne.Registry.GetItem;
#endif
            ItemDefinition defWeapon = GetItem("m1911");
            ItemInstance inst = defWeapon.GetDefaultInstance();

#if MONO
            if (inst is IntegerItemInstance intInstance)
                m1911Sprite = intInstance.Icon;
#else
            IntegerItemInstance temp = inst.TryCast<IntegerItemInstance>();
            if (temp != null)
                m1911Sprite = temp.Icon;
#endif

            Property example = Property.Properties[0]; // Take the RV
            RectTransform rt = example.PoI.IconContainer;
            Transform imageTr = null;
            for (int i = 0; i < rt.childCount; i++)
            {
                Transform tr = rt.GetChild(i);
                if (tr.name == "Owned")
                {
                    if (tr.childCount > 0)
                        imageTr = tr.GetChild(0);
                    if (imageTr != null)
                        homeSprite = imageTr.GetComponent<Image>().sprite;
                }
            }
        }
#endregion

    }
}
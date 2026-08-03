using MelonLoader;
using System.Collections;
using UnityEngine;

using static NACops.DebugModule;
using static NACops.FootPatrolGenerator;
using static NACops.NACops;
using static NACops.RaidPropertyEvent;
using static NACops.SentryGenerator;
using static NACops.VehiclePatrolGenerator;
using static NACops.PrivateInvestigator;
using static NACops.MassSurveillance;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Law;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.Property;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Map;
using ScheduleOne.Police;
using ScheduleOne.UI;
using TMPro;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.UI;
using Il2CppTMPro;
#endif


namespace NACops
{
    public static class ConsoleModule
    {
        public static bool isBuilding = false;
        public static bool isLoggingEnabled = false;
        public static readonly HashSet<string> ConsoleMethodNames = new() // Method names which cant be hidden in logs by default
        {
            "Help", "List", "Spawn", "Visualize", "BuildStart", "BuildEnd", "RunCommand"
        };

        [Flags]
        public enum CommandSupport
        {
            None = 0,
            List = 1 << 0,
            Spawn = 1 << 1,
            SpawnNoIndex = 1 << 2,
            Visualize = 1 << 3,
            Build = 1 << 4
        }

        public abstract class ConsoleCommandBase
        {
            public virtual string Name { get; }
            public virtual CommandSupport SupportedMethods { get; }
            public virtual void List() => Log("Not implemented");
            public virtual void Spawn(int index) => Log("Not implemented");
            public virtual void Visualize(int index) => Log("Not implemented");
            public virtual void Build(string arg) => Log($"Build Argument: {arg} Not implemented");
            protected static void CleanVisual()
            {
                if (pathVisualizer != null && pathVisualizer.Count > 0)
                    foreach (GameObject go in pathVisualizer)
                        GameObject.Destroy(go);
                pathVisualizer.Clear();

                if (lineRenderMat == null)
                    lineRenderMat = new Material(Shader.Find("Sprites/Default"));

                if (cameraBeamMat == null)
                    cameraBeamMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
            protected static void DrawPath(string name, Vector3[] points)
            {
                GameObject newGo = new GameObject($"Path_{name}");
                pathVisualizer.Add(newGo);
                LineRenderer lineRenderer = newGo.AddComponent<LineRenderer>();
                lineRenderer.material = lineRenderMat;
                lineRenderer.widthMultiplier = 0.5f;
                lineRenderer.startColor = Color.blue;
                lineRenderer.endColor = Color.red;
                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions(points);
            }
        }

        public class FootPatrolTarget : ConsoleCommandBase
        {
            public override string Name => "footpatrol";
            public override CommandSupport SupportedMethods => CommandSupport.List | CommandSupport.Spawn | CommandSupport.Visualize | CommandSupport.Build;

            public static List<Vector3> recordedPathNodes = new();
            public static string currentPathName;
            public override void List()
            {
                string listmessage = "";
                int i = 0;
                listmessage += "\nIndex: Name";
                foreach (PatrolInstance inst in generatedPatrolInstances.Keys)
                {
                    listmessage += $"\n{i}: {inst.Route.name}";
                    i++;
                }
                listmessage += "\n-------";
                Log(listmessage);
                return;
            }
            public override void Spawn(int index)
            {
                List<PatrolInstance> patrols = generatedPatrolInstances.Keys.ToList();
                if (index >= patrols.Count) return;
                PatrolInstance instance = patrols[index];
                if (instance.ActiveGroup != null)
                {
                    Log("Foot patrol group is already active");
                    return;
                }
                int originalStart = instance.StartTime;
                int originalEnd = instance.EndTime;
                instance.StartTime = NetworkSingleton<TimeManager>.Instance.CurrentTime;
                instance.EndTime = TimeManager.AddMinutesTo24HourTime(originalStart, 240);
                instance.StartPatrol();
                Log($"Patrol {instance.Route.name} Spawned");
                IEnumerator EndSoon()
                {
                    yield return new WaitForSeconds(240f);
                    instance.EndPatrol();
                    instance.StartTime = originalStart;
                    instance.EndTime = originalEnd;
                }

                coros.Add(MelonCoroutines.Start(EndSoon()));
                return;
            }
            public override void Visualize(int index)
            {
                CleanVisual();
                List<PatrolInstance> patrols = generatedPatrolInstances.Keys.ToList();
                if (index < 0 || index >= patrols.Count) return;
                FootPatrolRoute route = patrols[index].Route;
                Vector3[] waypoints = route.Waypoints.Select(waypoint => waypoint.position + Vector3.up * 8f).ToArray();
                DrawPath(route.name, waypoints);
                Log($"Patrol {route.name} Visualized");
                return;
            }


            public override void Build(string arg)
            {
                if (arg.ToLower() == "start")
                    BuildStart();
                else if (isBuilding)
                    BuildEnd();
            }
            public void BuildStart()
            {
                if (isBuilding)
                {
                    Log($"Already building a path or a sentry!\n    Use: nacops build {Name} stop\n    to stop building");
                    return;
                }
                isBuilding = true;
                currentPathName = $"{Name}_{Guid.NewGuid()}";
                Log($"Started building path with name {currentPathName}\nWalk around to create new path nodes!");
                coros.Add(MelonCoroutines.Start(FollowPlayer()));
            }
            public IEnumerator FollowPlayer()
            {
                Transform tr = Player.Local.CenterPointTransform;
                GameObject newGo = new GameObject($"Path");
                pathVisualizer.Add(newGo);
                recordedPathNodes.Add(Player.Local.CenterPointTransform.position);

                LineRenderer lineRenderer = newGo.AddComponent<LineRenderer>();
                lineRenderer.material = lineRenderMat;
                lineRenderer.widthMultiplier = 0.5f;
                lineRenderer.startColor = Color.blue;
                lineRenderer.endColor = Color.red;
                lineRenderer.positionCount = recordedPathNodes.Count;
                lineRenderer.SetPositions(recordedPathNodes.ToArray());

                while (registered && isBuilding)
                {
                    // if distance from last node is larger than 6 units
                    if (Vector3.Distance(tr.position, recordedPathNodes[recordedPathNodes.Count - 1]) > 6f)
                    {
                        BuildNode(lineRenderer);
                    }
                }

                yield return null;
            }
            public void BuildNode(LineRenderer lineRenderer)
            {
                recordedPathNodes.Add(Player.Local.CenterPointTransform.position);
                lineRenderer.positionCount = recordedPathNodes.Count;
                lineRenderer.SetPositions(recordedPathNodes.ToArray());
            }
            public void BuildEnd()
            {
                isBuilding = false;
                CleanVisual();
                if (recordedPathNodes.Count == 0)
                {
                    Log("No recorded nodes found.");
                    return;
                }
                if (recordedPathNodes.Count < 4)
                {
                    Log("Build more path nodes to save.");
                    recordedPathNodes.Clear();
                    return;
                }
                SerializedFootPatrol patrol = new();

                patrol.startTime = 1900;
                patrol.endTime = 500;
                patrol.members = 2;
                patrol.intensityRequirement = 1;
                patrol.onlyIfCurfew = false;
                patrol.name = currentPathName;
                patrol.days = new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }; // default all days

                List<Vector3> filteredNodes = new();
                filteredNodes.Add(recordedPathNodes[0]);
                foreach (Vector3 pos in recordedPathNodes)
                {
                    if (Vector3.Distance(pos, filteredNodes[filteredNodes.Count - 1]) > 24f)
                        filteredNodes.Add(pos);
                }
                patrol.waypoints = new(filteredNodes);

                serPatrols.loadedPatrols.Add(patrol);
                Log($"Finished building: {currentPathName}");
                Log($"Recorded path nodes: {recordedPathNodes.Count}\n    Reload the game to apply changes.");
                ConfigLoader.Save(serPatrols);
                recordedPathNodes.Clear();
            }
        }
        public class VehiclePatrolTarget : ConsoleCommandBase
        {
            public override string Name => "vehiclepatrol";
            public override CommandSupport SupportedMethods => CommandSupport.List | CommandSupport.Spawn | CommandSupport.Visualize | CommandSupport.Build;

            public static List<Vector3> recordedPathNodes = new();
            public static string currentPathName;

            public override void List()
            {
                string listmessage = "";
                int i = 0;
                listmessage += "\nIndex: Name";

                foreach (VehiclePatrolInstance inst in generatedVehiclePatrolInstances.Keys)
                {
                    listmessage += $"\n{i}: {inst.Route.name}";
                    i++;
                }
                listmessage += "\n-------";
                Log(listmessage);
                return;
            }
            public override void Spawn(int index)
            {
                List<VehiclePatrolInstance> patrols = generatedVehiclePatrolInstances.Keys.ToList();
                if (index >= patrols.Count) return;
                VehiclePatrolInstance instance = patrols[index];
                if (instance.activeOfficer != null)
                {
                    Log("Vehicle patrol is already active");
                    return;
                }
                int originalStart = instance.StartTime;
                instance.StartTime = NetworkSingleton<TimeManager>.Instance.CurrentTime;
                instance.StartPatrol();
                Log($"Vehicle Patrol {instance.Route.name} Spawned");
                IEnumerator EndSoon()
                {
                    yield return new WaitForSeconds(240f);
                    instance.StartTime = originalStart;
                }

                coros.Add(MelonCoroutines.Start(EndSoon()));
                return;
            }
            public override void Visualize(int index)
            {
                CleanVisual();
                List<VehiclePatrolInstance> patrols = generatedVehiclePatrolInstances.Keys.ToList();
                if (index < 0 || index >= patrols.Count) return;
                VehiclePatrolRoute route = patrols[index].Route;
                Vector3[] waypoints = route.Waypoints.Select(waypoint => waypoint.position + Vector3.up * 8f).ToArray();
                DrawPath(route.name, waypoints);
                Log($"Veicle Patrol {route.name} Visualized");
                return;
            }

            public override void Build(string arg)
            {
                if (arg.ToLower() == "start")
                    BuildStart();
                else if (isBuilding)
                    BuildEnd();
            }
            public void BuildStart()
            {
                if (isBuilding)
                {
                    Log($"Already building a path or a sentry!\n    Use: nacops build {Name} stop\n    to stop building");
                    return;
                }
                isBuilding = true;
                currentPathName = $"{Name}_{Guid.NewGuid()}";
                Log($"Started building path with name {currentPathName}\nWalk on the road to create new path nodes!");
                coros.Add(MelonCoroutines.Start(FollowPlayer()));
            }
            public IEnumerator FollowPlayer()
            {
                Transform tr = Player.Local.CenterPointTransform;
                GameObject newGo = new GameObject($"Path");
                pathVisualizer.Add(newGo);
                recordedPathNodes.Add(Player.Local.CenterPointTransform.position);

                LineRenderer lineRenderer = newGo.AddComponent<LineRenderer>();
                lineRenderer.material = lineRenderMat;
                lineRenderer.widthMultiplier = 0.5f;
                lineRenderer.startColor = Color.blue;
                lineRenderer.endColor = Color.red;
                lineRenderer.positionCount = recordedPathNodes.Count;
                lineRenderer.SetPositions(recordedPathNodes.ToArray());

                while (registered && isBuilding)
                {
                    yield return Wait1;
                    // if distance from last node is larger than 24 units
                    if (Vector3.Distance(tr.position, recordedPathNodes[recordedPathNodes.Count - 1]) > 6f)
                    {
                        BuildNode(lineRenderer);
                    }
                }

                yield return null;
            }
            public void BuildNode(LineRenderer lineRenderer)
            {
                recordedPathNodes.Add(Player.Local.CenterPointTransform.position);
                lineRenderer.positionCount = recordedPathNodes.Count;
                lineRenderer.SetPositions(recordedPathNodes.ToArray());
            }
            public void BuildEnd()
            {
                isBuilding = false;
                CleanVisual();
                if (recordedPathNodes.Count == 0)
                {
                    Log("No recorded nodes found.");
                    return;
                }
                if (recordedPathNodes.Count < 4)
                {
                    Log("Build more path nodes to save.");
                    recordedPathNodes.Clear();
                    return;
                }
                SerializedVehiclePatrol patrol = new();

                patrol.startTime = 1900;
                patrol.intensityRequirement = 1;
                patrol.onlyIfCurfew = false;
                patrol.name = currentPathName;
                patrol.days = new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }; // default all days

                List<Vector3> filteredNodes = new();
                filteredNodes.Add(recordedPathNodes[0]);
                foreach (Vector3 pos in recordedPathNodes)
                {
                    if (Vector3.Distance(pos, filteredNodes[filteredNodes.Count - 1]) > 24f)
                        filteredNodes.Add(pos);
                }
                patrol.waypoints = new(filteredNodes);

                serVehiclePatrols.loadedVehiclePatrols.Add(patrol);
                Log($"Finished building: {currentPathName}");
                Log($"Recorded path nodes: {recordedPathNodes.Count}\n    Reload the game to apply changes.");
                ConfigLoader.Save(serVehiclePatrols);
                recordedPathNodes.Clear();
            }
        }
        public class SentryTarget : ConsoleCommandBase
        {
            public override string Name => "sentry";
            public override CommandSupport SupportedMethods => CommandSupport.List | CommandSupport.Spawn | CommandSupport.Visualize | CommandSupport.Build;
            public static List<Vector3> recordedPathNodes = new();
            public static string currentPathName;

            public override void List()
            {
                string listmessage = "";
                int i = 0;
                listmessage += "\nIndex: Name";
                foreach (SentryInstance inst in generatedSentryInstances.Keys)
                {
                    listmessage += $"\n{i}: {inst._potentialLocations[0].gameObject.name}";
                    i++;
                }
                listmessage += "\n-------";
                Log(listmessage);
            }
            public override void Spawn(int index)
            {
                List<SentryInstance> sentrys = generatedSentryInstances.Keys.ToList();
                if (index >= sentrys.Count) return;
                SentryInstance instance = sentrys[index];
                if (instance._potentialLocations[0].AssignedOfficers.Count > 0)
                {
                    Log("Sentry is already active");
                    return;
                }
                int originalStart = instance.StartTime;
                int originalEnd = instance.EndTime;
                instance.StartTime = NetworkSingleton<TimeManager>.Instance.CurrentTime;
                instance.EndTime = TimeManager.AddMinutesTo24HourTime(originalStart, 240);
                instance.StartEntry();
                Log($"Sentry {instance._potentialLocations[0].gameObject.name} Spawned");
                IEnumerator EndSoon()
                {
                    yield return new WaitForSeconds(240f);
                    instance.EndSentry();
                    instance.StartTime = originalStart;
                    instance.EndTime = originalEnd;
                }

                coros.Add(MelonCoroutines.Start(EndSoon()));
                return;
            }
            public override void Visualize(int index)
            {
                CleanVisual();
                List<SentryInstance> sentrys = generatedSentryInstances.Keys.ToList();
                if (index < 0 || index >= sentrys.Count) return;
                SentryInstance instance = sentrys[index];
                for (int i = 0; i < instance._potentialLocations[0].Routes.Count; i++)
                {
                    Vector3 pos = instance._potentialLocations[0].Routes[0].RoutePoints[i].position;
                    Vector3[] standPoints = new Vector3[] { pos, pos + Vector3.up * 8f };
                    DrawPath($"{instance._potentialLocations[0].gameObject.name}_{i}", standPoints);
                }
                Log($"Sentry {instance._potentialLocations[0].gameObject.name} Visualized");
                return;
            }

            public override void Build(string arg)
            {
                if (arg.ToLower() == "start")
                    BuildStart();
                else if (isBuilding)
                    coros.Add(MelonCoroutines.Start(BuildEnd()));
            }
            public void BuildStart()
            {
                if (isBuilding)
                {
                    Log($"Already building a path or sentry!\n    Use: nacops build {Name} stop\n    to stop building");
                    return;
                }
                isBuilding = true;
                currentPathName = $"{Name}_{Guid.NewGuid()}";
                Log($"{currentPathName}: Set 1st Sentry Point\n    Walk to 2nd sentry point and type:\nnacops build {Name} stop");
                MakeVertBeam();
            }
            public void MakeVertBeam()
            {
                Transform tr = Player.Local.CenterPointTransform;
                GameObject newGo = new GameObject($"Path");
                pathVisualizer.Add(newGo);
                recordedPathNodes.Add(Player.Local.CenterPointTransform.position);

                LineRenderer lineRenderer = newGo.AddComponent<LineRenderer>();
                lineRenderer.material = lineRenderMat;
                lineRenderer.widthMultiplier = 0.5f;
                lineRenderer.startColor = Color.blue;
                lineRenderer.endColor = Color.red;
                lineRenderer.positionCount = 2;
                Vector3[] beam = { tr.position, tr.position + Vector3.up * 5f };
                lineRenderer.SetPositions(beam);
                return;
            }
            public IEnumerator BuildEnd()
            {
                recordedPathNodes.Add(Player.Local.CenterPointTransform.position);
                isBuilding = false;

                if (recordedPathNodes.Count == 0)
                {
                    Log("No recorded nodes found.");
                    yield break;
                }
                if (recordedPathNodes.Count != 2)
                {
                    Log("Build more sentry nodes to save.");
                    recordedPathNodes.Clear();
                    yield break;
                }
                SerializedSentry sentry = new();

                sentry.startTime = 1900;
                sentry.endTime = 500;
                sentry.members = 1;
                sentry.intensityRequirement = 1;
                sentry.onlyIfCurfew = false;
                sentry.name = currentPathName;
                sentry.days = new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }; // default all days
                sentry.standPosition1 = recordedPathNodes[0];
                sentry.standPosition2 = recordedPathNodes[1];
                serSentries.loadedSentrys.Add(sentry);
                Log($"Finished building: {currentPathName}");
                Log($"Recorded path nodes: {recordedPathNodes.Count}\n    Reload the game to apply changes.");
                ConfigLoader.Save(serSentries);
                recordedPathNodes.Clear();

                yield return Wait5;
                CleanVisual();
            }
        }
        public class RaidTarget : ConsoleCommandBase
        {
            public override string Name => "raid";
            public override CommandSupport SupportedMethods => CommandSupport.List | CommandSupport.Spawn;

            public override void List()
            {
                lock (heatConfigLock)
                {
                    List<PropertyHeat> currentHeats = new(heatConfig);
                    string listmessage = "";
                    int i = 0;
                    listmessage += "\nIndex: Name";
                    foreach (PropertyHeat heat in currentHeats)
                    {
                        listmessage += $"\n{i}: {heat.propertyCode}\n    DaysSinceRaid: {heat.daysSinceLastRaid}\n    Heat: {heat.propertyHeat}";
                        i++;
                    }
                    listmessage += "\n-------";
                    Log(listmessage);
                }

                return;
            }
            public override void Spawn(int index)
            {
                if (index < 0 || index >= heatConfig.Count) return;

                Property selected = null;
                foreach (Property prop in Property.Properties)
                {
                    if (prop.PropertyCode == heatConfig[index].propertyCode)
                        selected = prop;
                }
                if (!selected)
                    return;

                if (selected.NPCSpawnPoint == null)
                {
                    Log($"No valid destination for property: {selected.propertyName}");
                    return;
                }

#if MONO
                if (selected is Business)
#else
                Business temp = selected.TryCast<Business>();
                if (temp != null)
#endif
                {
                    Log("Cant start raid on a business");
                    return;
                }



                coros.Add(MelonCoroutines.Start(BeginRaidEvent(selected)));
                return;
            }
            public override void Visualize(int index)
            {
                Log("Not supported");
                return;
            }
        }
        public class InvestigatorTarget : ConsoleCommandBase
        {
            public override string Name => "investigator";
            public override CommandSupport SupportedMethods => CommandSupport.SpawnNoIndex;

            public override void List()
            {
                Log("Not supported");
                return;
            }
            public override void Spawn(int index)
            {
                if (investigatorActive)
                {
                    Log("Investigator is already active!");
                }
                else
                {
                    Log("Spawning Private Investigator");
                    coros.Add(MelonCoroutines.Start(HandlePIMonitor()));
                }
                return;
            }
            public override void Visualize(int index)
            {
                Log("Not supported");
                return;
            }
        }
        public class CopAnalyticsTarget : ConsoleCommandBase
        {
            public override string Name => "analytics";
            public static TextMeshProUGUI AnalyticsTextPanel;

#if DEBUG
            public override CommandSupport SupportedMethods => CommandSupport.List | CommandSupport.Build | CommandSupport.Visualize;
#else
            public override CommandSupport SupportedMethods => CommandSupport.Visualize;
#endif
            // Debug builds only can output the csv files or record the runtime
            // and output csv list analytics & build analytics start/stop
            // Visualize shows counts on screen
#if DEBUG
            public override void List()
            {
                Log("Creating static weekly officer requirements...");
                PreEvaluateWeeklyRequirements();
                return;
            }

            public override void Build(string arg)
            {
                if (arg.ToLower() == "start")
                    BuildStart();
                else if (isBuilding)
                    BuildEnd();
            }
            public void BuildStart()
            {
                if (isBuilding)
                {
                    Log($"Already recording runtime analytics!\n    Use: nacops build {Name} stop\n    to stop recording");
                    return;
                }
                Log($"Starting runtime analytics recording with automatic time pass.\n    Use: nacops build {Name} stop\n    to stop recording and output the file");
                isBuilding = true;
                TimeManager instance = NetworkSingleton<TimeManager>.Instance;
#if MONO
                instance.onHourPass = (Action)Delegate.Combine(instance.onHourPass, new Action(EvaluateLawSettingsState));
#else
                instance.onHourPass += (Il2CppSystem.Action)EvaluateLawSettingsState;
#endif

            }
            public void BuildEnd()
            {
                if (!isBuilding)
                {
                    Log($"Analytics runtime recording is not active!\n    Use: nacops build {Name} start\n    to start recording");
                    return;
                }
                isBuilding = false;

                TimeManager instance = NetworkSingleton<TimeManager>.Instance;
#if MONO
                instance.onHourPass = (Action)Delegate.Remove(instance.onHourPass, new Action(EvaluateLawSettingsState));
#else
                instance.onHourPass -= (Il2CppSystem.Action)EvaluateLawSettingsState;
#endif
                OnRuntimeAnalyticsBuildEnd();
            }

#endif
            public override void Visualize(int index)
            {
                if (AnalyticsTextPanel == null)
                {
                    MelonCoroutines.Start(MakeUI());
                    return;
                }

                if (AnalyticsTextPanel != null && AnalyticsTextPanel.enabled)
                {
                    Log("Disabling Analytics text");
                    AnalyticsTextPanel.enabled = false;
                }
                else if (AnalyticsTextPanel != null)
                {
                    Log("Enabling Analytics text");
                    AnalyticsTextPanel.enabled = true;
                }

                return;
            }
            public IEnumerator MakeUI()
            {
                AnalyticsTextPanel = new GameObject("CurrentLawIntensity").AddComponent<TextMeshProUGUI>();
                SetupAnalyticsUI(AnalyticsTextPanel);
                Log("Finished instantiating UI");
                coros.Add(MelonCoroutines.Start(UpdateUI()));
                yield break;
            }
            public IEnumerator UpdateUI()
            {
                SetAnalyticsString();
                for (; ; )
                {
                    yield return Wait30;
                    if (!registered) yield break;
                    if (!AnalyticsTextPanel.enabled) continue;
                    SetAnalyticsString();
                }
            }
            public void SetAnalyticsString()
            {
                string current = "";
                current += $"LAW INTENSITY: {Singleton<LawController>.Instance.internalLawIntensity}\n";

                current += $"IN POOL: {PoliceStation.PoliceStations[0].OfficerPool.Count}\n";

                int notInBuilding = 0;

                // precalculate how many are actually attending with behaviour active
                int behActiveCheckpoint = 0;
                int behActiveFootPatrol = 0;
                int behActiveSentry = 0;
                int behActiveVehiclePatrol = 0;


                foreach (PoliceOfficer offc in PoliceOfficer.Officers)
                {
                    if (!offc.isInBuilding) notInBuilding++;
                    if (offc.Behaviour.activeBehaviour != null)
                    {
                        if (offc.Behaviour.activeBehaviour == offc.CheckpointBehaviour)
                            behActiveCheckpoint++;

                        if (offc.Behaviour.activeBehaviour == offc.FootPatrolBehaviour)
                            behActiveFootPatrol++;

                        if (offc.Behaviour.activeBehaviour == offc.SentryBehaviour)
                            behActiveSentry++;

                        if (offc.Behaviour.activeBehaviour == offc.VehiclePatrolBehaviour)
                            behActiveVehiclePatrol++;
                    }

                }
                current += $"ACTIVE: {notInBuilding}/{PoliceOfficer.Officers.Count}\n";

                // calculate current law activity required
                int checkpointsInSettings = 0;
                int checkpointsOfficersReqMin = 0;
                int checkpointsOfficersReqMax = 0;

                int patrolsInSettings = 0;
                int patrolsOfficersReqMin = 0;
                int patrolsOfficersReqMax = 0;

                int sentriesInSettings = 0;
                int sentriesOfficersReqMin = 0;
                int sentriesOfficersReqMax = 0;

                int vehiclepatrolsInSettings = 0;
                int vehiclepatrolsOfficersReqMin = 0;

                LawActivitySettings settings = Singleton<LawController>.Instance.GetSettings();
                int currentTime = TimeManager.Instance.CurrentTime;
                List<string> formatted = new();
                foreach (var instance in settings.Checkpoints)
                {
                    if (TimeManager.IsGivenTimeWithinRange(currentTime, instance.StartTime, instance.EndTime))
                    {
                        checkpointsOfficersReqMin += instance.MinMembers;
                        checkpointsOfficersReqMax += instance.MaxMembers;
                        checkpointsInSettings++;
                    }
                }
                formatted.Add($"Checkpoints: {checkpointsInSettings} | static members: {checkpointsOfficersReqMin}-{checkpointsOfficersReqMax} | actual performing: {behActiveCheckpoint}\n");

                foreach (var instance in settings.Patrols)
                {
                    if (TimeManager.IsGivenTimeWithinRange(currentTime, instance.StartTime, instance.EndTime))
                    {
                        patrolsOfficersReqMin += instance.MinMembers;
                        patrolsOfficersReqMax += instance.MaxMembers;
                        patrolsInSettings++;
                    }
                }
                formatted.Add($"FootPatrols: {patrolsInSettings} | static members: {patrolsOfficersReqMin}-{patrolsOfficersReqMax} | actual performing: {behActiveFootPatrol}\n");

                foreach (var instance in settings.Sentries)
                {
                    if (TimeManager.IsGivenTimeWithinRange(currentTime, instance.StartTime, instance.EndTime))
                    {
                        sentriesOfficersReqMin += instance.MinMembers;
                        sentriesOfficersReqMax += instance.MaxMembers;
                        sentriesInSettings++;
                    }
                }
                formatted.Add($"Sentries: {sentriesInSettings} | static members: {sentriesOfficersReqMin}-{sentriesOfficersReqMax} | actual performing: {behActiveSentry}\n");

                foreach (var instance in settings.VehiclePatrols)
                {
                    if (TimeManager.IsGivenTimeWithinRange(currentTime, instance.StartTime, TimeManager.AddMinutesTo24HourTime(instance.latestStartTime, 60)))
                    {
                        vehiclepatrolsOfficersReqMin++;
                        vehiclepatrolsInSettings++;
                    }
                }
                formatted.Add($"VehiclePatrols: {vehiclepatrolsInSettings} | static members: {vehiclepatrolsOfficersReqMin} | actual performing: {behActiveVehiclePatrol}\n");

                int sumMin = checkpointsOfficersReqMin + patrolsOfficersReqMin + sentriesOfficersReqMin + vehiclepatrolsOfficersReqMin;
                int sumMax = checkpointsOfficersReqMax + patrolsOfficersReqMax + sentriesOfficersReqMax + vehiclepatrolsOfficersReqMin;
                int absDiff = Mathf.Abs(PoliceOfficer.Officers.Count - sumMin);
                string missingOrSurplus = PoliceOfficer.Officers.Count > sumMin ? $"Surplus {absDiff}" : $"Missing {absDiff}";
                current += $"OFFICERS REQUIRED NOW: {sumMin}-{sumMax} | {missingOrSurplus}\n";

                int sumBehActiveTotal = behActiveCheckpoint + behActiveFootPatrol + behActiveSentry + behActiveVehiclePatrol;
                int staticMedianBehsActive = Mathf.RoundToInt((float)(sumMin + sumMax) / 2f);
                int totalActivitiesCount = checkpointsInSettings + patrolsInSettings + sentriesInSettings + vehiclepatrolsInSettings;
                current += $"ACTIVITIES TOTAL: {totalActivitiesCount} | STATIC MEDIAN: {staticMedianBehsActive} | BEHACTIVE: {sumBehActiveTotal}\n";

                foreach (string formattedActivity in formatted)
                {
                    current += formattedActivity;
                }

                AnalyticsTextPanel.text = current;
                return;
            }

            public void SetupAnalyticsUI(TextMeshProUGUI comp)
            {
                comp.transform.SetParent(Singleton<HUD>.Instance.canvas.transform, false);
                comp.alignment = TextAlignmentOptions.TopLeft;
                comp.fontSize = 16;
                comp.color = Color.red;
                comp.rectTransform.anchorMin = new Vector2(0, 1);
                comp.rectTransform.anchorMax = new Vector2(0, 1);
                comp.rectTransform.pivot = new Vector2(0, 1);
                comp.rectTransform.anchoredPosition = new Vector2(40, -40);
                comp.rectTransform.sizeDelta = new Vector2(600f, 500f);
            }
        }
        public class SurveillanceTarget : ConsoleCommandBase
        {
            public override string Name => "surveillance";
            public override CommandSupport SupportedMethods => CommandSupport.SpawnNoIndex | CommandSupport.Visualize;
            public static bool hasDrawnVisuals = false;

            public override void Spawn(int index)
            {
                Log("Enabling nearest Flock instance");
                Vector3 pos = Player.Local.CenterPointTransform.position;
                HylandFlockInstance nearest = null;
                float nearestDist = 100f;

                foreach (HylandFlockInstance inst in allCameras)
                {
                    if (inst.activeToday) continue;
                    float instDist = Vector3.Distance(pos, inst.transform.position);
                    if (instDist < nearestDist)
                    {
                        nearestDist = instDist;
                        nearest = inst;
                    }
                }
                nearest.ActivateInstance();
                activeCameras.Add(nearest);
                Log("Enabled");
                return;
            }
            public override void Visualize(int index)
            {
                CleanVisual();

                if (hasDrawnVisuals)
                {
                    hasDrawnVisuals = false;
                    return;
                }

                for (int i = 0; i < activeCameras.Count; i++)
                {
                    Vector3 pos = activeCameras[i].transform.position;
                    Vector3[] beamPos = new Vector3[] { pos, pos + Vector3.up * 40f };
                    DrawPath($"ActiveFlock_{i}", beamPos);
                }
                hasDrawnVisuals = true;
                Log("Active cameras visualized");
                return;
            }
        }

    }
}
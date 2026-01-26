using MelonLoader;
using System.Collections;
using UnityEngine;

using static NACopsV1.DebugModule;
using static NACopsV1.FootPatrolGenerator;
using static NACopsV1.NACops;
using static NACopsV1.RaidPropertyEvent;
using static NACopsV1.SentryGenerator;
using static NACopsV1.VehiclePatrolGenerator;
using static NACopsV1.PrivateInvestigator;

#if MONO
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Law;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.Property;
using ScheduleOne.PlayerScripts;
#else
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.PlayerScripts;
#endif


namespace NACopsV1
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

            public List<Vector3> recordedPathNodes = new();
            public string currentPathName;
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
                    if (Vector3.Distance(tr.position, recordedPathNodes[recordedPathNodes.Count-1]) > 6f)
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
                patrol.members = 1;
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

            public List<Vector3> recordedPathNodes = new();
            public string currentPathName;
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

            public override void List()
            {
                string listmessage = "";
                int i = 0;
                listmessage += "\nIndex: Name";
                foreach (SentryInstance inst in generatedSentryInstances.Keys)
                {
                    listmessage += $"\n{i}: {inst.Location.gameObject.name}";
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
                if (instance.Location.AssignedOfficers.Count > 0)
                {
                    Log("Sentry is already active");
                    return;
                }
                int originalStart = instance.StartTime;
                int originalEnd = instance.EndTime;
                instance.StartTime = NetworkSingleton<TimeManager>.Instance.CurrentTime;
                instance.EndTime = TimeManager.AddMinutesTo24HourTime(originalStart, 240);
                instance.StartEntry();
                Log($"Sentry {instance.Location.gameObject.name} Spawned");
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
                for (int i = 0; i < instance.Location.StandPoints.Count; i++)
                {
                    Vector3 pos = instance.Location.StandPoints[i].position;
                    Vector3[] standPoints = new Vector3[] { pos, pos + Vector3.up * 8f };
                    DrawPath($"{instance.Location.gameObject.name}_{i}", standPoints);
                }
                Log($"Sentry {instance.Location.gameObject.name} Visualized");
                return;
            }

            public List<Vector3> recordedPathNodes = new();
            public string currentPathName;
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
                Log("Spawning Private Investigator");
                coros.Add(MelonCoroutines.Start(HandlePIMonitor()));
                return;
            }
            public override void Visualize(int index)
            {
                Log("Not supported");
                return;
            }
        }

    }
}
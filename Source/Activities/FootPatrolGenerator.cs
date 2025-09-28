


using UnityEngine;

using static NACopsV1.ConfigLoader;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.Law;
using ScheduleOne.NPCs.Behaviour;
#else
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.NPCs.Behaviour;
#endif

namespace NACopsV1
{
    public class FootPatrolGenerator
    {
        // as instance, usable weekdays
        public static Dictionary<PatrolInstance, List<string>> generatedPatrolInstances = new();
        public static FootPatrolsSerialized serPatrols;
        public static PatrolInstance[] GeneratePatrol(LawActivitySettings template, string day = "")
        {

            if (generatedPatrolInstances.Count == 0)
            {
                Log("Generating new patrol routes");
                Transform patrolsTr = LawController.Instance.transform.Find("PatrolRoutes");
                // basically foreach this but needs to be loaded from config file
                if (serPatrols == null)
                    serPatrols = LoadPatrolsConfig();
                foreach (SerializedFootPatrol ser in serPatrols.loadedPatrols)
                {
                    GameObject newPatrolObject = new(ser.name);
                    Log("Generate object for patrol: " + ser.name);
                    Log($"- Days: {string.Join(" ", ser.days)}");
                    FootPatrolRoute route = newPatrolObject.AddComponent<FootPatrolRoute>();
                    route.name = ser.name;
                    route.StartWaypointIndex = 0;

                    Transform[] generatedWaypoints = new Transform[ser.waypoints.Count];
                    for (int i = 0; i < ser.waypoints.Count; i++)
                    {
                        GameObject newWaypoint = new(name: i == 0 ? "Waypoint" : $"Waypoint ({i})");
                        newWaypoint.transform.position = ser.waypoints[i];
                        newWaypoint.transform.parent = newPatrolObject.transform;
                        generatedWaypoints[i] = newWaypoint.transform;
                    }

                    route.Waypoints = generatedWaypoints;

                    newPatrolObject.transform.parent = patrolsTr;

                    PatrolInstance inst = new();
                    inst.StartTime = ser.startTime;
                    inst.EndTime = ser.endTime;
                    inst.Members = ser.members;
                    inst.Route = route;
                    inst.OnlyIfCurfewEnabled = ser.onlyIfCurfew;

                    newPatrolObject.transform.parent = patrolsTr;
                    newPatrolObject.SetActive(true);

                    generatedPatrolInstances.Add(inst, ser.days);
                }
            }

            if (day == "")
            {
                int prevLen = template.Patrols.Length;
                int addedAmount = generatedPatrolInstances.Count;
                int newLen = prevLen + addedAmount;
                PatrolInstance[] newPatrolInstances = new PatrolInstance[newLen];
                System.Array.Copy(template.Patrols, newPatrolInstances, prevLen);

                int i = prevLen;
                foreach (KeyValuePair<PatrolInstance, List<string>> kvp in generatedPatrolInstances)
                {
                    if (!(i < newLen)) break;
                    newPatrolInstances[i] = kvp.Key;
                    i++;
                }
                return newPatrolInstances;
            }
            else
            {
                int prevLen = template.Patrols.Length;

                // Decide the added amount
                int addedAmount = 0;
                foreach (KeyValuePair<PatrolInstance, List<string>> kvp in generatedPatrolInstances)
                {
                    if (kvp.Value.Contains(day))
                    {
                        addedAmount++;
                    }
                }

                if (addedAmount == 0) return template.Patrols; // No change

                int newLen = prevLen + addedAmount;
                PatrolInstance[] newPatrolInstances = new PatrolInstance[newLen];
                System.Array.Copy(template.Patrols, newPatrolInstances, prevLen);

                int i = prevLen;
                foreach (KeyValuePair<PatrolInstance, List<string>> kvp in generatedPatrolInstances)
                {
                    if (!(i < newLen)) break;
                    if (kvp.Value.Contains(day))
                    {
                        newPatrolInstances[i] = kvp.Key;
                        i++;
                    }
                }
                Log($"    {day}: Added {addedAmount} patrols ({prevLen} -> {newLen})");
                return newPatrolInstances;
            }
        }
    }

    [Serializable]
    public class SerializedFootPatrol
    {
        public int startTime = 1900;
        public int endTime = 500;
        public int members = 1;
        public int intensityRequirement = 1;
        public bool onlyIfCurfew = false;

        public string name = "NACops Extra Loop";
        public List<string> days; //= new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }; // default all days

        public List<Vector3> waypoints = new();
    }
}
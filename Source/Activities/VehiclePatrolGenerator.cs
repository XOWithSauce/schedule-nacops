


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
    public class VehiclePatrolGenerator
    {
        // as instance, usable weekdays
        public static Dictionary<VehiclePatrolInstance, List<string>> generatedVehiclePatrolInstances = new();
        public static VehiclePatrolsSerialized serVehiclePatrols;
        public static VehiclePatrolInstance[] GenerateVehiclePatrol(LawActivitySettings template, string day = "")
        {

            if (generatedVehiclePatrolInstances.Count == 0)
            {
                Log("Generating new vehicle patrol routes");
                Transform patrolsTr = LawController.Instance.transform.Find("VehiclePatrolRoutes");
                // basically foreach this but needs to be loaded from config file
                if (serVehiclePatrols == null)
                    serVehiclePatrols = LoadVehiclePatrolsConfig();
                foreach (SerializedVehiclePatrol ser in serVehiclePatrols.loadedVehiclePatrols)
                {
                    GameObject newPatrolObject = new(ser.name);
                    Log("Generate object for patrol: " + ser.name);
                    Log($"- Days: {string.Join(" ", ser.days)}");
                    VehiclePatrolRoute route = newPatrolObject.AddComponent<VehiclePatrolRoute>();
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

                    VehiclePatrolInstance inst = new();
                    inst.StartTime = ser.startTime;
                    inst.Route = route;
                    inst.IntensityRequirement = ser.intensityRequirement;
                    inst.OnlyIfCurfewEnabled = ser.onlyIfCurfew;

                    newPatrolObject.transform.parent = patrolsTr;
                    newPatrolObject.SetActive(true);

                    generatedVehiclePatrolInstances.Add(inst, ser.days);
                }
            }

            if (day == "")
            {
                int prevLen = template.VehiclePatrols.Length;
                int addedAmount = generatedVehiclePatrolInstances.Count;
                int newLen = prevLen + addedAmount;
                VehiclePatrolInstance[] newPatrolInstances = new VehiclePatrolInstance[newLen];
                System.Array.Copy(template.VehiclePatrols, newPatrolInstances, prevLen);

                int i = prevLen;
                foreach (KeyValuePair<VehiclePatrolInstance, List<string>> kvp in generatedVehiclePatrolInstances)
                {
                    if (!(i < newLen)) break;
                    newPatrolInstances[i] = kvp.Key;
                    i++;
                }
                return newPatrolInstances;
            }
            else
            {
                int prevLen = template.VehiclePatrols.Length;

                // Decide the added amount
                int addedAmount = 0;
                foreach (KeyValuePair<VehiclePatrolInstance, List<string>> kvp in generatedVehiclePatrolInstances)
                {
                    if (kvp.Value.Contains(day))
                    {
                        addedAmount++;
                    }
                }

                if (addedAmount == 0) return template.VehiclePatrols; // No change

                int newLen = prevLen + addedAmount;
                VehiclePatrolInstance[] newPatrolInstances = new VehiclePatrolInstance[newLen];
                System.Array.Copy(template.VehiclePatrols, newPatrolInstances, prevLen);

                int i = prevLen;
                foreach (KeyValuePair<VehiclePatrolInstance, List<string>> kvp in generatedVehiclePatrolInstances)
                {
                    if (!(i < newLen)) break;
                    if (kvp.Value.Contains(day))
                    {
                        newPatrolInstances[i] = kvp.Key;
                        i++;
                    }
                }
                Log($"    {day}: Added {addedAmount} vehicle patrols ({prevLen} -> {newLen})");
                return newPatrolInstances;
            }
        }
    }

    [Serializable]
    public class SerializedVehiclePatrol
    {
        public int startTime = 2300;
        public int intensityRequirement = 1;
        public bool onlyIfCurfew = false;

        public string name = "NACops Vehicle Extra Loop";
        public List<string> days; //= new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }; // default all days 

        public List<Vector3> waypoints = new(); 
    }
}


using UnityEngine;

using static NACopsV1.ConfigLoader;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.Law;
#else
using Il2CppScheduleOne.Law;
#endif

namespace NACopsV1
{
    public class SentryGenerator
    {
        public static Dictionary<SentryInstance, List<string>> generatedSentryInstances = new();
        public static SentrysSerialized serSentries;
        public static SentryInstance[] GenerateSentry(LawActivitySettings template, string day = "")
        {
            if (generatedSentryInstances.Count == 0)
            {
                Log("Generating new sentry spots");
                Transform sentriesTr = LawController.Instance.transform.Find("Sentry Locations");
                if (sentriesTr == null)
                {
                    Log("    Sentry Locations transform is null");
                }

                Log("    Load Sentry Config");
                // basically foreach this but needs to be loaded from config file
                if (serSentries == null)
                    serSentries = LoadSentryConfig();
                Log("    Loaded Patrols config count: " + serSentries.loadedSentrys.Count);

                foreach (SerializedSentry ser in serSentries.loadedSentrys)
                {
                    GameObject newSentryObject = new(ser.name);
                    Log("Generate object for patrol: " + ser.name);
                    Log($"- Days: {string.Join(" ", ser.days)}");

                    SentryLocation loc = newSentryObject.AddComponent<SentryLocation>();
                    loc.StandPoints = new();
                    GameObject standPos1 = new("Stand point");
                    standPos1.transform.parent = newSentryObject.transform;
                    standPos1.transform.SetPositionAndRotation(ser.standPosition1, Quaternion.Euler(ser.pos1Rotation));
                    loc.StandPoints.Add(standPos1.transform);

                    GameObject standPos2 = new("Stand point (1)");
                    standPos2.transform.parent = newSentryObject.transform;
                    standPos2.transform.SetPositionAndRotation(ser.standPosition2, Quaternion.Euler(ser.pos2Rotation));
                    loc.StandPoints.Add(standPos2.transform);

                    loc.gameObject.SetActive(true);

                    SentryInstance inst = new();
                    inst.StartTime = ser.startTime;
                    inst.EndTime = ser.endTime;
                    inst.Members = ser.members;
                    inst.Location = loc;
                    inst.OnlyIfCurfewEnabled = ser.onlyIfCurfew;

                    newSentryObject.transform.parent = sentriesTr;
                    newSentryObject.SetActive(true);

                    generatedSentryInstances.Add(inst, ser.days);
                }
            }
            
            if (day == "")
            {
                int prevLen = template.Sentries.Length;
                int addedAmount = generatedSentryInstances.Count;
                int newLen = prevLen + addedAmount;

                SentryInstance[] newSentryInstances = new SentryInstance[newLen];
                System.Array.Copy(template.Sentries, newSentryInstances, prevLen);
                int i = prevLen;
                foreach (KeyValuePair<SentryInstance, List<string>> kvp in generatedSentryInstances)
                {
                    if (!(i < newLen)) break;
                    newSentryInstances[i] = kvp.Key;
                    i++;
                }
                return newSentryInstances;
            }
            else
            {
                int prevLen = template.Sentries.Length;
                // Decide the added amount
                int addedAmount = 0;
                foreach (KeyValuePair<SentryInstance, List<string>> kvp in generatedSentryInstances)
                {
                    if (kvp.Value.Contains(day))
                    {
                        addedAmount++;
                    }
                }

                if (addedAmount == 0) return template.Sentries; // No change

                int newLen = prevLen + addedAmount;
                SentryInstance[] newSentryInstances = new SentryInstance[newLen];
                System.Array.Copy(template.Sentries, newSentryInstances, prevLen);

                int i = prevLen;
                foreach (KeyValuePair<SentryInstance, List<string>> kvp in generatedSentryInstances)
                {
                    if (!(i < newLen)) break;
                    if (kvp.Value.Contains(day))
                    {
                        newSentryInstances[i] = kvp.Key;
                        i++;
                    }
                }
                Log($"    {day}: Added {addedAmount} sentries ({prevLen} -> {newLen})");
                return newSentryInstances;
            }
            
        }
    }

    [Serializable]
    public class SerializedSentry
    {
        public int startTime = 1900;
        public int endTime = 500;
        public int members = 1;
        public int intensityRequirement = 1;
        public bool onlyIfCurfew = false;

        public string name;

        public List<string> days; //= new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" }; // default all days

        public Vector3 standPosition1;
        public Vector3 pos1Rotation;

        public Vector3 standPosition2;
        public Vector3 pos2Rotation;
    }
}
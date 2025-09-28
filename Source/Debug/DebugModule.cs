
using System.Diagnostics;
using MelonLoader;
using UnityEngine;
using MelonLoader.Utils;
using System.Text;

using static NACopsV1.NACops;

#if MONO
using ScheduleOne.Map;
using ScheduleOne.Police;
using ScheduleOne.DevUtilities;
using ScheduleOne.Law;
using ScheduleOne.GameTime;
#else
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.GameTime;
#endif


namespace NACopsV1
{
    public static class DebugModule
    {
        public static int origCount = 0;
#if DEBUG
#if MONO
        public static Dictionary<HourlyRequirements, string> hrReqList = new(); // the hourpass appends here can be csv outputted
        public static bool exportingAnalytics = false;
#endif
#endif

        [Conditional("DEBUG")]
        public static void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        #region Debug Evaluate officer usage versus available and csv exports
        [Conditional("DEBUG")]
        public static void LogStateHourly()
        {
#if DEBUG
#if MONO
            // mono only debug

            bool instant = false;

            // To keep track of how officers inside station change
            // Gives a rough idea of how many officers is needed at any given time to meet the
            // added sentrys + routes requirement for officers, nearing 0 would be insufficient
            origCount = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>().Length;

            if (instant)
            {
                PreEvaluateWeeklyRequirements();
            } else
            {
                NetworkSingleton<TimeManager>.Instance.onHourPass += EvaluateLawSettingsState;
            }
#endif
#endif
            return;
        }

        [Conditional("DEBUG")]
        public static void OnInput()
        {
#if DEBUG
#if MONO
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.CapsLock))
                {
                    if (hrReqList.Count == 0 || exportingAnalytics) return;
                    exportingAnalytics = true;
                    var csvContent = new StringBuilder();

                    csvContent.AppendLine("Day,Time,RequiredOfficers,ActiveActivities,StationOccupants,FootPatrols,Sentries,VehiclePatrols,Checkpoints");

                    foreach (KeyValuePair<HourlyRequirements, string> kvp in hrReqList)
                    {
                        string dayName = kvp.Value;

                        csvContent.AppendLine(
                            $"{dayName}," +
                            $"{kvp.Key.Time:D4}," +
                            $"{kvp.Key.RequiredOfficers}," +
                            $"{kvp.Key.ActiveActivities}," +
                            $"{kvp.Key.StationOccupants}," +
                            $"{kvp.Key.FootPatrols}," +
                            $"{kvp.Key.Sentries}," +
                            $"{kvp.Key.VehiclePatrols}" +
                            $"{kvp.Key.Checkpoints},"

                        );
                    }

                    try
                    {
                        string modDirectory = Path.Combine(MelonEnvironment.ModsDirectory, "NACops");

                        if (!Directory.Exists(modDirectory))
                        {
                            Directory.CreateDirectory(modDirectory);
                        }

                        string filePath = Path.Combine(modDirectory, "WeeklyOfficerRequirementsRuntime.csv");

                        File.WriteAllText(filePath, csvContent.ToString());

                        Log($"Saved officer weekly requirements to: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning(ex);
                    }

                    hrReqList.Clear();
                    exportingAnalytics = false;
                }
            }
#endif
#endif
        }

#if DEBUG
#if MONO
        // the hourpass
        public static void EvaluateLawSettingsState()
        {

            if (exportingAnalytics) return;
            int time = NetworkSingleton<TimeManager>.Instance.CurrentTime;

            Log($"Day {NetworkSingleton<TimeManager>.Instance.CurrentDay} - Time {time}");

            if (!TimeManager.IsValid24HourTime(time.ToString())) return;
            // works well if used only for override settings because needs to wait 24 min irl basically for complete logs
            // kinda busted tho because its dynamic how the officers operate and it can change from session to session

            // for the entire week this should be just pre-evaluated and not have to wait like 2 hrs irl
            // maybe bind it to hourpass method so its not just waiting can speed up timescale?

            // the states should instead in this function be calculated by parsing the officers and checking which
            // ones are getting evaluated from the behaviour active.

            // estimate officer needs for the state of now
            LawActivitySettings currentSettings = Singleton<LawController>.Instance.CurrentSettings;
            if (currentSettings != null)
            {
                int checkpoints = 0;
                int checkpointsInstances = 0;
                int patrols = 0;
                int patrolsInstances = 0;
                int sentrys = 0;
                int sentrysInstances = 0;
                int inVehicle = 0;
                int vehicleInstances = 0;

                int uniqueActive = 0;

                foreach (CheckpointInstance checkpointInstance in currentSettings.Checkpoints)
                {
                    if (TimeManager.IsGivenTimeWithinRange(time, checkpointInstance.StartTime, checkpointInstance.EndTime))
                    {
                        //Log($"    Checkpoint  +{checkpointInstance.Members}");
                        checkpoints += checkpointInstance.Members;
                        uniqueActive++;
                        checkpointsInstances++;
                    }
                }
                foreach (PatrolInstance patrolInstance in currentSettings.Patrols)
                {
                    if (TimeManager.IsGivenTimeWithinRange(time, patrolInstance.StartTime, patrolInstance.EndTime))
                    {
                        //Log($"    Patrol  +{patrolInstance.Members}");
                        patrols += patrolInstance.Members;
                        uniqueActive++;
                        patrolsInstances++;
                    }
                }
                foreach (SentryInstance sentryInstance in currentSettings.Sentries)
                {
                    if (TimeManager.IsGivenTimeWithinRange(time, sentryInstance.StartTime, sentryInstance.EndTime))
                    {
                        //Log($"    Sentry  +{sentryInstance.Members}");
                        sentrys += sentryInstance.Members;
                        uniqueActive++;
                        sentrysInstances++;
                    }
                }
                foreach (VehiclePatrolInstance vehiclePatrolInstance in currentSettings.VehiclePatrols)
                {
                    // this one is a bit weird because the instance doesnt have end time so the assumption is that the route takes max 60mins 
                    if (TimeManager.IsGivenTimeWithinRange(time, vehiclePatrolInstance.StartTime, TimeManager.AddMinutesTo24HourTime(vehiclePatrolInstance.StartTime, 60)))
                    {
                        //Log($"    Vehicle  +1");
                        inVehicle += 1;
                        uniqueActive++;
                        vehicleInstances++;
                    }
                }
                int tot = checkpoints + patrols + sentrys + inVehicle;
                int missing = tot - origCount;
                string isMissing = missing < 0 ? "Available" : "Missing";
                missing = missing < 0 ? -missing : missing;
                Log($"Total Officers Required: {tot}\n    Unique Activities: {uniqueActive} \n    {isMissing} {missing} Officers");
                Log($"Police Station: \n    - Occupants: {PoliceStation.PoliceStations.FirstOrDefault().OccupantCount} / {origCount}");

                HourlyRequirements currHr = new();
                currHr.Time = time;
                currHr.RequiredOfficers = tot;
                currHr.ActiveActivities = uniqueActive;
                currHr.StationOccupants = PoliceStation.PoliceStations.FirstOrDefault().OccupantCount;
                currHr.Sentries = sentrysInstances;
                currHr.FootPatrols = patrolsInstances;
                currHr.Checkpoints = checkpointsInstances;
                currHr.VehiclePatrols = vehicleInstances;

                string day = NetworkSingleton<TimeManager>.Instance.CurrentDay.ToString();
                hrReqList.Add(currHr, day);

                if (time == 400)
                {
                    Log("Auto skip to next day");
                    NetworkSingleton<TimeManager>.Instance.SetTime(659);
                }
            }
            return;
        }


        // calculate from the base values, not as accurate as the dynamic logging of hourpass, but gives a rough idea of max requirements + allows instantly to generate statistics for mod with any sentry+patrol config
        public class HourlyRequirements
        {
            public int Time { get; set; }
            public int RequiredOfficers { get; set; }
            public int ActiveActivities { get; set; }
            public int StationOccupants { get; set; }
            public int FootPatrols { get; set; }
            public int Sentries { get; set; }
            public int VehiclePatrols { get; set; }
            public int Checkpoints { get; set; }

        }
        public static void PreEvaluateWeeklyRequirements()
        {

            Dictionary<string, LawActivitySettings> daySettings = new Dictionary<string, LawActivitySettings>
            {
                { "Monday", Singleton<LawController>.Instance.MondaySettings },
                { "Tuesday", Singleton<LawController>.Instance.TuesdaySettings },
                { "Wednesday", Singleton<LawController>.Instance.WednesdaySettings },
                { "Thursday", Singleton<LawController>.Instance.ThursdaySettings },
                { "Friday", Singleton<LawController>.Instance.FridaySettings },
                { "Saturday", Singleton<LawController>.Instance.SaturdaySettings },
                { "Sunday", Singleton<LawController>.Instance.SundaySettings }
            };

            var csvContent = new StringBuilder();

            csvContent.AppendLine("Day,Time,RequiredOfficers,ActiveActivities,StationOccupants,FootPatrols,Sentries,VehiclePatrols,Checkpoints");

            foreach (KeyValuePair<string, LawActivitySettings> kvp in daySettings)
            {
                string dayName = kvp.Key;
                LawActivitySettings settings = kvp.Value;

                Log($"Pre-calculating requirements for: {dayName}");

                int hrTime = 0;
                for (int hour = 0; hour < 24; hour++)
                {
                    if (TimeManager.IsValid24HourTime(hrTime.ToString()))
                    {
                        HourlyRequirements results = CalculateHourlyRequirements(settings, hrTime);

                        csvContent.AppendLine(
                            $"{dayName}," +
                            $"{results.Time:D4}," +
                            $"{results.RequiredOfficers}," +
                            $"{results.ActiveActivities}," +
                            $"{results.StationOccupants}," +
                            $"{results.FootPatrols}," +
                            $"{results.Sentries}," +
                            $"{results.VehiclePatrols}" +
                            $"{results.Checkpoints},"

                        );
                    }
                    else
                    {
                        Log("Not Valid time skipping precalculation");
                    }
                    hrTime = TimeManager.AddMinutesTo24HourTime(hrTime, 60);
                }
            }

            try
            {
                string modDirectory = Path.Combine(MelonEnvironment.ModsDirectory, "NACops");

                if (!Directory.Exists(modDirectory))
                {
                    Directory.CreateDirectory(modDirectory);
                }

                string filePath = Path.Combine(modDirectory, "WeeklyOfficerRequirements.csv");

                File.WriteAllText(filePath, csvContent.ToString());

                Log($"Saved officer weekly requirements to: {filePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(ex);
            }
            return;
        }
        private static HourlyRequirements CalculateHourlyRequirements(LawActivitySettings settings, int time)
        {

            int checkpoints = 0;
            int checkpointInstances = 0;
            int patrols = 0;
            int patrolsInstances = 0;
            int sentrys = 0;
            int sentrysInstances = 0;
            int inVehicle = 0;
            int vehiclesInstances = 0;
            int uniqueActive = 0;

            // Checkpoints
            foreach (var instance in settings.Checkpoints)
            {
                if (TimeManager.IsGivenTimeWithinRange(time, instance.StartTime, instance.EndTime))
                {
                    checkpoints += instance.Members;
                    uniqueActive++;
                    checkpointInstances++;
                }
            }
            // Foot Patrols
            foreach (var instance in settings.Patrols)
            {
                if (TimeManager.IsGivenTimeWithinRange(time, instance.StartTime, instance.EndTime))
                {
                    patrols += instance.Members;
                    uniqueActive++;
                    patrolsInstances++;
                }
            }
            // Sentries
            foreach (var instance in settings.Sentries)
            {
                if (TimeManager.IsGivenTimeWithinRange(time, instance.StartTime, instance.EndTime))
                {
                    sentrys += instance.Members;
                    uniqueActive++;
                    sentrysInstances++;
                }
            }
            // Vehicle Patrols
            foreach (var instance in settings.VehiclePatrols)
            {
                if (TimeManager.IsGivenTimeWithinRange(time, instance.StartTime, TimeManager.AddMinutesTo24HourTime(instance.StartTime, 60)))
                {
                    inVehicle += 1;
                    uniqueActive++;
                    vehiclesInstances++;
                }
            }

            return new HourlyRequirements
            {
                Time = time,
                RequiredOfficers = checkpoints + patrols + sentrys + inVehicle,
                ActiveActivities = uniqueActive,
                StationOccupants = 0,
                Checkpoints = checkpointInstances,
                FootPatrols = patrolsInstances,
                Sentries = sentrysInstances,
                VehiclePatrols = vehiclesInstances
            };

        }
#endif
#endif
        #endregion

    }
}
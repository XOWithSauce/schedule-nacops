using HarmonyLib;
using MelonLoader;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

using static NACops.ConsoleModule;
using static NACops.DebugModule;
using static NACops.ModDataPaths;

#if MONO
using ConsoleType = ScheduleOne.Console;
using ScheduleOne.DevUtilities;
using ScheduleOne.GameTime;
using ScheduleOne.Law;
using ScheduleOne.Map;
#else
using ConsoleType = Il2CppScheduleOne.Console;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Map;
#endif

namespace NACops
{
    public static class DebugModule
    {
        public static Material lineRenderMat;
        public static List<GameObject> pathVisualizer = new();
        public static Material cameraBeamMat;
#if DEBUG
        public static int origCount = 0;
        public static Dictionary<HourlyRequirements, string> hrReqList = new(); // the hourpass appends here can be csv outputted
        public static bool exportingAnalytics = false;
#endif


        public static void Log(string msg, [CallerMemberName] string memberName = "")
        {
#if DEBUG
            // Debug builds log everything
            MelonLogger.Msg($"[{memberName}] {msg}");
#else
            // Player has to manually enable full logging otherwise its just console feedback
            if (isLoggingEnabled || ConsoleMethodNames.Contains(memberName))
                MelonLogger.Msg($"[{memberName}] {msg}");
#endif
        }

        #region Debug controls for console

        public static Dictionary<string, ConsoleCommandBase> consoleTargets = new()
        {
            { "footpatrol", new FootPatrolTarget() },
            { "vehiclepatrol", new VehiclePatrolTarget() },
            { "sentry", new SentryTarget() },
            { "raid", new RaidTarget() },
            { "investigator", new InvestigatorTarget() },
            { "surveillance", new SurveillanceTarget() },
            { "analytics", new CopAnalyticsTarget() },
        };
        public static void RunCommand(List<string> args)
        {
            if (args.Count == 2 && args[1].ToLower() == "help")
            {
                Help();
                return;
            }

            if (args.Count == 3 && args[1].ToLower() == "enable" && args[2].ToLower() == "logs")
            {
                isLoggingEnabled = true;
                return;
            }

            if (args.Count < 3)
            {
                Log("Usage: nacops (action) (target) (index or argument)\n    Try: nacops help");
                return;
            }

            string actionStr = args[1].ToLower();
            string targetStr = args[2].ToLower();
            // Try parse index
            int index = args.Count > 3 && Int32.TryParse(args[3], out index) ? index : -1;
            bool useStringArgs = false;
            // if not index try parse start or stop
            if (index == -1 && args.Count > 3 && (args[3].ToLower() == "start" || args[3].ToLower() == "stop"))
                useStringArgs = true;

            if (!consoleTargets.TryGetValue(targetStr, out ConsoleCommandBase target))
            {
                Log($"Unknown command target '{targetStr}'");
                return;
            }

            CommandSupport requestedMethod = actionStr switch
            {
                "list" => CommandSupport.List,
                "spawn" => CommandSupport.Spawn | CommandSupport.SpawnNoIndex,
                "visualize" => CommandSupport.Visualize,
                "build" => CommandSupport.Build,
                _ => CommandSupport.None
            };

            if ((target.SupportedMethods & requestedMethod) == 0)
            {
                Log($"Command target '{targetStr}' does not support requested method '{requestedMethod}'");
                return;
            }

            if (requestedMethod == CommandSupport.Build && !useStringArgs)
            {
                Log($"Command requested method 'build {targetStr}' only supports arguments 'start' and 'stop'");
                return;
            }

            switch (requestedMethod)
            {
                case CommandSupport.List:
                    target.List();
                    break;

                case CommandSupport.Spawn | CommandSupport.SpawnNoIndex:
                    target.Spawn(index);
                    break;

                case CommandSupport.Visualize:
                    target.Visualize(index);
                    break;

                case CommandSupport.Build:
                    target.Build(args[3]);
                    break;
            }
        }
        public static void Help()
        {
            string listmessage = "";
            listmessage += "\nSupported Commands:";
            listmessage += $"\n\n# ENABLE FULL LOGGING";
            listmessage += $"\nnacops enable logs";

            foreach (ConsoleCommandBase target in consoleTargets.Values)
            {
                listmessage += $"\n\n# {target.Name.ToUpper()}";
                if (target.SupportedMethods.HasFlag(CommandSupport.List))
                    listmessage += $"\nnacops list {target.Name}";
                if (target.SupportedMethods.HasFlag(CommandSupport.Spawn))
                    listmessage += $"\nnacops spawn {target.Name} (index)";
                if (target.SupportedMethods.HasFlag(CommandSupport.SpawnNoIndex))
                    listmessage += $"\nnacops spawn {target.Name}";
                if (target.SupportedMethods.HasFlag(CommandSupport.Visualize))
                {
                    if (target is CopAnalyticsTarget || target is SurveillanceTarget)
                        listmessage += $"\nnacops visualize {target.Name}";
                    else
                        listmessage += $"\nnacops visualize {target.Name} (index)";
                }
                if (target.SupportedMethods.HasFlag(CommandSupport.Build))
                {
                    listmessage += $"\nnacops build {target.Name} start";
                    listmessage += $"\nnacops build {target.Name} stop";
                }
            }
            Log(listmessage);
            return;
        }

        #endregion

#if DEBUG

        #region Cops data analytics DEBUG configuration only
        public static void OnRuntimeAnalyticsBuildEnd()
        {
            MelonLogger.Msg("Failed to run runtime analytics! Command only available in DEBUG builds. Build mod from source using the DEBUG ");

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
                string filePath = GetPathTo("WeeklyOfficerRequirementsRuntime.csv");

                File.WriteAllText(filePath, csvContent.ToString());

                Log($"Saved static officer weekly requirements to: {filePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning(ex);
            }

            hrReqList.Clear();
            exportingAnalytics = false;
        }
        public static void EvaluateLawSettingsState()
        {
            if (exportingAnalytics) return;
            if (!ConsoleModule.isBuilding) return;

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
                        checkpoints += checkpointInstance.MinMembers;
                        uniqueActive++;
                        checkpointsInstances++;
                    }
                }
                foreach (PatrolInstance patrolInstance in currentSettings.Patrols)
                {
                    if (TimeManager.IsGivenTimeWithinRange(time, patrolInstance.StartTime, patrolInstance.EndTime))
                    {
                        //Log($"    Patrol  +{patrolInstance.Members}");
                        patrols += patrolInstance.MinMembers;
                        uniqueActive++;
                        patrolsInstances++;
                    }
                }
                foreach (SentryInstance sentryInstance in currentSettings.Sentries)
                {
                    if (TimeManager.IsGivenTimeWithinRange(time, sentryInstance.StartTime, sentryInstance.EndTime))
                    {
                        //Log($"    Sentry  +{sentryInstance.Members}");
                        sentrys += sentryInstance.MinMembers;
                        uniqueActive++;
                        sentrysInstances++;
                    }
                }
                foreach (VehiclePatrolInstance vehiclePatrolInstance in currentSettings.VehiclePatrols)
                {
                    // Based on source this is the only way that it can be active during the hour
                    if (vehiclePatrolInstance.activeOfficer != null)
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
                Log($"Police Station: \n    - Occupants: {PoliceStation.PoliceStations[0].OfficerPool.Count} / {origCount}");

                HourlyRequirements currHr = new();
                currHr.Time = time;
                currHr.RequiredOfficers = tot;
                currHr.ActiveActivities = uniqueActive;
                currHr.StationOccupants = PoliceStation.PoliceStations[0].OfficerPool.Count;
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
                    if (TimeManager.IsValid24HourTime(hrTime))
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
                string filePath = GetPathTo("WeeklyOfficerRequirementsStatic.csv");

                File.WriteAllText(filePath, csvContent.ToString());

                Log($"Saved static officer weekly requirements to: {filePath}");
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
                    checkpoints += instance.MinMembers;
                    uniqueActive++;
                    checkpointInstances++;
                }
            }
            // Foot Patrols
            foreach (var instance in settings.Patrols)
            {
                if (TimeManager.IsGivenTimeWithinRange(time, instance.StartTime, instance.EndTime))
                {
                    patrols += instance.MinMembers;
                    uniqueActive++;
                    patrolsInstances++;
                }
            }
            // Sentries
            foreach (var instance in settings.Sentries)
            {
                if (TimeManager.IsGivenTimeWithinRange(time, instance.StartTime, instance.EndTime))
                {
                    sentrys += instance.MinMembers;
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
        #endregion

#endif

    }

    // Patch the Console Submit command functions to add the Debug commands
#if MONO
    [HarmonyPatch(typeof(ConsoleType), "SubmitCommand", new Type[] { typeof(List<string>) })]
#else
    [HarmonyPatch(typeof(ConsoleType), "SubmitCommand", new Type[] { typeof(Il2CppSystem.Collections.Generic.List<string>) })]
#endif
    public static class Console_SubmitCommand_ListString_Patch
    {
#if MONO
        public static bool Prefix(ConsoleType __instance, List<string> args)
        {
#else
        public static bool Prefix(ConsoleType __instance, Il2CppSystem.Collections.Generic.List<string> args)
        {
            List<string> managedArgs = new();
            foreach (string arg in args) // convert from il2cpp list object to normal
                managedArgs.Add(arg);
#endif
        
            if (args.Count == 0) return true; 
            if (args[0].ToLower() == "nacops")
            {
#if MONO
                RunCommand(args);
#else
                RunCommand(managedArgs);
#endif
                return true;
            }
            return true;

        }
    }


    // This because it needs to be patched for the above patch to work
    [HarmonyPatch(typeof(ConsoleType), "SubmitCommand", new Type[] { typeof(string) })]
    public static class Console_SubmitCommand_String_Patch
    {
        public static bool Prefix(ConsoleType __instance, string args)
        {
            return true;
        }
    }
}
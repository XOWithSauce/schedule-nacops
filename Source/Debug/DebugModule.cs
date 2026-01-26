
using HarmonyLib;
using MelonLoader;
using System.Runtime.CompilerServices;
using UnityEngine;

using static NACopsV1.ConsoleModule;
using static NACopsV1.DebugModule;

#if MONO
using ConsoleType = ScheduleOne.Console;
#else
using ConsoleType = Il2CppScheduleOne.Console;
#endif

namespace NACopsV1
{
    public static class DebugModule
    {
        public static int origCount = 0;
        public static Material lineRenderMat;
        public static List<GameObject> pathVisualizer = new();

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
            { "investigator", new InvestigatorTarget() }
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
                    listmessage += $"\nnacops visualize {target.Name} (index)";
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
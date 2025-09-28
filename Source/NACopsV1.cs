using System.Collections;

using HarmonyLib;
using MelonLoader;
using UnityEngine;

using static NACopsV1.BaseUtility;
using static NACopsV1.ConfigLoader;
using static NACopsV1.CrazyCops;
using static NACopsV1.FootPatrolGenerator;
using static NACopsV1.LethalCops;
using static NACopsV1.NearbyCrazyCops;
using static NACopsV1.OfficerOverrides;
using static NACopsV1.PrivateInvestigator;
using static NACopsV1.SentryGenerator;
using static NACopsV1.DebugModule;


#if MONO
using ScheduleOne.Law;
using ScheduleOne.Persistence;
using ScheduleOne.Police;
using ScheduleOne.UI;
using ScheduleOne.UI.MainMenu;
using ScheduleOne.DevUtilities;
using FishNet.Managing;
using FishNet.Object;
#else
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.MainMenu;
using Il2CppScheduleOne.DevUtilities;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Object;
#endif

[assembly: MelonInfo(typeof(NACopsV1.NACops), NACopsV1.BuildInfo.Name, NACopsV1.BuildInfo.Version, NACopsV1.BuildInfo.Author, NACopsV1.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace NACopsV1
{
    public static class BuildInfo
    {
        public const string Name = "NACopsV1";
        public const string Description = "Crazyyyy cops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.9.0";
        public const string DownloadLink = null;
    }

    public class NACops : MelonMod
    {
        public static NACops Instance { get; private set; }

        public static ModConfig currentConfig;
        public static NAOfficerConfig officerConfig;
        public static List<object> coros = new();

        public static readonly HashSet<PoliceOfficer> allActiveOfficers = new();

        public static HashSet<PoliceOfficer> currentDrugApprehender = new HashSet<PoliceOfficer>();
        public static HashSet<PoliceOfficer> currentSummoned = new HashSet<PoliceOfficer>();

        public static int currentPICount = 0;

        public static bool registered = false;
        public static bool lastSaveLoad = false;
        public static bool firstTimeLoad = false;

        public static NetworkObject policeBase;

        public static NetworkManager networkManager;

        public static int sessionPropertyHeat = 0;

        public static List<LawActivitySettings> generatedLawSettings = new();

        #region static waits
        public static WaitForSeconds Wait01 = new WaitForSeconds(0.1f);
        public static WaitForSeconds Wait05 = new WaitForSeconds(0.5f);
        public static WaitForSeconds Wait1 = new WaitForSeconds(1f);
        public static WaitForSeconds Wait2 = new WaitForSeconds(2f);
        public static WaitForSeconds Wait5 = new WaitForSeconds(5f);
        public static WaitForSeconds Wait30 = new WaitForSeconds(30f);
        #endregion

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            Instance = this;
            MelonLogger.Msg("NACops Mod Loaded");
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                if (LoadManager.Instance != null && !registered && !firstTimeLoad)
                {
                    firstTimeLoad = true;
#if MONO
                    LoadManager.Instance.onLoadComplete.AddListener(OnLoadCompleteCb);
#else
                    LoadManager.Instance.onLoadComplete.AddListener((UnityEngine.Events.UnityAction)OnLoadCompleteCb);
#endif
                }
            }
            if (buildIndex != 1)
            {
                if (registered)
                {
                    ExitPreTask();
                }
            }
        }

        public override void OnUpdate()
        {
            DebugModule.OnInput();
        }

        private void OnLoadCompleteCb()
        {
            if (registered) return;
            registered = true;

            currentConfig = ConfigLoader.LoadModConfig();
            officerConfig = ConfigLoader.LoadOfficerConfig();

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>(true);

            SetPoliceNPC();

            coros.Add(MelonCoroutines.Start(Setup()));

           
        }

        public static IEnumerator Setup()
        {
            yield return Wait5;
            yield return MelonCoroutines.Start(StationInit());
            yield return Wait2;
            yield return MelonCoroutines.Start(OfficersInit());
            yield return Wait2;
            coros.Add(MelonCoroutines.Start(RunCoros()));

            // stripped if not debug
            LogStateHourly();
        }

        public static IEnumerator OfficersInit()
        {
            Log("Officers Init");
            if (officerConfig.ModAddedOfficersCount != 0)
                yield return MelonCoroutines.Start(ExtendOfficerPool());

            // Officers original populate
            allActiveOfficers.Clear();
            foreach (var o in UnityEngine.Object.FindObjectsOfType<PoliceOfficer>(true))
            {
                allActiveOfficers.Add(o);
            }

            coros.Add(MelonCoroutines.Start(SetOfficers()));

            yield return null;
        }

        public static IEnumerator StationInit() // is override enough here or do the patrols/sentries need to be assigned to weekday settings too
        {
            Log("Generating Law settings");

            // 2 modes, simple override from current day as template or override each weekday with new arr (might cause more lag tho?)
            bool overrideAllDays = false;

            if (overrideAllDays)
            {
                Log("Override Law Settings");
                // because the current doesnt always exist? load time problem
                bool settingsDontExist = Singleton<LawController>.Instance.CurrentSettings == null;
                Log($"Current is Null : {settingsDontExist}");
                // else apply template from monday if it exists?
                bool mondaySettingsDontExist = Singleton<LawController>.Instance.MondaySettings == null;
                Log($"Monday settings is null:  {mondaySettingsDontExist}");

                LawActivitySettings template;
                if (!settingsDontExist)
                {
                    template = Singleton<LawController>.Instance.CurrentSettings;
                }
                else if (settingsDontExist && !mondaySettingsDontExist)
                {
                    template = Singleton<LawController>.Instance.MondaySettings;
                }
                else
                {
                    Log("No usable law activity settings to copy from");
                    yield break;
                }

                LawActivitySettings settings = new();
                settings.Curfews = template.Curfews;
                settings.VehiclePatrols = template.VehiclePatrols;
                settings.Checkpoints = template.Checkpoints;


                if (currentConfig.ExtraOfficerPatrols)
                {
                    Log("Gen patrol");
                    settings.Patrols = GeneratePatrol(template);
                }
                else
                    settings.Patrols = template.Patrols;

                if (currentConfig.ExtraOfficerSentries)
                {
                    Log("Gen sentries");
                    settings.Sentries = GenerateSentry(template);
                }
                else
                    settings.Sentries = template.Sentries;

                generatedLawSettings.Add(settings);
                Log("Override");
                Singleton<LawController>.Instance.OverrideSetings(settings);
                Log("Settings generated and overridden \n     Controller state: " + Singleton<LawController>.Instance.OverrideSettings);
            }
            else
            {
                Log("Apply Custom to All Days");

                // map the day string
                Dictionary<string, LawActivitySettings> daySettings = new Dictionary<string, LawActivitySettings>
                {
                    { "mon", Singleton<LawController>.Instance.MondaySettings },
                    { "tue", Singleton<LawController>.Instance.TuesdaySettings },
                    { "wed", Singleton<LawController>.Instance.WednesdaySettings },
                    { "thu", Singleton<LawController>.Instance.ThursdaySettings },
                    { "fri", Singleton<LawController>.Instance.FridaySettings },
                    { "sat", Singleton<LawController>.Instance.SaturdaySettings },
                    { "sun", Singleton<LawController>.Instance.SundaySettings }
                };

                foreach (KeyValuePair<string, LawActivitySettings> kvp in daySettings)
                {
                    Log("Generating patrols and sentries for day: " + kvp.Key);
                    string dayCode = kvp.Key;
                    LawActivitySettings settings = new();

                    settings.Curfews = kvp.Value.Curfews;
                    settings.VehiclePatrols = kvp.Value.VehiclePatrols;
                    settings.Checkpoints = kvp.Value.Checkpoints;

                    if (currentConfig.ExtraOfficerPatrols)
                    {
                        Log("Gen patrol");
                        settings.Patrols = GeneratePatrol(kvp.Value, kvp.Key);
                    }
                    else
                        settings.Patrols = kvp.Value.Patrols;

                    if (currentConfig.ExtraOfficerSentries)
                    {
                        Log("Gen sentries");
                        settings.Sentries = GenerateSentry(kvp.Value, kvp.Key);
                    }
                    else
                        settings.Sentries = kvp.Value.Sentries;

                    generatedLawSettings.Add(settings);

                    switch (kvp.Key) // because kvp.value isnt assignable
                    {
                        case "mon":
                            Singleton<LawController>.Instance.MondaySettings = settings;
                            break;
                        case "tue":
                            Singleton<LawController>.Instance.TuesdaySettings = settings;
                            break;
                        case "wed":
                            Singleton<LawController>.Instance.WednesdaySettings = settings;
                            break;
                        case "thu":
                            Singleton<LawController>.Instance.ThursdaySettings = settings;
                            break;
                        case "fri":
                            Singleton<LawController>.Instance.FridaySettings = settings;
                            break;
                        case "sat":
                            Singleton<LawController>.Instance.SaturdaySettings = settings;
                            break;
                        case "sun":
                            Singleton<LawController>.Instance.SundaySettings = settings;
                            break;
                    }
                }
            }

            yield return null;
        }

        public static IEnumerator RunCoros()
        {
            Log("Coros begin");
            if (currentConfig.CrazyCops)
                coros.Add(MelonCoroutines.Start(RunCrazyCops()));
            if (currentConfig.NearbyCrazyCops)
                coros.Add(MelonCoroutines.Start(RunNearbyCrazyCops()));
            if (currentConfig.LethalCops)
                coros.Add(MelonCoroutines.Start(RunNearbyLethalCops()));
            if (currentConfig.PrivateInvestigator)
                coros.Add(MelonCoroutines.Start(RunInvestigator()));

            yield return null;
        }


        #region Harmony Patches for exiting coros
        static void ExitPreTask()
        {
            registered = false;
            foreach (object coro in coros)
            {
                if (coro != null)
                    MelonCoroutines.Stop(coro);
            }
            allActiveOfficers.Clear();
            coros.Clear();
            currentSummoned.Clear();
            currentDrugApprehender.Clear();
            Player_ConsumeProduct_Patch.evaluating = false;
            generatedOfficerPool.Clear();
            generatedLawSettings.Clear();
            generatedPatrolInstances.Clear();
            serPatrols = null;
            generatedSentryInstances.Clear();
            serSentries = null;

#if MONO
#if DEBUG
            hrReqList.Clear();
#endif
#endif
        }

        [HarmonyPatch(typeof(LoadManager), "ExitToMenu")]
        public static class LoadManager_ExitToMenu_Patch
        {
            public static bool Prefix(LoadManager __instance, SaveInfo autoLoadSave = null, MainMenuPopup.Data mainMenuPopup = null, bool preventLeaveLobby = false)
            {
                ExitPreTask();
                return true;
            }
        }

        [HarmonyPatch(typeof(DeathScreen), "LoadSaveClicked")]
        public static class DeathScreen_LoadSaveClicked_Patch
        {
            public static bool Prefix(DeathScreen __instance)
            {
                ExitPreTask();
                return true;
            }
        }
#endregion


    }
}

using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using System.Reflection;

using static NACops.ConfigLoader;
using static NACops.FootPatrolGenerator;
using static NACops.VehiclePatrolGenerator;
using static NACops.SentryGenerator;
using static NACops.LethalCops;
using static NACops.NearbyCrazyCops;
using static NACops.OfficerOverrides;
using static NACops.PrivateInvestigator;
using static NACops.DebugModule;
using static NACops.NoticeOpenCarry;
using static NACops.RacistOfficers;
using static NACops.RaidPropertyEvent;
using static NACops.RuntimeImpostor;
using static NACops.CopInitHelper;
using static NACops.MassSurveillance;
using static NACops.AvatarUtility;
using static NACops.ConsoleModule;

#if MONO
using ScheduleOne.Law;
using ScheduleOne.Persistence;
using ScheduleOne.Police;
using ScheduleOne.GameTime;
using ScheduleOne.UI;
using ScheduleOne.UI.MainMenu;
using ScheduleOne.PlayerScripts;
using ScheduleOne.DevUtilities;
using FishNet.Managing;
#else
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.MainMenu;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.Map;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Object;
#endif

[assembly: MelonInfo(typeof(NACops.NACops), NACops.BuildInfo.Name, NACops.BuildInfo.Version, NACops.BuildInfo.Author, NACops.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]

#if MONO
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
[assembly: MelonLoader.VerifyLoaderVersion("0.7.2", true)]
#else 
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
[assembly: MelonLoader.VerifyLoaderVersion("0.7.2", true)]
#endif

namespace NACops
{
    public static class BuildInfo
    {
        public const string Name = "NACops";
        public const string Description = "Crazyyyy cops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "2.1.0";
        public const string DownloadLink = null;
    }

    public class NACops : MelonMod
    {
        public static NACops Instance { get; private set; }
        public static ModPrefsHandler Prefs { get; private set; }
        public static ModConfig currentConfig;
        public static NAOfficerConfig officerConfig;
        public static ThresholdMappings thresholdConfig;
        public static RaidConfig raidConfig;
        public static MassSurveillanceConfig surveillanceConfig;

        public static object heatConfigLock = new object();
        public static List<PropertyHeat> heatConfig;

        public static bool isSaving = false;

        public static List<object> coros = new();

        public static readonly HashSet<PoliceOfficer> allActiveOfficers = new();

        public static HashSet<PoliceOfficer> currentDrugApprehender = new HashSet<PoliceOfficer>();

        public static bool registered = false;
        public static bool lastSaveLoad = false;
        public static bool firstTimeLoad = false;
        public static bool hasInitiatedAllOfficers = false;
        public static NetworkManager networkManager;

        public static List<LawActivitySettings> generatedLawSettings = new();

        #region static waits
        public static WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();
        public static WaitForSeconds Wait01 = new WaitForSeconds(0.1f);
        public static WaitForSeconds Wait05 = new WaitForSeconds(0.5f);
        public static WaitForSeconds Wait1 = new WaitForSeconds(1f);
        public static WaitForSeconds Wait2 = new WaitForSeconds(2f);
        public static WaitForSeconds Wait5 = new WaitForSeconds(5f);
        public static WaitForSeconds Wait30 = new WaitForSeconds(30f);
        #endregion

        #region Melon Prefs
        // On init sync .json config based on melon preferences if they differ from default
        // TODO ADD the officer config stuffs so instead of just bool check floats ints etc
        public static void SyncConfig()
        {
            bool hasChanged = false;
            FieldInfo[] modConfigFields = currentConfig.GetType().GetFields();
            foreach (FieldInfo field in modConfigFields)
            {
                var entry = Prefs.modConfigCategory.GetEntry(field.Name);
                if (entry == null) continue;

                if ((bool)field.GetValue(currentConfig) == (bool)entry.BoxedValue)
                {
                    Log("No changed value for :" + field.Name);
                    continue; // not changed
                }
                else
                {
                    hasChanged = true;
                    Log("Update config value for :" + field.Name);
                    field.SetValue(currentConfig, entry.BoxedValue);
                }
            }

            if (hasChanged)
            {
                ConfigLoader.Save(currentConfig, logConfirm: false);
            }
        }
        #endregion

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            Instance = this;

            currentConfig = ConfigLoader.LoadModConfig();

            Prefs = new ModPrefsHandler();
            Prefs.SetupMelonPreferences();
            SyncConfig();
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
        private void OnLoadCompleteCb()
        {
            if (registered) return;
            registered = true;
            coros.Add(MelonCoroutines.Start(Setup()));
        }
        public static IEnumerator Setup()
        {
#if MONO
            yield return new WaitUntil(() => LoadManager.Instance.IsGameLoaded);
#else
            yield return new WaitUntil((Il2CppSystem.Func<bool>)(() => LoadManager.Instance.IsGameLoaded));
#endif
            Log("Loading configs");
            currentConfig = ConfigLoader.LoadModConfig();
            officerConfig = ConfigLoader.LoadOfficerConfig();
            heatConfig = ConfigLoader.LoadPropertyHeats().loadedPropertyHeats;
            thresholdConfig = ConfigLoader.LoadFrequencyConfig();
            raidConfig = ConfigLoader.LoadRaidConfig();
            surveillanceConfig = ConfigLoader.LoadSurveillanceConfig();

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>(true);

            yield return MelonCoroutines.Start(ReplicateCopNPC());

            coros.Add(MelonCoroutines.Start(OfficersInit()));
            coros.Add(MelonCoroutines.Start(SetupMassSurveillance()));
            SetRaidSprite();
            yield return MelonCoroutines.Start(AddDayPassRaid());
            yield return MelonCoroutines.Start(StationInit());
            coros.Add(MelonCoroutines.Start(OpenCarryInit()));

            TimeManager instance = NetworkSingleton<TimeManager>.Instance;
#if MONO
            instance.onHourPass = (Action)Delegate.Combine(instance.onHourPass, new Action(Customer_ProcessHandover_Patch.ReduceBuyBustHours));
#else
            instance.onHourPass += (Il2CppSystem.Action)Customer_ProcessHandover_Patch.ReduceBuyBustHours;
#endif
            Log("Setup complete");
            yield break;
        }

        public static IEnumerator OfficersInit()
        {
            Log("Officers Init");
            allActiveOfficers.Clear();
            yield return Wait01;
            if (officerConfig.ModAddedOfficersCount != 0)
            {
                yield return MelonCoroutines.Start(SpawnOfficersRuntime());
            }
            else
            {
                for (int i = 0; i < PoliceOfficer.Officers.Count; i++)
                    generatedOfficerPool.Add(PoliceOfficer.Officers[i]);
            }
            foreach (PoliceOfficer offc in generatedOfficerPool)
                allActiveOfficers.Add(offc);

            yield return MelonCoroutines.Start(CreateInvestigator());
            yield return MelonCoroutines.Start(CreateBuyBustCop());

            yield return MelonCoroutines.Start(SetOfficers());
            coros.Add(MelonCoroutines.Start(RunCoros()));
            hasInitiatedAllOfficers = true;
            Log("All officer npcs initiated");
            yield break;
        }


        public static IEnumerator OpenCarryInit()
        {
#if MONO
            PlayerInventory.instance.onEquippedSlotChanged = (Action<int>)Delegate.Combine(PlayerInventory.instance.onEquippedSlotChanged, new Action<int>(OnSlotChanged)); // add state brandishing
#else
            PlayerInventory.instance.onEquippedSlotChanged += (Il2CppSystem.Action<int>)OnSlotChanged;
#endif

#if MONO
            Player.Local.onArrested += OnPlayerArrested;
#else
            Player.Local.onArrested += (Il2CppSystem.Action)OnPlayerArrested;
#endif
            SetWeaponsLegalStatus();
            Log("Enabled No Open Carry Weapons");
            yield break;
        }
        public static IEnumerator AddDayPassRaid()
        {
            if (!currentConfig.RaidsEnabled) yield break;
#if MONO
            NetworkSingleton<TimeManager>.Instance.onSleepEnd += OnDayPassEvaluateRaid;
#else
            NetworkSingleton<TimeManager>.Instance.onSleepEnd += (Il2CppSystem.Action)OnDayPassEvaluateRaid;
#endif
        }
        public static IEnumerator StationInit()
        {
            Log("Generating Law settings");
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
                Log("Generating patrols, vehicle patrols and sentries for day: " + kvp.Key);
                string dayCode = kvp.Key;
                LawActivitySettings settings = new();

                settings.Curfews = kvp.Value.Curfews;

                if (currentConfig.CheckpointsEnabled)
                    settings.Checkpoints = kvp.Value.Checkpoints;
                else
                    settings.Checkpoints = new CheckpointInstance[0];

                if (currentConfig.ExtraOfficerPatrols)
                {
                    Log("Gen patrol");
                    settings.Patrols = GeneratePatrol(kvp.Value, kvp.Key);
                }
                else
                    settings.Patrols = kvp.Value.Patrols;

                if (currentConfig.ExtraVehiclePatrols)
                {
                    Log("Gen vehicle patrol");
                    settings.VehiclePatrols = GenerateVehiclePatrol(kvp.Value, kvp.Key);
                }
                else
                    settings.VehiclePatrols = kvp.Value.VehiclePatrols;

                if (currentConfig.ExtraOfficerSentries)
                {
                    Log("Gen sentries");
                    settings.Sentries = GenerateSentry(kvp.Value, kvp.Key);
                }
                else
                    settings.Sentries = kvp.Value.Sentries;

                generatedLawSettings.Add(settings);

                switch (kvp.Key)
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

            yield break;
        }
        public static IEnumerator RunCoros()
        {
            Log("Coros begin");
            coros.Add(MelonCoroutines.Start(RunNearbyCrazyCops()));
            coros.Add(MelonCoroutines.Start(RunNearbyLethalCops()));
            coros.Add(MelonCoroutines.Start(EvaluateOfficersVision()));
            coros.Add(MelonCoroutines.Start(RunInvestigator()));
            yield break;
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
            currentDrugApprehender.Clear();
            Player_ConsumeProduct_Patch.evaluating = false;
            hasInitiatedAllOfficers = false;
            Customer_ProcessHandover_Patch.cooldownHours = 3;

            generatedOfficerPool.Clear();
            generatedLawSettings.Clear();
            copBaseClone = null;
            investigator = null;
            investigatorID = 0;
            isTakingPhotos = false;
            buyBustCop = null;
            buyBustCopID = 0;
            
            generatedPatrolInstances.Clear();
            serPatrols = null;
            generatedVehiclePatrolInstances.Clear();
            serVehiclePatrols = null;
            generatedSentryInstances.Clear();
            serSentries = null;

            HasSetBrandishing = false;
            IsCheckingSlot = false;
            rangedWeaponPrefab = null;
            if (createdTextures.Count > 0)
            {
                // Clear investigator avatar impostor textures
                foreach (Texture2D texture in createdTextures.Values)
                    if (texture != null)
                        UnityEngine.Object.Destroy(texture);
                createdTextures.Clear();
            }
            if (instancedSettings.Count > 0)
            {
                // Destroy ScriptableObject instanced avatar settings
                foreach (var kvp in instancedSettings)
                    if (kvp.Value != null)
                        UnityEngine.Object.DestroyImmediate(kvp.Value);
                instancedSettings.Clear();
            }
            networkManager = null;

            heatConfig.Clear();
            ResetRaidEvent();
            ResetMassSurveillance();

            maleVOs.Clear();
            femaleVOs.Clear();

            // reset console and debug related
            DebugModule.pathVisualizer.Clear();
            CopAnalyticsTarget.AnalyticsTextPanel = null;
            SurveillanceTarget.hasDrawnVisuals = false;
            FootPatrolTarget.currentPathName = "";
            FootPatrolTarget.recordedPathNodes.Clear();
            SentryTarget.currentPathName = "";
            SentryTarget.recordedPathNodes.Clear();
            VehiclePatrolTarget.currentPathName = "";
            VehiclePatrolTarget.recordedPathNodes.Clear();
            ConsoleModule.isBuilding = false;
            CopAnalyticsTarget.AnalyticsTextPanel = null;
#if DEBUG
            // the csv export related need to be reset too
            DebugModule.origCount = 0;
            DebugModule.hrReqList.Clear();
            DebugModule.exportingAnalytics = false;
#endif
            return;
        }


        [HarmonyPatch(typeof(SaveManager), "Save", new Type[] { typeof(string) })]
        public static class SaveManager_Save_String_Patch
        {
            public static bool Prefix(SaveManager __instance, string saveFolderPath)
            {
                if (!isSaving)
                {
                    isSaving = true;
                    lock (heatConfigLock)
                    {
                        PropertiesHeatSerialized heats = new();
                        heats.loadedPropertyHeats = new(heatConfig);
                        ConfigLoader.Save(heats);
                    }
                }
                isSaving = false;
                return true;
            }
        }

        [HarmonyPatch(typeof(SaveManager), "Save", new Type[] { })]
        public static class SaveManager_Save_Patch
        {
            public static bool Prefix(SaveManager __instance)
            {
                return true;
            }
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

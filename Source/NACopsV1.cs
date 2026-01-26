using System.Collections;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

using static NACopsV1.BaseUtility;
using static NACopsV1.ConfigLoader;
using static NACopsV1.FootPatrolGenerator;
using static NACopsV1.VehiclePatrolGenerator;
using static NACopsV1.SentryGenerator;
using static NACopsV1.LethalCops;
using static NACopsV1.NearbyCrazyCops;
using static NACopsV1.OfficerOverrides;
using static NACopsV1.PrivateInvestigator;
using static NACopsV1.DebugModule;
using static NACopsV1.NoticeOpenCarry;
using static NACopsV1.RacistOfficers;
using static NACopsV1.RaidPropertyEvent;

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
using FishNet.Object;
#else
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.MainMenu;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Object;
#endif

[assembly: MelonInfo(typeof(NACopsV1.NACops), NACopsV1.BuildInfo.Name, NACopsV1.BuildInfo.Version, NACopsV1.BuildInfo.Author, NACopsV1.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]

/*
 Todays agenda:

 - Ensure that Il2Cpp conversion worked
- Test again

- Raid -> lock door to prevent cops from entering the apartment immeaditely?
    -> Bust down the door anim+ logic?
    --> Disable the default anim for opening doors to avoid overlap

- Cops can raid businesses?

- Cop disguises as customer and tries to deal with pllayer
--> has dealing related behaviour by default?
--> set name + avatar settings identical to customer
---> msg conv manually?
---> Should be detectable (VO emitter radio rarely or conversation differs from usual customer conv)

- Cops busting player hired dealers
--> msg from dealer (i just got busted by the popo)

- New feature: DecreaseStatus : true
---> Decreases pursuit level / crime wanted level overtime to next lower status instead of resetting to None

- Feature to disable question mark from vision events of police
- Undercover cop cars so driving civ vehs

 */

namespace NACopsV1
{
    public static class BuildInfo
    {
        public const string Name = "NACopsV1";
        public const string Description = "Crazyyyy cops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "2.0.0";
        public const string DownloadLink = null;
    }

    public class NACops : MelonMod
    {
        public static NACops Instance { get; private set; }

        public static ModConfig currentConfig;
        public static NAOfficerConfig officerConfig;
        public static ThresholdMappings thresholdConfig;
        public static RaidConfig raidConfig;

        public static object heatConfigLock = new object();
        public static List<PropertyHeat> heatConfig;

        public static bool isSaving = false;

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

            yield return Wait5;
            currentConfig = ConfigLoader.LoadModConfig();
            officerConfig = ConfigLoader.LoadOfficerConfig();
            heatConfig = ConfigLoader.LoadPropertyHeats().loadedPropertyHeats;
            thresholdConfig = ConfigLoader.LoadFrequencyConfig();
            raidConfig = ConfigLoader.LoadRaidConfig();

            networkManager = UnityEngine.Object.FindObjectOfType<NetworkManager>(true);

            SetRaidSprite();
            SetPoliceNPC();
            yield return MelonCoroutines.Start(AddDayPassRaid());
            yield return MelonCoroutines.Start(StationInit());
            yield return MelonCoroutines.Start(OfficersInit());
            coros.Add(MelonCoroutines.Start(OpenCarryInit()));
            coros.Add(MelonCoroutines.Start(RunCoros()));
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
        public static IEnumerator OpenCarryInit()
        {
            if (!currentConfig.NoOpenCarryWeapons) yield break;
#if MONO
            PlayerInventory.instance.onEquippedSlotChanged = (Action<int>)Delegate.Combine(PlayerInventory.instance.onEquippedSlotChanged, new Action<int>(OnSlotChanged));
#else
            PlayerInventory.instance.onEquippedSlotChanged += (Il2CppSystem.Action<int>)OnSlotChanged;
#endif
            void OnInventoryChange(bool b)
            {
                OnSlotChanged(1);
            }
#if MONO
            PlayerInventory.instance.onInventoryStateChanged = (Action<bool>)Delegate.Combine(PlayerInventory.instance.onInventoryStateChanged, new Action<bool>(OnInventoryChange));
#else
            PlayerInventory.instance.onInventoryStateChanged += (Il2CppSystem.Action<bool>)OnInventoryChange;
#endif
            Player.Local.onArrested.AddListener((UnityEngine.Events.UnityAction)OnPlayerArrested);
            SetWeaponsLegalStatus();
            Log("Enabled No Open Carry Weapons");
            yield return null;
        }
        public static IEnumerator AddDayPassRaid()
        {
            if (!currentConfig.RaidsEnabled) yield break;
#if MONO
            NetworkSingleton<TimeManager>.Instance.onDayPass += OnDayPassEvaluateRaid;
#else
            NetworkSingleton<TimeManager>.Instance.onDayPass += (Il2CppSystem.Action)OnDayPassEvaluateRaid;
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
                settings.Checkpoints = kvp.Value.Checkpoints;

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

            yield return null;
        }
        public static IEnumerator RunCoros()
        {
            Log("Coros begin");
            if (currentConfig.NearbyCrazyCops)
                coros.Add(MelonCoroutines.Start(RunNearbyCrazyCops()));
            if (currentConfig.LethalCops)
                coros.Add(MelonCoroutines.Start(RunNearbyLethalCops()));
            if (currentConfig.RacistCops)
                coros.Add(MelonCoroutines.Start(EvaluateOfficersVision()));
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
            generatedVehiclePatrolInstances.Clear();
            serVehiclePatrols = null;
            generatedSentryInstances.Clear();
            serSentries = null;
            networkManager = null;
            currentPICount = 0;
            HasSetBrandishing = false;
            IsCheckingSlot = false;
            heatConfig.Clear();
            ResetRaidEvent();
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

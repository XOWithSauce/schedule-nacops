
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Persistence;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Persistence;
#endif

namespace NACopsV1
{

    // Because vector3 isnt just xyz for serialization, we remove everything except xyz from the base object properties
    // Helps with patrols + sentrys serialization
    public class UnityContractResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            JsonObjectContract contract = base.CreateObjectContract(objectType);

            if (objectType == typeof(Vector3))
            {
                for (int i = contract.Properties.Count - 1; i >= 0; i--)
                {
                    var property = contract.Properties[i];
                    if (property.PropertyName == "normalized" || property.PropertyName == "magnitude" || property.PropertyName == "sqrMagnitude")
                    {
                        contract.Properties.RemoveAt(i);
                    }
                }
            }
            return contract;
        }
    }

    public static class ConfigLoader
    {
        private static string modConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "config.json");
        private static string officerConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "officer.json");
        private static string patrolsConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "Spawn", "patrols.json");
        private static string sentrysConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "Spawn", "sentrys.json");
        private static string vehiclePatrolsConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "Spawn", "vehiclepatrols.json");
        private static string eventFrequencyConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "progression.json");
        private static string raidConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "raid.json");
        private static string propertyHeatConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "HeatData"); // /organisation.json


        #region Mod Configurations JSON
        public static ModConfig LoadModConfig()
        {
            ModConfig config;
            if (File.Exists(modConfig))
            {
                try
                {
                    string json = File.ReadAllText(modConfig);
                    config = JsonConvert.DeserializeObject<ModConfig>(json);
                }
                catch (Exception ex)
                {
                    config = new ModConfig();
                    MelonLogger.Warning("Failed to read NACops Mod config: " + ex);
                }
            }
            else
            {
                MelonLogger.Warning("Missing NACops Mod config, creating directory and template.");
                config = new ModConfig();
                Save(config);
            }
            return config;
        }

        public static void Save(ModConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(modConfig));
                File.WriteAllText(modConfig, json);
                MelonLogger.Warning($"    NACops Mod config, written to: {modConfig}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Mod config: " + ex);
            }

        }
        #endregion

        #region Officers Configurations JSON
        public static NAOfficerConfig LoadOfficerConfig()
        {
            NAOfficerConfig config;
            if (File.Exists(officerConfig))
            {
                try
                {
                    string json = File.ReadAllText(officerConfig);
                    config = JsonConvert.DeserializeObject<NAOfficerConfig>(json);
                    config.ModAddedOfficersCount = Mathf.Clamp(config.ModAddedOfficersCount, 0, 20);
                }
                catch (Exception ex)
                {
                    config = new NAOfficerConfig();
                    MelonLogger.Warning("Failed to read NACops config: " + ex);
                }
            }
            else
            {
                MelonLogger.Warning("Missing NACops Officers config, creating directory and template.");
                config = new NAOfficerConfig();
                Save(config);
            }
            return config;
        }

        public static void Save(NAOfficerConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config);
                Directory.CreateDirectory(Path.GetDirectoryName(officerConfig));
                File.WriteAllText(officerConfig, json);
                MelonLogger.Warning($"    NACops Officers config, written to: {officerConfig}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Officers config: " + ex);
            }

        }
        #endregion

        #region Patrols JSON
        public static FootPatrolsSerialized LoadPatrolsConfig()
        {
            FootPatrolsSerialized config;
            if (File.Exists(patrolsConfig))
            {
                try
                {
                    string json = File.ReadAllText(patrolsConfig);
                    config = JsonConvert.DeserializeObject<FootPatrolsSerialized>(json);

                    List<string> weekdays = new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

                    // foreach validate
                    foreach (SerializedFootPatrol ser in config.loadedPatrols)
                    {
                        ser.members = Mathf.Clamp(ser.members, 1, 4);
                        ser.name = string.IsNullOrEmpty(ser.name) ? "NaCopsPatrol " : ser.name;
                        ser.intensityRequirement = Mathf.Clamp(ser.intensityRequirement, 0, 10);
                        if (!TimeManager.IsValid24HourTime(ser.startTime.ToString()))
                        {
                            MelonLogger.Warning($"FootPatrolsConfig '{ser.name}' has invalid start time");
                            ser.startTime = 1900;
                        }
                        if (!TimeManager.IsValid24HourTime(ser.endTime.ToString()))
                        {
                            MelonLogger.Warning($"FootPatrolsConfig '{ser.name}' has invalid end time");
                            ser.endTime = 2330;
                        }
                        if (ser.waypoints.Count == 0)
                        {
                            MelonLogger.Warning($"FootPatrolsConfig is missing Waypoints for {ser.name}");
                        }

                        // Validate weekdays
                        for (int i = ser.days.Count - 1; i != -1; i--)
                        {

                            if (ser.days[i] != string.Empty)
                            {
                                ser.days[i] = ser.days[i].ToLower();
                                if (!weekdays.Contains(ser.days[i]))
                                {
                                    MelonLogger.Warning($"FootPatrolsConfig '{ser.name}' has invalid weekday: '{ser.days[i]}'");
                                    ser.days.RemoveAt(i);
                                }
                            }
                            else //string empty
                            {
                                ser.days.RemoveAt(i);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    config = new FootPatrolsSerialized();
                    MelonLogger.Warning("Failed to read FootPatrolsSerialized config: " + ex);
                }
            }
            else
            {
                config = new FootPatrolsSerialized();
                config.loadedPatrols = new();
                Save(config);
            }
            return config;
        }

        public static void Save(FootPatrolsSerialized config)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new UnityContractResolver()
                };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
                Directory.CreateDirectory(Path.GetDirectoryName(patrolsConfig));
                File.WriteAllText(patrolsConfig, json);
                MelonLogger.Warning($"    Foot Patrols Config has been saved!");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Foot Patrols config: " + ex);
            }
        }
        #endregion

        #region Vehicle Patrols JSON
        public static VehiclePatrolsSerialized LoadVehiclePatrolsConfig()
        {
            VehiclePatrolsSerialized config;
            if (File.Exists(vehiclePatrolsConfig))
            {
                try
                {
                    string json = File.ReadAllText(vehiclePatrolsConfig);
                    config = JsonConvert.DeserializeObject<VehiclePatrolsSerialized>(json);

                    List<string> weekdays = new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

                    // foreach validate
                    foreach (SerializedVehiclePatrol ser in config.loadedVehiclePatrols)
                    {
                        ser.name = string.IsNullOrEmpty(ser.name) ? "NaCopsVehiclePatrol " : ser.name;
                        ser.intensityRequirement = Mathf.Clamp(ser.intensityRequirement, 0, 10);
                        if (!TimeManager.IsValid24HourTime(ser.startTime.ToString()))
                        {
                            MelonLogger.Warning($"Vehicle Patrol Config '{ser.name}' has invalid start time");
                            ser.startTime = 1900;
                        }
                        if (ser.waypoints.Count == 0)
                        {
                            MelonLogger.Warning($"Vehicle Patrol Config is missing Waypoints for {ser.name}");
                        }

                        // Validate weekdays
                        for (int i = ser.days.Count - 1; i != -1; i--)
                        {

                            if (ser.days[i] != string.Empty)
                            {
                                ser.days[i] = ser.days[i].ToLower();
                                if (!weekdays.Contains(ser.days[i]))
                                {
                                    MelonLogger.Warning($"Vehicle Patrol Config '{ser.name}' has invalid weekday: '{ser.days[i]}'");
                                    ser.days.RemoveAt(i);
                                }
                            }
                            else //string empty
                            {
                                ser.days.RemoveAt(i);
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    config = new VehiclePatrolsSerialized();
                    MelonLogger.Warning("Failed to read Vehicle Patrol config: " + ex);
                }
            }
            else
            {
                config = new VehiclePatrolsSerialized();
                config.loadedVehiclePatrols = new();
                Save(config);
            }
            return config;
        }

        public static void Save(VehiclePatrolsSerialized config)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new UnityContractResolver()
                };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
                Directory.CreateDirectory(Path.GetDirectoryName(vehiclePatrolsConfig));
                File.WriteAllText(vehiclePatrolsConfig, json);
                MelonLogger.Warning($"    Vehicle Patrols config has been saved!");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Vehicle Patrols config: " + ex);
            }
        }
        #endregion

        #region Sentrys JSON
        public static SentrysSerialized LoadSentryConfig()
        {
            SentrysSerialized config;
            if (File.Exists(sentrysConfig))
            {
                try
                {
                    string json = File.ReadAllText(sentrysConfig);
                    config = JsonConvert.DeserializeObject<SentrysSerialized>(json);

                    List<string> weekdays = new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

                    // foreach validate
                    foreach (SerializedSentry ser in config.loadedSentrys)
                    {
                        ser.members = Mathf.Clamp(ser.members, 1, 2);
                        ser.name = string.IsNullOrEmpty(ser.name) ? "NACopsSentry " : ser.name;
                        ser.intensityRequirement = Mathf.Clamp(ser.intensityRequirement, 0, 10);
                        if (!TimeManager.IsValid24HourTime(ser.startTime.ToString()))
                        {
                            MelonLogger.Warning($"Sentry Config '{ser.name}' has invalid start time");
                            ser.startTime = 1900;
                        }
                        if (!TimeManager.IsValid24HourTime(ser.endTime.ToString()))
                        {
                            MelonLogger.Warning($"Sentry Config '{ser.name}' has invalid end time");
                            ser.endTime = 2330;
                        }

                        // Validate weekdays
                        for (int i = ser.days.Count - 1; i != -1; i--)
                        {

                            if (ser.days[i] != string.Empty)
                            {
                                ser.days[i] = ser.days[i].ToLower();
                                if (!weekdays.Contains(ser.days[i]))
                                {
                                    MelonLogger.Warning($"Sentry Config '{ser.name}' has invalid weekday: '{ser.days[i]}'");
                                    ser.days.RemoveAt(i);
                                }
                            }
                            else //string empty
                            {
                                ser.days.RemoveAt(i);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    config = new SentrysSerialized();
                    MelonLogger.Warning("Failed to read SentrysSerialized config: " + ex);
                }
            }
            else
            {
                config = new SentrysSerialized();
                config.loadedSentrys = new();
                Save(config);
            }
            return config;
        }

        public static void Save(SentrysSerialized config)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new UnityContractResolver()
                };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
                Directory.CreateDirectory(Path.GetDirectoryName(sentrysConfig));
                File.WriteAllText(sentrysConfig, json);
                MelonLogger.Warning($"    Sentry config has been saved!");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Sentry config: " + ex);
            }
        }
        #endregion

        #region Property Heat Persistent JSON
        public static string SanitizeAndFormatName(string orgName)
        {
            string saveFileName = orgName;

            if (saveFileName != null)
            {
                saveFileName = saveFileName.Replace(" ", "_").ToLower();
                saveFileName = saveFileName.Replace(",", "");
                saveFileName = saveFileName.Replace(".", "");
                saveFileName = saveFileName.Replace("<", "");
                saveFileName = saveFileName.Replace(">", "");
                saveFileName = saveFileName.Replace(":", "");
                saveFileName = saveFileName.Replace("\"", "");
                saveFileName = saveFileName.Replace("/", "");
                saveFileName = saveFileName.Replace("\\", "");
                saveFileName = saveFileName.Replace("|", "");
                saveFileName = saveFileName.Replace("?", "");
                saveFileName = saveFileName.Replace("*", "");
            }
            saveFileName = saveFileName + ".json";
            return saveFileName;
        }

        public static PropertiesHeatSerialized LoadPropertyHeats()
        {
            PropertiesHeatSerialized config;
            string orgName = LoadManager.Instance.ActiveSaveInfo?.OrganisationName;
            string fileName = SanitizeAndFormatName(orgName);
            if (File.Exists(Path.Combine(propertyHeatConfig, fileName)))
            {
                try
                {
                    string json = File.ReadAllText(Path.Combine(propertyHeatConfig, fileName));
                    config = JsonConvert.DeserializeObject<PropertiesHeatSerialized>(json);
                }
                catch (Exception ex)
                {
                    config = new PropertiesHeatSerialized();
                    config.loadedPropertyHeats = new();
                    string[] codes = { "sweatshop", "bungalow", "storageunit", "dockswarehouse", "barn", "manor" };
                    foreach (string code in codes)
                    {
                        PropertyHeat propertyHeat = new();
                        propertyHeat.propertyCode = code;
                        config.loadedPropertyHeats.Add(propertyHeat);
                    }
                    MelonLogger.Warning("Failed to read NACops Property Heat config: " + ex);
                }
            }
            else
            {
                MelonLogger.Warning("Missing NACops Property Heat config, creating directory and template.");
                config = new();
                Save(config, true);
            }
            return config;
        }

        public static void Save(PropertiesHeatSerialized config, bool generateTemplate = false)
        {

            if (generateTemplate)
            {
                config.loadedPropertyHeats = new();
                string[] codes = { "sweatshop", "bungalow", "storageunit", "dockswarehouse", "barn", "manor" };
                foreach (string code in codes)
                {
                    PropertyHeat propertyHeat = new();
                    propertyHeat.propertyCode = code;
                    config.loadedPropertyHeats.Add(propertyHeat);
                }
            }

            try
            {
                string orgName = LoadManager.Instance.ActiveSaveInfo?.OrganisationName;
                string fileName = SanitizeAndFormatName(orgName);
                string saveDestination = Path.Combine(propertyHeatConfig, fileName);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(saveDestination));
                File.WriteAllText(saveDestination, json);
                if (generateTemplate)
                    MelonLogger.Warning($"    NACops Property Heat config, written to: {propertyHeatConfig}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Property Heat config: " + ex);
            }

        }
        #endregion

        #region Event Frequency JSON
        public static ThresholdMappings LoadFrequencyConfig()
        {
            ThresholdMappings config;
            if (File.Exists(eventFrequencyConfig))
            {
                try
                {
                    string json = File.ReadAllText(eventFrequencyConfig);
                    config = JsonConvert.DeserializeObject<ThresholdMappings>(json);
                    // Validate
                    foreach (MinMaxThreshold thres in config.LethalCopFrequency)
                    {
                        if (thres.MinOf < 0)
                            thres.MinOf = 0;
                        if (thres.Min >= thres.Max)
                        {
                            MelonLogger.Warning("Found invalid value in progression.json at LethalCopFreq Min value, must be smaller than Max value");
                            if (thres.Max > 0f)
                                thres.Min = thres.Max * 0.5f;
                        }
                    }
                    foreach (MinMaxThreshold thres in config.LethalCopRange)
                    {
                        if (thres.MinOf < 0)
                            thres.MinOf = 0;
                        if (thres.Min >= thres.Max)
                        {
                            MelonLogger.Warning("Found invalid value in progression.json at LethalCopRange Min value, must be smaller than Max value");
                            if (thres.Max > 0f)
                                thres.Min = thres.Max * 0.5f;
                        }
                    }
                    foreach (MinMaxThreshold thres in config.NearbyCrazyFrequency)
                    {
                        if (thres.MinOf < 0)
                            thres.MinOf = 0;
                        if (thres.Min >= thres.Max)
                        {
                            MelonLogger.Warning("Found invalid value in progression.json at NearbyCrazFreq Min value, must be smaller than Max value");
                            if (thres.Max > 0f)
                                thres.Min = thres.Max * 0.5f;
                        }
                    }
                    foreach (MinMaxThreshold thres in config.NearbyCrazyRange)
                    {
                        if (thres.MinOf < 0)
                            thres.MinOf = 0;
                        if (thres.Min >= thres.Max)
                        {
                            MelonLogger.Warning("Found invalid value in progression.json at NearbyCrazRange Min value, must be smaller than Max value");
                            if (thres.Max > 0f)
                                thres.Min = thres.Max * 0.5f;
                        }
                    }
                    foreach (MinMaxThreshold thres in config.PIFrequency)
                    {
                        if (thres.MinOf < 0)
                            thres.MinOf = 0;
                        if (thres.Min >= thres.Max)
                        {
                            MelonLogger.Warning("Found invalid value in progression.json at PIFreq Min value, must be smaller than Max value");
                            if (thres.Max > 0f)
                                thres.Min = thres.Max * 0.5f;
                        }
                    }
                    foreach (MinMaxThreshold thres in config.SnitchProbability)
                    {
                        if (thres.MinOf < 0)
                            thres.MinOf = 0;
                        if (thres.Min >= thres.Max)
                        {
                            MelonLogger.Warning("Found invalid value in progression.json at SnitchProbability Min value, must be smaller than Max value");
                            if (thres.Max > 0f)
                                thres.Min = thres.Max * 0.5f;
                        }
                    }
                    foreach (MinMaxThreshold thres in config.BuyBustProbability)
                    {
                        if (thres.MinOf < 0)
                            thres.MinOf = 0;

                        if (thres.Min >= thres.Max)
                        {
                            MelonLogger.Warning("Found invalid value in progression.json at BuyBustProbability Min value, must be smaller than Max value");
                            if (thres.Max > 0f)
                                thres.Min = thres.Max * 0.5f;
                        }
                    }

                }
                catch (Exception ex)
                {
                    config = new ThresholdMappings();
                    MelonLogger.Warning("Failed to read NACops Event Frequency config: " + ex);
                }
            }
            else
            {
                MelonLogger.Warning("Missing NACops Event Frequency config, creating directory and template.");
                config = new ThresholdMappings();
                Save(config);
            }
            return config;
        }

        public static void Save(ThresholdMappings config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(eventFrequencyConfig));
                File.WriteAllText(eventFrequencyConfig, json);
                MelonLogger.Warning($"    NACops Event Frequency config, written to: {eventFrequencyConfig}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Event Frequency config: " + ex);
            }

        }
        #endregion

        #region Raid Config JSON
        public static RaidConfig LoadRaidConfig()
        {
            RaidConfig config;
            if (File.Exists(ConfigLoader.raidConfig))
            {
                try
                {
                    string json = File.ReadAllText(ConfigLoader.raidConfig);
                    config = JsonConvert.DeserializeObject<RaidConfig>(json);
                    // Validate to avoid extreme values
                    config.TraverseToPropertySpeed = Mathf.Clamp(config.TraverseToPropertySpeed, 0.1f, 1f);
                    config.ClearPropertySpeed = Mathf.Clamp(config.ClearPropertySpeed, 0.1f, 1f);
                    config.MaxDestroyIters = Mathf.Clamp(config.MaxDestroyIters, 1, 10);
                    config.RaidCopsCount = Mathf.Clamp(config.RaidCopsCount, 1, 10);
                    config.DaysUntilCanRaid = Mathf.Clamp(config.DaysUntilCanRaid, 1, 20);
                    config.PropertyHeatThreshold = Mathf.Clamp(config.PropertyHeatThreshold, 1, 100);
                }
                catch (Exception ex)
                {
                    config = new RaidConfig();
                    MelonLogger.Warning("Failed to read NACops Raid config: " + ex);
                }
            }
            else
            {
                MelonLogger.Warning("Missing NACops Raid config, creating directory and template.");
                config = new RaidConfig();
                Save(config);
            }
            return config;
        }

        public static void Save(RaidConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigLoader.raidConfig));
                File.WriteAllText(ConfigLoader.raidConfig, json);
                MelonLogger.Warning($"    NACops Raid config, written to: {ConfigLoader.raidConfig}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Raid config: " + ex);
            }

        }

        #endregion



        [Serializable]
        public class ModConfig
        {
            public bool DebugMode = false;
            public bool RaidsEnabled = true;
            public bool ExtraOfficerPatrols = true;
            public bool ExtraVehiclePatrols = true;
            public bool ExtraOfficerSentries = true;
            public bool NoOpenCarryWeapons = true;
            public bool PrivateInvestigator = true;
            public bool WeedInvestigator = true;
            public bool CorruptCops = true;
            public bool SnitchingSamples = true;
            public bool BuyBusts = true;
            public bool NearbyCrazyCops = true;
            public bool LethalCops = true;
            public bool RacistCops = false;
        }

        [Serializable]
        public class NAOfficerConfig
        {
            public int ModAddedVehicleCount = 3;
            public int ModAddedOfficersCount = 8;
            public bool CanEnterBuildings = true;
            public bool OverrideArresting = true;
            public float ArrestTime = 1.25f; // def 1.75
            public float ArrestRange = 3.50f; //def 2.75

            public bool OverrideMovement = true;
            public float MovementSpeedMultiplier = 1.7f;

            public bool OverrideWeapon = true;
            public int WeaponMagSize = 20;
            public float WeaponFireRate = 0.33f;
            public float WeaponMaxRange = 25f;
            public float WeaponReloadTime = 0.5f;
            public float WeaponRaiseTime = 0.2f;
            public float WeaponHitChanceMax = 0.3f;
            public float WeaponHitChanceMin = 0.8f;

            public bool OverrideMaxHealth = true;
            public float OfficerMaxHealth = 175f;

            public bool OverrideBodySearch = true;
            public float BodySearchDuration = 6f;
            public float BodySearchChance = 1f;

            public bool OverrideCombatBeh = true;
            public float CombatGiveUpRange = 40f;
            public float CombatGiveUpTime = 60f;
            public float CombatSearchTime = 60f;
            public float CombatMoveSpeed = 6.8f;
            public int CombatEndAfterHits = 40;
        }

        [Serializable] 
        public class RaidConfig
        {
            public float TraverseToPropertySpeed = 0.47f;
            public float ClearPropertySpeed = 0.38f;
            public int MaxDestroyIters = 4;
            public int RaidCopsCount = 3;
            public int DaysUntilCanRaid = 8;
            public int PropertyHeatThreshold = 14;
        }

        [Serializable]
        public class FootPatrolsSerialized
        {
            public List<SerializedFootPatrol> loadedPatrols = new();
        }
        [Serializable]
        public class VehiclePatrolsSerialized
        {
            public List<SerializedVehiclePatrol> loadedVehiclePatrols = new();
        }

        [Serializable]
        public class SentrysSerialized
        {
            public List<SerializedSentry> loadedSentrys = new();
        }

        
    }
}
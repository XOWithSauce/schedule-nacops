using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

using static NACops.ModDataPaths;

#if MONO
using ScheduleOne.GameTime;
using ScheduleOne.Persistence;
#else
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Persistence;
#endif

namespace NACops
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
        #region Mod Configurations JSON
        public static ModConfig LoadModConfig()
        {
            ModConfig config;
            string filePath = GetPathTo(pathModConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
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

        public static void Save(ModConfig config, bool logConfirm = true)
        {
            try
            {
                string filePath = GetPathTo(pathModConfig);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                if (logConfirm)
                    MelonLogger.Warning($"NACops Mod config, written to: {filePath}");
            }
            catch (Exception ex)
            {
                if (logConfirm)
                    MelonLogger.Warning("Failed to save NACops Mod config: " + ex);
            }

        }
        #endregion

        #region Officers Configurations JSON
        public static NAOfficerConfig LoadOfficerConfig()
        {
            NAOfficerConfig config;
            string filePath = GetPathTo(pathOfficerConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    config = JsonConvert.DeserializeObject<NAOfficerConfig>(json);
                    config.ModAddedOfficersCount = Mathf.Clamp(config.ModAddedOfficersCount, 0, 20);

                    List<string> supportedWeaponValues = new List<string>()
                    {
                        "m1911", "goldenm1911", "shotgun", "revolver"
                    };
                    if (!supportedWeaponValues.Contains(config.RangedWeapon))
                    {
                        // Ensure that its supported, if not set default
                        config.RangedWeapon = "m1911";
                    }

                    foreach (var key in config.VisionSpeed.Keys.ToList())
                    {
                        // clamp to range
                        config.VisionSpeed[key] = Mathf.Clamp(config.VisionSpeed[key], 0.01f, 10f);
                    }
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
                string filePath = GetPathTo(pathOfficerConfig);
                string json = JsonConvert.SerializeObject(config);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                MelonLogger.Warning($"NACops Officers config, written to: {filePath}");
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
            string filePath = GetPathTo(pathPatrolsConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    config = JsonConvert.DeserializeObject<FootPatrolsSerialized>(json);

                    List<string> weekdays = new() { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

                    // foreach validate
                    foreach (SerializedFootPatrol ser in config.loadedPatrols)
                    {
                        ser.members = Mathf.Clamp(ser.members, 1, 4);
                        ser.name = string.IsNullOrEmpty(ser.name) ? "NACopsPatrol " : ser.name;
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
                string filePath = GetPathTo(pathPatrolsConfig);
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new UnityContractResolver()
                };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                MelonLogger.Warning($"Foot Patrols Config has been saved!");
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
            string filePath = GetPathTo(pathVehiclePatrolsConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
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
                string filePath = GetPathTo(pathVehiclePatrolsConfig);
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new UnityContractResolver()
                };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                MelonLogger.Warning($"Vehicle Patrols config has been saved!");
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
            string filePath = GetPathTo(pathSentrysConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
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

                        if (ser.minutesPerPoint <= 0 || ser.minutesPerPoint > 480)
                        {
                            MelonLogger.Warning($"Sentry Config '{ser.name}' has invalid minutes per point value. Range 1-480");
                            ser.minutesPerPoint = 60;
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
                string filePath = GetPathTo(pathSentrysConfig);
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new UnityContractResolver()
                };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented, settings);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                MelonLogger.Warning($"Sentry config has been saved!");
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
            string filePath = GetPathTo(pathPropertyHeatConfig);
            string orgName = LoadManager.Instance.ActiveSaveInfo.OrganisationName;
            int slotNumber = LoadManager.Instance.ActiveSaveInfo.SaveSlotNumber;
            string fileName = $"{slotNumber}_{SanitizeAndFormatName(orgName)}";
            if (File.Exists(Path.Combine(filePath, fileName)))
            {
                try
                {
                    string json = File.ReadAllText(Path.Combine(filePath, fileName));
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
            string filePath = GetPathTo(pathPropertyHeatConfig);

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
                string orgName = LoadManager.Instance.ActiveSaveInfo.OrganisationName;
                int slotNumber = LoadManager.Instance.ActiveSaveInfo.SaveSlotNumber;
                string fileName = $"{slotNumber}_{SanitizeAndFormatName(orgName)}";
                string saveDestination = Path.Combine(filePath, fileName);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(saveDestination));
                File.WriteAllText(saveDestination, json);
                if (generateTemplate)
                    MelonLogger.Warning($"NACops Property Heat config, written to: {saveDestination}");
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
            string filePath = GetPathTo(pathEventFrequencyConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
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
                string filePath = GetPathTo(pathEventFrequencyConfig);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                MelonLogger.Warning($"NACops Event Frequency config, written to: {filePath}");
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
            string filePath = GetPathTo(pathRaidConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    config = JsonConvert.DeserializeObject<RaidConfig>(json);
                    // Validate to avoid extreme values
                    config.TraverseToPropertySpeed = Mathf.Clamp(config.TraverseToPropertySpeed, 0.1f, 1f);
                    config.ClearPropertySpeed = Mathf.Clamp(config.ClearPropertySpeed, 0.1f, 1f);
                    config.MaxDestroyIters = Mathf.Clamp(config.MaxDestroyIters, 1, 10);
                    config.RaidCopsCount = Mathf.Clamp(config.RaidCopsCount, 1, 10);
                    config.DaysUntilCanRaid = Mathf.Clamp(config.DaysUntilCanRaid, 1, 20);
                    config.PropertyHeatThreshold = Mathf.Clamp(config.PropertyHeatThreshold, 1, 100);
                    config.RaiderMaxHealth = Mathf.Clamp(config.RaiderMaxHealth, 1, 300);
                    config.RaiderWeaponDmg = Mathf.Clamp(config.RaiderWeaponDmg, 1, 100);

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
                string filePath = GetPathTo(pathRaidConfig);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                MelonLogger.Warning($"NACops Raid config, written to: {filePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Raid config: " + ex);
            }
        }
        #endregion

        #region Mass Surveillance Config JSON
        public static MassSurveillanceConfig LoadSurveillanceConfig()
        {
            MassSurveillanceConfig config;
            string filePath = GetPathTo(pathSurveillanceConfig);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    config = JsonConvert.DeserializeObject<MassSurveillanceConfig>(json);
                    // Validate
                    config.ActiveCamerasPerDay = Mathf.Clamp(config.ActiveCamerasPerDay, 1, 10);
                    config.CameraNoticeCooldown = Mathf.Clamp(config.CameraNoticeCooldown, 1, 60);
                    config.CameraActivationRange = Mathf.Clamp(config.CameraActivationRange, 1, 50);
                    config.CameraNoticeSpeed = Mathf.Clamp(config.CameraNoticeSpeed, 1, 10);
                }
                catch (Exception ex)
                {
                    config = new MassSurveillanceConfig();
                    MelonLogger.Warning("Failed to read NACops Mass Surveillance config: " + ex);
                }
            }
            else
            {
                MelonLogger.Warning("Missing NACops Mass Surveillance config, creating directory and template.");
                config = new MassSurveillanceConfig();
                Save(config);
            }
            return config;
        }

        public static void Save(MassSurveillanceConfig config)
        {
            try
            {
                string filePath = GetPathTo(pathSurveillanceConfig);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, json);
                MelonLogger.Warning($"NACops Mass Surveillance config, written to: {filePath}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops Mass Surveillance config: " + ex);
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
            public bool CheckpointsEnabled = true;
            public bool NoOpenCarryWeapons = true;
            public bool PrivateInvestigator = true;
            public bool WeedInvestigator = true;
            public bool CorruptCops = true;
            public bool SnitchingSamples = true;
            public bool BuyBusts = true;
            public bool MassSurveillance = true;
            public bool NearbyCrazyCops = true;
            public bool LethalCops = false;
            public bool RacistCops = false;
        }

        [Serializable]
        public class NAOfficerConfig
        {
            public int ModAddedOfficersCount = 8;
            public bool CanEnterBuildings = true; // default false
            public bool ShowNoticeIcons = true; // default true

            public bool OverrideArresting = true;
            public float ArrestTime = 1.25f; // def 1.75
            public float ArrestRange = 3.50f; //def 2.75

            public bool OverrideMovement = true;
            public float MovementSpeedMultiplier = 1.45f; // default 1

            public bool OverrideWeapon = true;
            public string RangedWeapon = "m1911"; // default -> supported m1911, goldenm1911, shotgun, revolver
            public float WeaponDamage = 46f; // default 35
            public float WeaponAimTimeMax = 1.0f; // default 1.5f
            public float WeaponAimTimeMin = 0.5f; // default 0.5
            public int WeaponMagSize = 20; // default 7
            public float WeaponFireRate = 0.33f; // default 1.5
            public float WeaponMaxRange = 25f; // default 20
            public float WeaponReloadTime = 0.5f; // default 3
            public float WeaponRaiseTime = 0.2f; // default 1.5
            public float WeaponHitChanceMax = 0.3f; // default 0.1
            public float WeaponHitChanceMin = 0.8f; // default 0.8

            public bool OverrideTaser = true;
            public float TaserDamage = 5f; // default 0
            public float TaserAimTimeMax = 1.0f; // default 1.5f
            public float TaserAimTimeMin = 0.5f; // default 0.5
            public float TaserFireRate = 3f; // default 5
            public float TaserMaxRange = 15f; // default 10
            public float TaserReloadTime = 1f; // default 2
            public float TaserRaiseTime = 0.7f; // default 1
            public float TaserHitChanceMax = 0.3f; // default 0.1
            public float TaserHitChanceMin = 0.8f; // default 0.8

            public bool OverrideMaxHealth = true;
            public float OfficerMaxHealth = 175f; // default 100

            public bool OverrideBodySearch = true;
            public float BodySearchDuration = 6f; // default 5
            public float BodySearchChance = 1f; // default 0.4

            // Overrides pursuit beh which is the main combat beh
            public bool OverrideCombatBeh = true;
            public float CombatGiveUpRange = 9999f; // default 9999 (e.g. infinite)
            public float CombatSearchTime = 9999f; // default 9999 (e.g. infinite)
            public float CombatMoveSpeed = 1.3f; // default 0.6f
            public int CombatEndAfterHits = 0; // default 0 (e.g. infinite)

            public bool OverrideVision = true;
            public float VisionRangeMultiplier = 2.0f; // default 1.8 (can be 1-4) (at default 1.8 effective range = 45, at 2.0 = 50)
            // How quickly officer notices certain crime types all remain default but changeable
            public Dictionary<string, float> VisionSpeed = new()
            {
                {"Suspicious", 0.3f },
                {"DisobeyingCurfew", 0.3f },
                {"Vandalizing", 0.3f },
                {"PettyCrime", 0.2f },
                {"DrugDealing", 0.4f },
                {"Wanted", 0.1f },
                {"Pickpocketing", 0.3f },
                {"DischargingWeapon", 0.1f },
                {"Brandishing", 0.1f },
            };

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
            public float RaiderMaxHealth = 240f;
            public float RaiderWeaponDmg = 65f;
        }

        [Serializable]
        public class MassSurveillanceConfig
        {
            public bool UseUnidirectionalCameras = true;
            public bool UseOmnidirectionalCameras = true;
            public bool SurveilCrimeStatus = true;
            public bool SurveilBaseCrimes = true;
            public int ActiveCamerasPerDay = 5;
            public int CameraActivationRange = 20;
            public int CameraNoticeSpeed = 2;
            public int CameraNoticeCooldown = 30;
            public bool PayFinesFromBank = true;
            public bool GrowPaymentsWithProgression = true; // When true scale with mod settings, when false use the below mult
            public int CrimePaymentMultiplier = 1;
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
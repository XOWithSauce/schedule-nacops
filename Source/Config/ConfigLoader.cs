
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;
#if MONO
using ScheduleOne.GameTime;
#else
using Il2CppScheduleOne.GameTime;
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
        private static string patrolsConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "patrols.json");
        private static string sentrysConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "sentrys.json");

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
                    MelonLogger.Warning("Failed to read NACops config: " + ex);
                }
            }
            else
            {
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
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops config: " + ex);
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
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops config: " + ex);
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
            }
            return config;
        }

        // Todo save default from the game and serialize to allow changing? Contract resolver already pasted to support
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
                            MelonLogger.Warning($"SentryConfig '{ser.name}' has invalid start time");
                            ser.startTime = 1900;
                        }
                        if (!TimeManager.IsValid24HourTime(ser.endTime.ToString()))
                        {
                            MelonLogger.Warning($"SentryConfig '{ser.name}' has invalid end time");
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
                                    MelonLogger.Warning($"SentryConfig '{ser.name}' has invalid weekday: '{ser.days[i]}'");
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
            }
            return config;
        }
        // Todo save default from the game and serialize to allow changing? Contract resolver already pasted to support
        // this could use like a debug builder to make it easy to add / remove from configs
        // custom ui lists patrols and sentrys separately
        // options: add, remove, cancel, save
        // add -> takes to new ui where text fields prompt the base values
        // --> add UI has confirm / cancel
        // --> add UI has enable buttons for weekdays horiz align
        // ----> for foot patrol it would ask to walk the path and auto gen waypoints based on distance to prev point while player walks, generate primitive objects no collider transparent to show path
        // ----> for sentry it would ask to stand at sentry position with desired rotation then confirm with enter? and do it twice, generate cones no collider transparent to show positions

        #endregion

        [Serializable]
        public class ModConfig
        {
            public bool ExtraOfficerPatrols = true;
            public bool ExtraOfficerSentries = true;
            public bool LethalCops = true;
            public bool NearbyCrazyCops = true;
            public bool CrazyCops = true;
            public bool PrivateInvestigator = true;
            public bool WeedInvestigator = true;
            public bool CorruptCops = true;
            public bool SnitchingSamples = true;
            public bool BuyBusts = true;
        }

        [Serializable]
        public class NAOfficerConfig
        {
            public int ModAddedOfficersCount = 8;
            public bool OverrideMovement = true;
            public bool OverrideCombatBeh = true;
            public bool OverrideBodySearch = true;
            public bool OverrideWeapon = true;
            public bool OverrideMaxHealth = true;
            public float MovementRunSpeed = 6.8f;
            public float MovementWalkSpeed = 2.4f;
            public float CombatGiveUpRange = 40f;
            public float CombatGiveUpTime = 60f;
            public float CombatSearchTime = 60f;
            public float CombatMoveSpeed = 6.8f;
            public int CombatEndAfterHits = 40;
            public float OfficerMaxHealth = 175f;
            public int WeaponMagSize = 20;
            public float WeaponFireRate = 0.33f;
            public float WeaponMaxRange = 25f;
            public float WeaponReloadTime = 0.5f;
            public float WeaponRaiseTime = 0.2f;
            public float WeaponHitChanceMax = 0.3f;
            public float WeaponHitChanceMin = 0.8f;
        }

        [Serializable]
        public class FootPatrolsSerialized
        {
            public List<SerializedFootPatrol> loadedPatrols = new();
        }

        [Serializable]
        public class SentrysSerialized
        {
            public List<SerializedSentry> loadedSentrys = new();
        }

    }
}
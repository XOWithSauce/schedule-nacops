

using MelonLoader.Utils;

namespace NACops
{
    /// <summary>
    /// Consolidates all the paths that mod might use in different installations
    /// </summary>
    public static class ModDataPaths
    {

        // Folder where all userdata is
        private readonly static string BASE_USERDATA_NAME = "XO_WithSauce-NACops";

        // For when user uses thunderstore mod manager it creates the following name for the folder where it puts the folders, depends on backend version
        private readonly static string TS_PACKAGE_NAME = "XO_WithSauce-NACops_";
#if MONO
        private readonly static string packagePathUserData = Path.Combine(MelonEnvironment.UserDataDirectory, TS_PACKAGE_NAME + "MONO", BASE_USERDATA_NAME);
#else
        private readonly static string packagePathUserData = Path.Combine(MelonEnvironment.UserDataDirectory, TS_PACKAGE_NAME + "IL2CPP", BASE_USERDATA_NAME);
#endif
        // For when user drags and drops the UserData folder from manual download, this is fallback checked path for the mod userdata folder
        private readonly static string manualPathUserData = Path.Combine(MelonEnvironment.UserDataDirectory, BASE_USERDATA_NAME);

        // Then for each config file or persistent data file it depends on the subfolder level
        public static readonly string pathModConfig = "config.json";
        public static readonly string pathOfficerConfig = "officer.json";
        public static readonly string pathRaidConfig = "raid.json";
        public static readonly string pathEventFrequencyConfig = "progression.json";
        public static readonly string pathSurveillanceConfig = "surveillance.json";

        public static readonly string pathPatrolsConfig = Path.Combine("Spawn", "patrols.json");
        public static readonly string pathVehiclePatrolsConfig = Path.Combine("Spawn", "vehiclepatrols.json");
        public static readonly string pathSentrysConfig = Path.Combine("Spawn", "sentrys.json");
        
        public static readonly string pathPropertyHeatConfig = "HeatData"; // Directory, Filename dynamic {saveslot num}_{organization}.json

        private static bool hasCheckedInstallationPath = false;
        private static bool isModManagerInstallation = false;

        // one helper function to merge mod paths with installation
        public static string GetPathTo(string modDataDestination)
        {
            if (!hasCheckedInstallationPath)
            {
                if (Directory.Exists(packagePathUserData))
                {
                    //MelonLogger.Msg("Installation is a mod manager installation");
                    isModManagerInstallation = true;
                }

                if (Directory.Exists(manualPathUserData))
                {
                    //MelonLogger.Msg("Installation is a manual installation");
                    isModManagerInstallation = false;
                }

                hasCheckedInstallationPath = true;
            }
            string userDataPath = isModManagerInstallation ? packagePathUserData : manualPathUserData;
            return Path.Combine(userDataPath, modDataDestination);
        }


    }
}
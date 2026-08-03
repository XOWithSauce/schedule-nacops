using MelonLoader;
using System.Reflection;

using static NACops.DebugModule;
using static NACops.NACops;

namespace NACops
{
    public class ModPrefsHandler
    {
        public MelonPreferences_Category modConfigCategory;

        public void SetupMelonPreferences()
        {
            string categoryIdentifier = $"{BuildInfo.Name} {BuildInfo.Author}";
            modConfigCategory = MelonPreferences.CreateCategory(identifier: categoryIdentifier, display_name: BuildInfo.Name);
            modConfigCategory.CreateEntry(
                "DebugMode", currentConfig.DebugMode,
                display_name: "Debug Mode Enabled",
                description: "Enable debug mode to test features"
            );

            modConfigCategory.CreateEntry(
                "RaidsEnabled", currentConfig.RaidsEnabled,
                display_name: "Raids Enabled",
                description: "Enable raid events"
            );

            modConfigCategory.CreateEntry(
                "ExtraOfficerPatrols", currentConfig.ExtraOfficerPatrols,
                display_name: "Extra officer foot patrols",
                description: "Adds new officer foot patrols from 'Spawn/patrols.json' file"
            );

            modConfigCategory.CreateEntry(
                "ExtraVehiclePatrols", currentConfig.ExtraVehiclePatrols,
                display_name: "Extra officer vehicle patrols",
                description: "Adds new officer vehicle patrols from 'Spawn/vehiclepatrols.json' file"
            );

            modConfigCategory.CreateEntry(
                "ExtraOfficerSentries", currentConfig.ExtraOfficerSentries,
                display_name: "Extra officer sentries",
                description: "Adds new officer stationary sentries from 'Spawn/sentries.json' file"
            );

            modConfigCategory.CreateEntry(
                "CheckpointsEnabled", currentConfig.CheckpointsEnabled,
                display_name: "Checkpoints Enabled",
                description: "Enable the usage of road block checkpoints"
            );

            modConfigCategory.CreateEntry(
                "NoOpenCarryWeapons", currentConfig.NoOpenCarryWeapons,
                display_name: "No open carry weapons",
                description: "Makes holding weapons in hand and in inventory illegal"
            );

            modConfigCategory.CreateEntry(
                "PrivateInvestigator", currentConfig.PrivateInvestigator,
                display_name: "Private investigator",
                description: "Enable the private investigator who spies on the player and gathers evidence for property heat system"
            );

            modConfigCategory.CreateEntry(
                "WeedInvestigator", currentConfig.WeedInvestigator,
                display_name: "Weed investigator",
                description: "Enable a feature where using drugs will cause nearby cops to search for the player"
            );

            modConfigCategory.CreateEntry(
                "CorruptCops", currentConfig.CorruptCops,
                display_name: "Corrupt cops",
                description: "Enable a feature where cops give false charges that cause the players arrest to be more expensive"
            );

            modConfigCategory.CreateEntry(
                "SnitchingSamples", currentConfig.SnitchingSamples,
                display_name: "Snitching samples",
                description: "Enable a feature where giving free samples can result in Investigation crime status"
            );

            modConfigCategory.CreateEntry(
                "BuyBusts", currentConfig.BuyBusts,
                display_name: "Buy busts",
                description: "Enable a feature where after completing a deal an officer can spawn behind the player and attempt to arrest"
            );

            modConfigCategory.CreateEntry(
                "MassSurveillance", currentConfig.MassSurveillance,
                display_name: "Mass Surveillance",
                description: "Enable the usage of Cameras across Hyland Point to monitor the player and report any crimes."
            );

            modConfigCategory.CreateEntry(
                "NearbyCrazyCops", currentConfig.NearbyCrazyCops,
                display_name: "Nearby crazy cops",
                description: "Enable a feature where cops will randomly find the player nearby and body search"
            );

            modConfigCategory.CreateEntry(
                "LethalCops", currentConfig.LethalCops,
                display_name: "Lethal cops",
                description: "Enable a feature where cops will randomly start lethally hunting the player when nearby"
            );

            modConfigCategory.CreateEntry(
                "RacistCops", currentConfig.RacistCops,
                display_name: "Racist cops",
                description: "Enable a feature where cops will hunt down black skin coloured players on sight"
            );

            for (int i = 0; i < modConfigCategory.Entries.Count; i++)
            {
                string id = modConfigCategory.Entries[i].Identifier;

                void ThisEntryChanged(object objOld, object objNew)
                {
                    //Log("Entry change wrapper");
                    OnEntryChange(id, objOld, objNew);
                }
                modConfigCategory.Entries[i].OnEntryValueChangedUntyped.Subscribe(ThisEntryChanged);
            }

            MelonPreferences.SaveCategory<MelonPreferences_Category>(categoryIdentifier, printmsg: false);
            Log("Melon preferences created");
        }

        public static void OnEntryChange(string identifier, object objOld, object objNew)
        {
            // instead of sync config this can just auto apply on changed
            FieldInfo[] modConfigFields = currentConfig.GetType().GetFields();
            foreach (FieldInfo field in modConfigFields)
            {
                if (!field.Name.Contains(identifier)) continue;
                field.SetValue(currentConfig, (bool)objNew);
            }
            // Instantly reflect the melon pref change in .json
            ConfigLoader.Save(currentConfig, logConfirm: false);
        }
    }
}
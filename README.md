# SCHEDULE I NACOPS MOD

## Features

- Adds Property Raids and Property Heat system
- Cops will occasionally appoint a disguised Private Investigator to monitor you
- Adds configuration support for officers health, damage and movement
- Adds new Foot Patrols & Officer Sentries & Vehicle Patrols and allows for configuration of these activities
- Cops will now try to search for players smoking illegal product and also apprehend the suspect
- New customers will try to snitch on you resulting in Car Dispatches and Investigation
- When dealing to unlocked customers, they have a chance to be a part of a Buy Bust!
- Cops will give you more crime charges if arrested when the Corrupt Cops is enabled
- Cops difficulty can be set to extreme with Lethal Cops or impossible with Racist Cops
- Overall Cops events frequency is tied to Game Progression:
	- Networth
  - Total Days in the Save
  - Customer Relationships

## Important!

- **"alternate" or "alternate-beta" users**: Download the `NACopsV1-MONO` version.
- **"default" or "beta" users**: Download the `NACopsV1-IL2CPP` version.

## Installation Steps

1. Install Melon Loader from a trusted source like [MelonWiki](https://melonwiki.xyz/).
2. Copy the DLL file and the `NACops` into the `Mods` folder.
3. You are good to go!

## Configuration

### (Optional) Mod Configuration Steps:

1. Open the `NACops` folder and locate the file called `config.json`.
2. The default contents of the `config.json` file are as follows:
   
```json
{
  "DebugMode": false,
  "RaidsEnabled": true,
  "ExtraOfficerPatrols": true,
  "ExtraVehiclePatrols": true,
  "ExtraOfficerSentries": true,
  "NoOpenCarryWeapons": true,
  "PrivateInvestigator": true,
  "WeedInvestigator": true,
  "CorruptCops": true,
  "SnitchingSamples": true,
  "BuyBusts": true,
  "NearbyCrazyCops": true,
  "LethalCops": false,
  "RacistCops": false
}
```
- DebugMode:
  - true: Makes Buy busts and Snitching Samples always trigger regardless of chance
  - false: Disabled

- RaidsEnabled: true
  - true: Use the raid.json file to run raids and save property heat data
  - false: Disabled

- ExtraOfficerPatrols:
  - true: Use the Spawn/patrols.json file to create more Foot Patrols
  - false: Disabled

- ExtraVehiclePatrols:
  - true: Use the Spawn/vehiclepatrols.json file to create more Vehicle Patrols
  - false: Disabled

- ExtraOfficerSentries:
  - true: Use the Spawn/sentrys.json file to create more Officer Sentries
  - false: Disabled

- NoOpenCarryWeapons:
  - true: Makes holding weapons or having them in inventory illegal
  - false: Disabled

- PrivateInvestigator:
	- true: Spawns a disguised officer to monitor you and build up property heat
	- false: Disabled

- WeedInvestigator:
	- true: Forces nearby cops to find you and body search when consuming any drug nearby
	- false: Disabled

- CorruptCops:
	- true: Cops will give you false charges when the events run
	- false: Disabled

- SnitchingSamples:
	- true: When you give Potential Customers samples they have a chance to Snitch on you -> Vehicle patrol + Investigation status
	- false: Disabled

- BuyBusts:
	- true: When you deal customers product, based on the customer relationship this might trigger a Buy Bust, spawning a Cop behind you!
	- false: Disabled

- NearbyCrazyCops:
	- true: Forces nearby cops to actively find you and initiate body search
	- false: Disabled

- LethalCops:
	- true: Forces a nearby cop to actively target you and lethally hunt you 
	- false: Disabled

- RacistCops:
  - true: While playing as a black character, forces all nearby officers within 80 units to hunt you lethally on sight
  - false: Disabled


### (Optional) Officer Configuration Steps:

1. Open the `NACops` folder and locate the file called `officer.json`.
2. The default contents of the `officer.json` file are as follows:

```json
{
  "ModAddedVehicleCount": 3,
  "ModAddedOfficersCount": 8,
  "CanEnterBuildings": true,
  "OverrideArresting": true,
  "ArrestTime": 1.25,
  "ArrestRange": 3.50,
  "OverrideMovement": true,
  "MovementSpeedMultiplier": 1.65,
  "OverrideWeapon": true,
  "WeaponMagSize": 20,
  "WeaponFireRate": 0.33,
  "WeaponMaxRange": 25.0,
  "WeaponReloadTime": 0.5,
  "WeaponRaiseTime": 0.2,
  "WeaponHitChanceMax": 0.3,
  "WeaponHitChanceMin": 0.8,
  "OverrideMaxHealth": true,
  "OfficerMaxHealth": 175.0,
  "OverrideBodySearch": true,
  "BodySearchDuration": 6.0,
  "BodySearchChance": 1.0,
  "OverrideCombatBeh": true,
  "CombatGiveUpRange": 40.0,
  "CombatGiveUpTime": 60.0,
  "CombatSearchTime": 60.0,
  "CombatMoveSpeed": 6.8,
  "CombatEndAfterHits": 40
}
```
- ModAddedVehicleCount:
  - Increases the max cap of spawnable cars from Police Station

- ModAddedOfficersCount:
  - How many additional officers to spawn into the world.
  - Range: 0 - 20

- CanEnterBuildings:
  - true: Officers can enter players properties. Player can enter their own properties while wanted.
  - false: Disabled

- OverrideArresting:
  - true: Use the `ArrestTime` and `ArrestRange` values to override arresting behaviour
  - false: Uses the game default settings on the officers or other mods settings.

- OverrideMovement:
	- true: Apply the `MovementSpeedMultiplier`
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideWeapon:
	- true: Apply the `WeaponMagSize`, `WeaponFireRate`, `WeaponMaxRange`, `WeaponReloadTime`, `WeaponRaiseTime`, `WeaponHitChanceMax` and `WeaponHitChanceMin` to the default M1911 gun
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideMaxHealth:
	- true: Apply the `OfficerMaxHealth`
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideBodySearch:
	- true: Apply the `BodySearchDuration` and `BodySearchChance` values to Body Searching behaviour
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideCombatBeh:
	- true: Apply the `CombatGiveUpRange`, `CombatGiveUpTime`, `CombatSearchTime`, `CombatMoveSpeed` and `CombatEndAfterHits`
	- false: Uses the game default settings on the officers or other mods settings.


### (Optional) Foot Patrol Configuration Steps:

1. Open the `NACops` folder and then `Spawn` folder and locate the file called `patrols.json`.
2. The `patrols.json` file contains multiple preset patrols. This file can be modified by removing, changing values or adding new patrols templates.
3. The ModAddedOfficerCount in officers.json file should be increased when:
  - Adding new templates
  - Increasing officer counts 
  - Activity length or weekdays

4. Each added patrol contains following example config values:

```json
{
    "startTime": 730,
    "endTime": 1050,
    "members": 2,
    "intensityRequirement": 0,
    "onlyIfCurfew": false,
    "name": "Northtown Canal Loop",
    "days": [ "tue", "thu", "sat" ],
    "waypoints": [
        {
            "x": -71.256,
            "y": 0.0,
            "z": 41.394
        },
        ...
        {
          "x": -67.629,
          "y": 0.0,
          "z": 41.367
        }
    ]
},
```
- startTime:
  - When the activity should start
  - Example: 07:30 is `730`

- endTime:
  - When the activity should end
  - Example: 10:50 is `1050`

- members:
  - How many officers partake in the activity
  - Range 1-4

- intensityRequirement
  - Law intensity required for the activity to be enabled
  - Range (min) 0-10 (max)

- onlyIfCurfew:
  - true: Only run the activity during curfew
  - false: Run the activity before and during curfew

- name:
  - Custom name for the activity

- days:
  - Contains a list of abbreviated weekdays which indicate what weekdays use this activity
  - Supported Values: `"mon"`, `"tue"`, `"wed"`, `"thu"`, `"fri"`, `"sat"`, `"sun"` 

- waypoints:
  - Contains a list of waypoints which the officers traverse during patrol
  - Each waypoint needs X and Z values. The Y value can be `0.0`

### (Optional) Vehicle Patrol Configuration Steps:

1. Open the `NACops` folder and then `Spawn` folder and locate the file called `vehiclepatrols.json`.
2. The `vehiclepatrols.json` file contains multiple preset patrols. This file can be modified by removing, changing values or adding new patrols templates.
3. The ModAddedOfficerCount in officers.json file should be increased when:
  - Adding new templates
  - Increasing officer counts 
  - Activity length or weekdays

4. Each added patrol contains following example config values:

```json
{
    "startTime": 730,
    "intensityRequirement": 0,
    "onlyIfCurfew": false,
    "name": "Full Map Loop",
    "days": [ "tue", "thu", "sat" ],
    "waypoints": [
        {
            "x": 17.76604,
            "y": 1.36655593,
            "z": 50.36123
        },
        ...
        {
          "x": 64.61583,
          "y": 1.37344146,
          "z": 52.04244
        }
    ]
},
```
- startTime:
  - When the activity should start
  - Example: 07:30 is `730`

- intensityRequirement
  - Law intensity required for the activity to be enabled
  - Range (min) 0-10 (max)

- onlyIfCurfew:
  - true: Only run the activity during curfew
  - false: Run the activity before and during curfew

- name:
  - Custom name for the activity

- days:
  - Contains a list of abbreviated weekdays which indicate what weekdays use this activity
  - Supported Values: `"mon"`, `"tue"`, `"wed"`, `"thu"`, `"fri"`, `"sat"`, `"sun"` 

- waypoints:
  - Contains a list of waypoints which the officers traverse during patrol
  - Each waypoint needs X and Z values. The Y value can be `0.0`

### (Optional) Officer Sentry Configuration Steps:

1. Open the `NACops` folder and locate the file called `sentrys.json`.
2. The `sentrys.json` file contains multiple preset sentry positions. This file can be modified by removing, changing values or adding new patrols templates.
3. The ModAddedOfficerCount in officers.json file should be increased when:
  - Adding new templates
  - Increasing officer counts 
  - Activity length or weekdays

4. Each added patrol contains following example config values:

```json
{
    "startTime": 2230,
    "endTime": 330,
    "members": 1,
    "intensityRequirement": 0,
    "onlyIfCurfew": false,
    "name": "Northtown Pharmacy Sentry",
    "days": [ "thu", "fri", "sat", "sun" ],
    "standPosition1": {
        "x": -55.705,
        "y": 0.0,
        "z": 122.622
    },
    "pos1Rotation": {
        "x": 0.0,
        "y": 297.0,
        "z": 0.0
    },
    "standPosition2": {
        "x": -64.092,
        "y": 0.0,
        "z": 119.330
    },
    "pos2Rotation": {
        "x": 0.0,
        "y": 15.0,
        "z": 0.0
    }
},
```
- startTime:
  - When the activity should start
  - Example: 07:30 is `730`

- endTime:
  - When the activity should end
  - Example: 10:50 is `1050`

- members:
  - How many officers partake in the activity
  - Range 1-4

- intensityRequirement
  - Law intensity required for the activity to be enabled
  - Range (min) 0-10 (max)

- onlyIfCurfew:
  - true: Only run the activity during curfew
  - false: Run the activity before and during curfew

- name:
  - Custom name for the activity

- days:
  - Contains a list of abbreviated weekdays which indicate what weekdays use this activity
  - Supported Values: `"mon"`, `"tue"`, `"wed"`, `"thu"`, `"fri"`, `"sat"`, `"sun"` 

- standPosition (1 and 2):
  - Requires stand positions with X and Z values

- posRotation (1 and 2):
  - Requires officer rotation Y value

---

> **Note**: The configuration files and directory structure described in this document will be created automatically in the `Mods/NACops/` directory if they are missing.

---

Contribute, Build from Source or Verify Integrity -> [GitHub](https://github.com/XOWithSauce/schedule-nacops/)
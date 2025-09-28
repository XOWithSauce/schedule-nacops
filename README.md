# SCHEDULE I NA COPS MOD

**NEEDS MELON LOADER (BOTH ALTERNATE AND DEFAULT VERSIONS ARE NOW SUPPORTED!)**

## Features

- Adds new Foot Patrols & Officer Sentries and allows for configuration of these activities
- Makes the cops more lethal by making them arrest you easily and conducting searches periodically
- Cops now occasionally use lethal force if you approach them
- Cops will occasionally appoint a disguised Private Investigator to monitor you
- Cops will now try to search for players smoking illegal product and also apprehend the suspect
- Cops will give you more crime charges if arrested
- New customers will try to snitch on you resulting in Car Dispatches and Investigation
- When dealing to customers, they have a chance to be a part of a Buy Bust!
- Overall Cops difficulty is tied to Game Progression:
	- Total Earnings
  - Total Days in the Save
  - Customer Relationships

## Important!

- **"alternate" or "alternate-beta" branch users**: Download the `NACopsV1-MONO` version.
- **"default" or "beta" branch users**: Download the `NACopsV1-IL2CPP` version.

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
    "ExtraOfficerPatrols": true,
    "ExtraOfficerSentries": true,
    "LethalCops": true,
    "NearbyCrazyCops": true,
    "CrazyCops": true,
    "PrivateInvestigator": true,
    "WeedInvestigator": true,
    "CorruptCops": true,
    "SnitchingSamples": true,
    "BuyBusts": true,
}
```
- ExtraOfficerPatrols:
  - true: Use the patrols.json file to create more Foot Patrols
  - false: Disabled

- ExtraOfficerSentries:
  - true: Use the sentrys.json file to create more Officer Sentries
  - false: Disabled

- LethalCops:
	- true: Forces nearby cops to actively target you and lethally hunt you
	- false: Disabled

- NearbyCrazyCops:
	- true: Forces nearby cops to actively find you and initiate body search
	- false: Disabled

- CrazyCops:
	- true: Forces cops to try and initiate: Vehicle pursuits, Foot pursuits if visible or Initiate Investigations
	- false: Disabled

- PrivateInvestigator:
	- true: Forces a nearby cop to transform into a Private Investigator that follows you
	- false: Disabled

- WeedInvestigator:
	- true: Forces nearby cops to find you and body search when smoking product nearby
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

### (Optional) Officer Configuration Steps:

1. Open the `NACops` folder and locate the file called `officer.json`.
2. The default contents of the `officer.json` file are as follows:

```json
{
    "ModAddedOfficersCount": 8,
    "OverrideMovement": true,
    "OverrideCombatBeh": true,
    "OverrideBodySearch": true,
    "OverrideWeapon": true,
    "OverrideMaxHealth": true,
    "MovementRunSpeed": 6.8,
    "MovementWalkSpeed": 2.4,
    "CombatGiveUpRange": 40.0,
    "CombatGiveUpTime": 60.0,
    "CombatSearchTime": 60.0,
    "CombatMoveSpeed": 6.8,
    "CombatEndAfterHits": 40,
    "OfficerMaxHealth": 175.0,
    "WeaponMagSize": 20,
    "WeaponFireRate": 0.33,
    "WeaponMaxRange": 25.0,
    "WeaponReloadTime": 0.5,
    "WeaponRaiseTime": 0.2,
    "WeaponHitChanceMax": 0.3,
    "WeaponHitChanceMin": 0.8
}
```
- ModAddedOfficersCount:
  - How many additional officers to spawn into the world.
  - Range: 0 - 20

- OverrideMovement:
	- true: Apply the `MovementRunSpeed` and `MovementWalkSpeed`
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideMovement:
	- true: Apply the `MovementRunSpeed` and `MovementWalkSpeed`
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideCombatBeh:
	- true: Apply the `CombatGiveUpRange`, `CombatGiveUpTime`, `CombatSearchTime`, `CombatMoveSpeed` and `CombatEndAfterHits`
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideBodySearch:
	- true: Apply increase to the body search duration and apply random speed boosts to the officer speed during it
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideWeapon:
	- true: Apply the `WeaponMagSize`, `WeaponFireRate`, `WeaponMaxRange`, `WeaponReloadTime`, `WeaponRaiseTime`, `WeaponHitChanceMax` and `WeaponHitChanceMin` to the default M1911 gun
	- false: Uses the game default settings on the officers or other mods settings.

- OverrideMaxHealth:
	- true: Apply the `OfficerMaxHealth`
	- false: Uses the game default settings on the officers or other mods settings.

### (Optional) Foot Patrol Configuration Steps:

1. Open the `NACops` folder and locate the file called `patrols.json`.
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

> **Note**: The `config.json` and `officer.json` files will be automatically created in the `Mods/NACops/` directory if it's missing.

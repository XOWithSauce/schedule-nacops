<img src="https://i.imgur.com/eriIU8P.jpeg">

# NACops

**Requires Melon Loader**

NACops adds property raids, investigators and extends the configurability of the police in the Hyland Point.

## Table of Contents

---
* [Features](#features)

* [Installation](#installation)

* [Property Raids](#property-raids)
  * [Info](#property-raids-info)
  * [Property Raids Configuration](#property-raids-configuration)

* [Private Investigator](#private-investigator)
  * [Property Heat](#property-heat-data)

* [In-Game Console](#in-game-console-support)
  * [List Indexes](#list)
  * [Spawn patrols, sentries, investigators or raids](#spawn)
  * [Visualize routes or sentries](#visualize)
  * [Build new routes or sentries](#build)

* [Configuration](#configuration)
  * [Mod Configuration](#optional-mod-configuration-steps)
  * [Officer Configuration](#optional-officer-configuration-steps)
  * [Foot Patrol Configuration](#optional-foot-patrol-configuration-steps)
  * [Vehicle Patrol Configuration](#optional-vehicle-patrol-configuration-steps)
  * [Officer Sentry Configuration](#optional-officer-sentry-configuration-steps)
  * [Progression Difficulty Configuration](#optional-progression-difficulty-configuration)

---

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
- Overall Cops events frequency is tied to Game Progression
- Adds console commands to manually trigger police activities and configure the mod


## Installation

#### Important!

- **"alternate" or "alternate-beta" users**: Download the `NACopsV1-MONO` version.
- **"default" or "beta" users**: Download the `NACopsV1-IL2CPP` version.

1. Install Melon Loader from a trusted source like [MelonWiki](https://melonwiki.xyz/).
2. Copy the DLL file and the `NACops` into the `Mods` folder.
3. You are good to go!


## Property Raids

Property raids are configurable events where a group of raid police enter your property, steal your illegal products, destroy your growing gear and disrupt your drug producing operations. 

<img src="https://i.imgur.com/N6X8UZX.png">

### Property Raids Info

- Property raids can begin in the morning when the player wakes up
- Properties are only valid for a raid if there are enough items built inside
- If the property has high enough heat and it has not been raided recently, a group of 3 officers will spawn to partake in the raid
- When arriving at the property the raid officers will scare any hired employees


Raid officers can have 3 different roles based on what the property has built inside:

  1. **Destroy Growing Gear**: Take out any pots, drying racks or mushroom beds in the property
  2. **Destroy Lab Gear**: Take out any advanced mixing stations, lab ovens, chemistry stations or cauldrons
  3. **Search Containers**: Search the containers in the property for illegal products! Only safes and beds can be used to hide items from these officers.

- By default each raid officer can destroy only 4 built items or search 4 containers. 

- After the raid officer is done with their task, they leave the property and despawn.

### Property Raids Configuration

Property Raids frequency, required heat and raid officers can be configured from the `raid.json` file.


1. Open the `Mods/NACops/raid.json` file
2. This file can be modified to change raid related logic and its contents by default are:

```json
{
  "TraverseToPropertySpeed": 0.47,
  "ClearPropertySpeed": 0.38,
  "MaxDestroyIters": 4,
  "RaidCopsCount": 3,
  "DaysUntilCanRaid": 8,
  "PropertyHeatThreshold": 14
}
```
- **TraverseToPropertySpeed**: 
  - How fast the raid officers move while traveling to property
  - Range: 0.1 - 1.0
- **ClearPropertySpeed**: 
  - How fast the officers move while performing their task at the property
  - Range: 0.1 - 1.0
- **MaxDestroyIters**: 
  - Maximum amount of built items destroyed OR containers cleared
  - Range: 1-10
- **RaidCopsCount**: 
  - How many raid officers get spawned to raid a property
  - Range: 1-10
- **DaysUntilCanRaid**: 
  - How many days have to be waited for a raid to begin again
  - Range: 1-20
- **PropertyHeatThreshold**: 
  - How high the property heat must be for a raid to begin
  - Range: 1-100


> Increasing the `RaidCopsCount` and `MaxDestroyIters` values will require more built items in your property to be valid for a raid.
> The default values are just low enough to allow Bungalow and Sweatshop to be raided if fully built!
---

## Private Investigator

The private investigator spawns randomly with increasing frequency based on your total Networth! They spawn with a random name, clothing and appearance.

<img src="https://i.imgur.com/L4WO3cN.png">

---
During the investigation the officer follows you around and tries to maintain a line of sight or stand nearby. While doing this they list all the properties you have entered, how many times you've been spotted and how many times you've been nearby.

At the end of their 4 (ingame) hour shift, based on their investigation the property heats increase or decrease:

- If the player spent **alot** of time inside a property and they are still inside that property and they have been spotted outside atleast once: 
  - Property heat increases by 6 to 8

- If the player spent **a bit** of time inside a property and they are still inside that property and they been spotted outside atleast once:
  - Property heat increases by 4 to 5

- If the player spent **a bit** of time inside a property, but they are not inside right now and they have been spotted outside atleast twice and the property heat is lower than 8:
  - Property heat increases by 2 to 3

- If the investigator didnt get enough evidence on a property so that none of the above have been met and if the property heat is above 5:
  - Property heat decreases by 1 to 4

---
### Property Heat Data

Any property with property heat higher than 12 will have the heat decrease by 1 each passing day. Each time a raid occurs the property heat is reset to 0.

The Property Heat data is saved to `Mods/NACops/HeatData/(organisation).json` automatically when saving the game. 

You can modify the content of this file and change the days passed or the current property heat. The content is by default:
```json
{
  "loadedPropertyHeats": [
    {
      "propertyCode": "sweatshop",
      "propertyHeat": 0,
      "daysSinceLastRaid": 4
    },
    {
      "propertyCode": "bungalow",
      "propertyHeat": 0,
      "daysSinceLastRaid": 4
    },
    {
      "propertyCode": "storageunit",
      "propertyHeat": 0,
      "daysSinceLastRaid": 4
    },
    {
      "propertyCode": "dockswarehouse",
      "propertyHeat": 0,
      "daysSinceLastRaid": 4
    },
    {
      "propertyCode": "barn",
      "propertyHeat": 0,
      "daysSinceLastRaid": 4
    },
    {
      "propertyCode": "manor",
      "propertyHeat": 0,
      "daysSinceLastRaid": 4
    }
  ]
}
```

- **propertyCode**: Exact property code game uses to identify the property.

- **propertyHeat**: Accumulated property heat. Must be higher than `raid.json`->`PropertyHeatThreshold` for a raid to begin.

- **daysSinceLastRaid**: Days passed since the last raid. Starts at 4 by default. Any owned properties will increase this by 1 each time player wakes up. Must be higher than `raid.json`->`DaysUntilCanRaid` for a raid to begin.



## In-Game Console Support

The NACops mod supports using the in-game console to run events, list all of the added sentries and patrols, visualizing positions of sentries or routes of foot / vehicle patrols and building your own vehicle patrol routes, foot patrol routes or new sentries.

Use: `nacops help` to see all the supported commands in Melon Loader Console

Use: `nacops enable logs` to enable all debug logging

---
---
### List

You need to specify an index when spawning or visualizing. The List command allows you to easily see and list all of the indexes.

Example usage: `nacops list footpatrol`
> Print into the console a list with "Index: Name" structure of all the mod added foot patrols

Example usage: `nacops list raid`
> Print into the console a list with "Index: Property Code, Days Since Last Raid, Property Heat"

---
---
### Spawn

You need to specify an index when spawning a raid, foot patrol, vehicle patrol or a sentry. The private investigator does not need a spawn index.

Example usage: `nacops spawn footpatrol 0`
> Spawn the first index (0) of the mod added foot patrols instantly.

Example usage: `nacops spawn sentry 2`
> Spawn the third index (2) of the mod added sentries instantly.

Example usage: `nacops spawn raid 1`
> Start a raid instantly at the second index (1) (by default Bungalow in HeatData)

Example usage: `nacops spawn investigator`
> Spawn an investigator instantly. No index needed.

---
---
### Visualize

<img src="https://i.imgur.com/qPdzbie.png">
You need to specify an index when visualizing a foot patrol, vehicle patrol or sentry. Only these can be visualized.

---
Example usage: `nacops visualize footpatrol 0`
> Visualize the first index (0) of the mod added foot patrols route.

Example usage: `nacops visualize sentry 2`
> Visualize the third index (2) of the mod added sentries standing positions.

Example usage: `nacops visualize vehiclepatrol 1`
> Visualize the second index (1) of the mod added vehicle patrols route.
---
---
### Build

You can build new foot patrol routes, vehicle patrol routes or sentries easily with a few commands. These get automatically saved to `Mods/NACops/Spawn/` with a random identifier. You can find your own generated builds by scrolling to the end of the file. The random identifier is printed into the Melon Loader Console when saving.

> To apply any new builds you must go back to main menu and reload the save or exit the game and re-launch.
---
#### Build a new foot patrol route:

Example usage: `nacops build footpatrol start` -> Walk -> `nacops build footpatrol stop`
> Start drawing a path for a new foot patrol from your current position. Walk around to make the path. Then use the stop command to stop it. This will automatically save the path to the end of patrols.json file.
---
#### Build a new foot patrol route:

Example usage: `nacops build vehiclepatrol start` -> Walk -> `nacops build vehiclepatrol stop`
> Remember to build path only on a road. Start drawing a path for a new vehicle patrol from your current position. Walk on the road to make the path. Then use the stop command to stop it. This will automatically save the path to the end of vehiclepatrols.json file.

---
#### Build a new sentry position:

Example usage: 1st position: `nacops build sentry start` -> 2nd position: `nacops build sentry stop`
> Each sentry needs 2 positions. Walk into the first position then use the start command. Walk to another nearby position and then use the stop command. This sets the positions and automatically saves the sentry to the end of sentrys.json file.

---
---
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
  - true: Use the `raid.json` file to run raids and save property heat data
  - false: Disabled

- ExtraOfficerPatrols:
  - true: Use the `Spawn/patrols.json` file to create more Foot Patrols
  - false: Disabled

- ExtraVehiclePatrols:
  - true: Use the `Spawn/vehiclepatrols.json` file to create more Vehicle Patrols
  - false: Disabled

- ExtraOfficerSentries:
  - true: Use the `Spawn/sentrys.json` file to create more Officer Sentries
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
	- true: Forces nearby cops to actively find you and initiate body search. If inside a vehicle this can trigger a vehicle pursuit.
	- false: Disabled

- LethalCops:
	- true: Forces a nearby cop to actively target you and lethally hunt you 
	- false: Disabled

- RacistCops:
  - true: While playing as a black character, forces all nearby officers within 80 units to hunt you lethally on sight
  - false: Disabled

---
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

---
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
---
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

---

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

### (Optional) Progression difficulty configuration:

1. Open the `NACops` folder and locate the file called `progression.json`.
2. The `progression.json` file contains progression related thresholds and their respective random ranges. Each one is explained after the json file representation.
3. You can modify these or add new ones based on your liking. The default configuration increases the difficulty and frequency of mod added events with progression.

```json
{
  "LethalCopFrequency": [
    { "MinOf": 0, "Min": 30.0, "Max": 60.0 },
    { "MinOf": 5, "Min": 20.0, "Max": 60.0 },
    { "MinOf": 10, "Min": 20.0, "Max": 50.0 },
    { "MinOf": 20, "Min": 15.0, "Max": 40.0 },
    { "MinOf": 30, "Min": 10.0, "Max": 30.0 },
    { "MinOf": 40, "Min": 10.0, "Max": 20.0 },
    { "MinOf": 50, "Min": 8.0, "Max": 18.0 }
  ],
  "LethalCopRange": [
    { "MinOf": 0, "Min": 1.0, "Max": 3.0 },
    { "MinOf": 8000, "Min": 1.0, "Max": 5.0 },
    { "MinOf": 30000, "Min": 2.0, "Max": 6.0 },
    { "MinOf": 100000, "Min": 3.0, "Max": 8.0 },
    { "MinOf": 300000, "Min": 4.0, "Max": 10.0 },
    { "MinOf": 600000, "Min": 9.0, "Max": 14.0 },
    { "MinOf": 1000000, "Min": 10.0, "Max": 15.0 }
  ],
  "NearbyCrazyFrequency": [
    { "MinOf": 0, "Min": 120.0, "Max": 400.0 },
    { "MinOf": 5, "Min": 120.0, "Max": 350.0 },
    { "MinOf": 10, "Min": 100.0, "Max": 200.0 },
    { "MinOf": 20, "Min": 80.0, "Max": 100.0 },
    { "MinOf": 30, "Min": 60.0, "Max": 100.0 },
    { "MinOf": 40, "Min": 50.0, "Max": 80.0 },
    { "MinOf": 50, "Min": 30.0, "Max": 80.0 }
  ],
  "NearbyCrazyRange": [
    { "MinOf": 0, "Min": 10.0, "Max": 20.0 },
    { "MinOf": 8000, "Min": 10.0, "Max": 25.0 },
    { "MinOf": 30000, "Min": 10.0, "Max": 30.0 },
    { "MinOf": 100000, "Min": 20.0, "Max": 35.0 },
    { "MinOf": 300000, "Min": 20.0, "Max": 40.0 },
    { "MinOf": 500000, "Min": 25.0, "Max": 40.0 }
  ],
  "PIFrequency": [
    { "MinOf": 0, "Min": 600.0, "Max": 1200.0 },
    { "MinOf": 9000, "Min": 500.0, "Max": 1000.0 },
    { "MinOf": 30000, "Min": 450.0, "Max": 800.0 },
    { "MinOf": 50000, "Min": 400.0, "Max": 800.0 },
    { "MinOf": 80000, "Min": 300.0, "Max": 600.0 },
    { "MinOf": 300000, "Min": 300.0, "Max": 550.0 },
    { "MinOf": 500000, "Min": 300.0, "Max": 550.0 },
    { "MinOf": 900000, "Min": 300.0, "Max": 500.0 },
    { "MinOf": 1500000, "Min": 300.0, "Max": 500.0 },
    { "MinOf": 8000000, "Min": 300.0, "Max": 400.0 }
  ],
  "SnitchProbability": [
    { "MinOf": 0, "Min": 0.0, "Max": 0.53 },
    { "MinOf": 5, "Min": 0.0, "Max": 0.65 },
    { "MinOf": 10, "Min": 0.0, "Max": 0.7 },
    { "MinOf": 20, "Min": 0.0, "Max": 0.75 },
    { "MinOf": 30, "Min": 0.0, "Max": 0.85 },
    { "MinOf": 40, "Min": 0.0, "Max": 0.9 },
    { "MinOf": 50, "Min": 0.05, "Max": 0.95 },
    { "MinOf": 60, "Min": 0.1, "Max": 1.0 },
    { "MinOf": 70, "Min": 0.15, "Max": 1.0 },
    { "MinOf": 80, "Min": 0.2, "Max": 1.0 },
    { "MinOf": 90, "Min": 0.25, "Max": 1.0 }
  ],
  "BuyBustProbability": [
    { "MinOf": 0, "Min": 0.0, "Max": 1.0 },
    { "MinOf": 5, "Min": 0.0, "Max": 0.9 },
    { "MinOf": 10, "Min": 0.0, "Max": 0.8 },
    { "MinOf": 15, "Min": 0.0, "Max": 0.75 },
    { "MinOf": 20, "Min": 0.0, "Max": 0.65 },
    { "MinOf": 25, "Min": 0.0, "Max": 0.75 },
    { "MinOf": 30, "Min": 0.0, "Max": 0.6 },
    { "MinOf": 40, "Min": 0.0, "Max": 0.55 },
    { "MinOf": 50, "Min": 0.0, "Max": 0.49 }
  ]
}
```
- **MinOf** value indicates the minimum required value for the observed threshold.
- **Min** and **Max** are the random value boundaries that get selected when that threshold is met.


- **LethalCopFrequency**:
  - **MinOf**: Days played in the save
  - **Min & Max**: Random amount of seconds that is waited before Lethal Cops can trigger

- **LethalCopRange**:
  - **MinOf**: Total Networth in the save
  - **Min & Max**: Random range how nearby player has to be to an officer for Lethal Cops to trigger

- **NearbyCrazyFrequency**:
  - **MinOf**: Days played in the save
  - **Min & Max**: Random amount of seconds that is waited before Nearby Crazy Cops can trigger

- **NearbyCrazyRange**:
  - **MinOf**: Total Networth in the save
  - **Min & Max**: Random range how nearby player has to be to an officer for Nearby Crazy Cops to trigger

- **PIFrequency**:
  - **MinOf**: Total Networth in the save
  - **Min & Max**: Random amount of seconds that is waited before an investigator can spawn again

- **SnitchProbability**:
  - **MinOf**: Days played in the save
  - **Min & Max**: Random range of the chance to trigger Snitching Samples. When the random value goes above 0.5 the event triggers.

- **BuyBustProbability**:
  - **MinOf**: Customer Relationship (0 worst relations to 50 best relations)
  - **Min & Max**: Random range of the chance to trigger a Buy Bust. When the random value goes above 0.5 the event triggers.

---

> **Note**: The configuration files and directory structure described in this document will be created automatically in the `Mods/NACops/` directory if they are missing.

---

Contribute, Build from Source or Verify Integrity -> [GitHub](https://github.com/XOWithSauce/schedule-nacops/)
# SCHEDULE I NA COPS MOD

**NEEDS MELON LOADER (BOTH ALTERNATE AND DEFAULT VERSIONS ARE NOW SUPPORTED!)**

## Features

- Makes the cops more lethal by making them arrest you easily and conducting searches periodically
- Cops now occasionally use lethal force if you approach them
- Cops will occasionally appoint a disguised Private Investigator to monitor you
- Cops will now try to search for players smoking ganja and also apprehend the suspect
- Cops will give you more crime charges if arrested
- New customers will try to snitch on you resulting in Car Dispatches and Investigation
- When dealing to customers, they have a chance to be a part of a Buy Bust!
- Overall Cops difficulty is tied to Game Progression:
	- Total Earnings
   	- Total Days in the Save
   	- Customer Relationships

## Important!

- **"alternate" or "alternate-beta" branch users**: Download the `NACopsV1-Mono` version.
- **"default" or "beta" branch users**: Download the `NACopsV1-IL2CPP` version.

## Installation Steps

1. Install Melon Loader from a trusted source like [MelonWiki](https://melonwiki.xyz/).
2. Copy the DLL file and the `NACops` folder (with `config.json`) into the `Mods` folder.
3. You are good to go!

## Configuration

The mod supports overriding behaviors and variables to allow cross-compatibility with other mods.

### (Optional) Configuration Steps:

1. Open the `NACops` folder and locate the file called `config.json`.
2. The default contents of the `config.json` file are as follows:
   
```json
{
  "OverrideMovement": true,
  "OverrideCombatBeh": true,
  "OverrideBodySearch": true,
  "OverrideWeapon": true,
  "OverrideMaxHealth": true,
  "LethalCops": true,
  "NearbyCrazyCops": true,
  "CrazyCops": true,
  "PrivateInvestigator": true,
  "WeedInvestigator": true,
  "CorruptCops": true,
  "SnitchingSamples": true,
  "BuyBusts": true,
  "IncludeSpawned": false
}
```
- Override(Parameter):
	- t﻿rue: Uses the NACops mod settings on the officers and overrides any other mod settings
	- false: Uses the game default settings on the officers or other mods settings.

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

 - IncludeSpawned:
	- true: When the mod is running, tries to search for spawned / despawned cops. Only enable this feature if you use mods that spawn or despawn cops at runtime!
   	- false: Default Disabled.



> **Note**: The `config.json` file will be automatically created in the `Mods/NACops/config.json` directory if it's missing.

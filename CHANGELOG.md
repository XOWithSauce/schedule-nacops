# Version 2.1.0
- Ensured that mod works in the latest 0.4.6f11 update, requiring alot of refactoring and bugfixing
- Changed MelonLoader dependency to be 0.7.2 or Higher (prefer 0.7.3)
- Added new feature Mass Surveillance, where cameras across the Hyland Point track player, potentially alerting cops if crimes are noticed
  - Additionally this feature allows for changing the Fine payment amounts for crimes (with mod default being that crime fines increase with progression)
  - Additionally this feature allows for paying crime charges from Bank balance instead of deducting fines from cash
- Added support for using MelonPreferences alongside the json config system
  - Creates the main mod config.json equivalent options for preferences
  - Melon Preferences settings will always take precedence over config.json file settings, and if config.json doesnt match the Melon Preferences, then the config.json will be updated to match Melon Preferences
  - Tried to make most of the features support changing at runtime, but it still requires extensive testing and might have issues if changed at runtime
- Added support for installing the mod through Thunderstore Mod Manager
- Added 3-hour (ingame) cooldown to Buy Bust event so that it doesnt happen back to back in a short timeframe
- Added support for changing the officers ranged weapons from officer.json file at "RangedWeapon", with supported values as "m1911", "goldenm1911", "revolver" and "shotgun". This will instantiate the desired weapon and show it in the officers holsters.
- Added support for changing the officers Taser settings in officer.json file
  - Subsequently changed the Taser to be a bit better than it is by default and also it now deals small amount of damage
- Added support for changing the officers Ranged weapons aim time settings in officer.json file and mod default set to slightly increase the speed at which officers aim with  weapons
- Added support for changing the officers Vision settings in officer.json file (controls how fast they notice crimes and at what range)
- Added support for disabling the officers notice icons (the ? and !) that appear when they are seeing the player in officer.json file at "ShowNoticeIcons" (default true)
- Added support for disabling Road Checkpoints in the main config.json file at "CheckpointsEnabled"
- Added console support for visualizing the new Mass Surveillance camera locations with `nacops visualize surveillance`
- Added logic to Private Investigator to allow them to rarely make phone calls during investigation and if player is inside a property, the investigator can rarely come close to the building and take photos of players property with the phone camera
- Changed the "CombatGiveUpRange" and "CombatSearchTime" in officer.json file to be the game default values (9999.0)
- Changed the mod default Officer movement speed from 1.75 down to 1.45 in officer.json file
- Changed all mod related data to load from and save to UserData folder instead of the Mods folder
- Changed the mod name to not have the "V1" suffix anymore
- Changed officers spawning logic to instantiate a clone from a runtime officer object and re-initializing it due to spawnable prefabs not having an officer template prefab anymore
- Changed Buy Bust officer to not spawn a new officer each time, instead now it uses a single officer object that is instantiated at startup
- Changed Private Investigator officer to not spawn a new officer each time, instead also now uses a single officer object that is instantiated at startup
- Changed the Private Investigator movement logic and behaviour to avoid player vision more and to keep distance to the player
- Changed the Private Investigator movement logic to calculate distance to nearest road nodes closest point on the line segment to avoid pausing movement while the officer is on a road and to avoid pathfinding to a position that is on a road (might not work perfectly at all locations)
- Changed the Private Investigator movement logic and behaviour to evaluate faster or slower based on the current state
- Changed the Private Investigator Movement speed to be relative to distance to the player, with distance increasing the movement speed. Simultaneously decreased the default movement speed drastically.
- Adjusted Private Investigator investigation result calculation to account for faster evaluation speed
- Rotated the Private Investigator names to new ones
- Changed Foot patrol and Sentry config logic to use the Min and Max members amount, with the config value "member" being assigned now to MaxMembers amount, and the Min members value defaulting always to 1.
- Changed Buy Bust officer to not instantly despawn upon dying, instead now has 30-second wait before despawning
- Changed the Property Heat data to save to a json file with (slot number)_(organisation name).json formatting instead of just the organisation name
- Changed the Avatar randomization logic to track all created ScriptableObject instances so that upon save exit or save reload the previous ScriptableObject instances are destroyed
- Fixed a bug where if the CanEnterBuildings is true in officer.json, player could enter unowned businesses and properties
- Fixed a bug where the mod added vehicle patrol routes didnt have the class field for the route name assigned
- Fixed a bug where the main mod config had the LethalCops set to true in the config loader module, when it was supposed to be default false
- Removed Mod added vehicle count from officer.json file
- Added to DEBUG configuration builds support for generating .csv files for analyzing police officer related behaviour weekdays, lengths, total availability, etc. to help with future development. This is old code which was removed in version 2.0.0, but now brought back lol. 2 different modes are available for this: static and runtime analysis, with static being a fast quick check and runtime analysis taking longer but being more precise.
  - Static analysis `nacops list analytics` -> Reads all the police scheduled events and behaviours for each weekday and outputs a .csv file to the mod UserData folder
  - Runtime analysis `nacops build analytics start` -> Starts recording all officer behaviours at runtime to output a precise amount of ran police events and behaviours for each weekday, outputting a .csv file when sending `nacops build analytics stop` command.

# Version 2.0.2
- Ensured that mod works in the 0.4.5f2 update
- Fixed a bug in the feature Notice Open Carry weapons where brandishing state was being removed incorrectly when player was holding a weapon
- Removed redundant code from the Notice Open Carry weapons feature

# Version 2.0.1

- Ensured that mod works in the 0.4.3f3 update
- Added 2 new config values to the officer.json file "WeaponPath" and "WeaponDamage", the weapon path is work in progress and will not do anything in this update.
- Changed the raid officer maximum waiting time inside the property while traveling to be 15 seconds up from 10 seconds so they will get interrupted by another search routine if they fail to path to the next object under 15 seconds.
- Changed Investigator office to change property heat using a multiplier based on the players last crime status when the investigation concludes. This means that higher crime level at the end will also decrease property heat more. Having Dead or Alive crime status at the end causes the investigation resulted heat to be multiplied by 1.75, Wanted 1.45 and Under Arrest 1.2.
- Changed Buy Bust officer to wait 8 seconds before enabling their arresting to avoid instant arrests
- Simplified Buy Bust officer code
- Fixed a bug in IL2CPP version of the mod where buy bust officer would not work due to incorrect harmony function input parameters
- Fixed a bug in the mod spawned officers where the equipped belt would be unassigned causing errors in game logs
- FIxed a bug in the Investigator and Raid Officers where they would cause excessive error messages in game logs
- Fixed a bug in the Investigator where they would stay standing after investigation concludes
- Fixed a bug in the Property Raid feature where the raiders are unable to travel to NPC Spawn Point in the Barn
- Fixed a bug in the Property Raid feature where occasionally the raiders are unable to travel to Manor after traveling to Manor NPC Spawn Point
- Fixed a bug in the Property Raid feature where rarely if 1 officer is alive and refuses to walk forward for some reason the raid would never start and officers would never despawn
- Fixed a bug in the Property Raid feature where the same container could be searched twice
- Fixed a bug where while using the CanEnterBuildings: true value in officer.json and while being under curfew the officers can arrest player inside or near their own property multiple times in a row locking them into infinite arrest loop

# Version 2.0.0
- Added Property Raids feature that allows cops to destroy your properties and steal illegal products. Raids can be configured in raid.json.
  - Raids start after sleeping through the night, in the morning if investigator has built up enough property heat and property hasnt been raided recently
- Added Property Heat system that allows the Property Raids to happen. Private Investigator builds up heat. Heat data is saved in NACops/HeatData/(organisation name).json
- Added new Racist Cops feature where cops really just dont like black people
- Added new No Open Carry Weapons feature that makes guns illegal to carry in hand and in containers
- Added Configuration support for Vehicle Patrols in vehiclepatrols.json file
- Added Configuration support for allowing Officers to enter inside buildings in the officers.json file (CanEnterBuildings : true)
  - While this is enabled (by default in the mod) the player can enter their buildings while wanted
- Added Configuration support for changing arrest speed and range in the officers.json file
- Added Configuration support for changing the max amount of police vehicles in the game in the officers.json file
- Added Configuration support for the Frequencies, Ranges and Probabilities of certain events (now in progression.json)
  - Configuration scales difficulty against Days Played, Networth & Customer relationships (more in readme)
- Added support for Random Avatar generation for officers. 
  - Now Private Investigator uses Custom random avatar, clothing and name
  - Cops added by the mod config (officers.json ModAddedOfficersCount) get also custom random avatar
- Added support for using Console commands for NACops features, spawning visuals for the police patrols and building new routes using commands
- Added Shrooms to the WeedInvestigator feature so now cops will try to search for player consuming it

- Refactored the Private Investigator logic to be better for its movement and location selection and also to build up property heat
- Moved the default location of spawnable sentrys, vehicle patrols and foot patrols .json files from NACops/file.json to NACops/Spawn/file.json
- Moved the Vehicle Pursuit related script from CrazyCops (now removed feature) to NearbyCrazyCops feature

- Changed the LethalCops and NearbyCrazyCops to only evaluate the nearest officer always
- Changed the default mod builds to contain all debug logging which can be enabled in runtime by using console command "nacops enable logs", console feedback is enabled by default
- Changed the config officers.json values for WalkSpeed and RunSpeed to be a single value: MovementSpeedMultiplier
- Changed the config system to be more robust and generate the necessary .json configuration templates to Mods/NACops/ directory if missing
- Changed the default order of officers.json related values to be better ordered
- Changed the mod config and startup to start loading after game indicates succesful game load state
- Changed the base probability for customers snitching samples from 0.8 to 0.5 and changed the progression related values in progression.json to compensate
- Changed default body search speed to be faster
- Changed Lethal Cops to be disabled by default in the mod config

- Fixed a bug in the Private Investigator where the warping script would crash the game
- Fixed a bug in the Private Investigator where after exiting a save the mod state would not reset properly and never spawn again
- Fixed a bug in the coroutines that use progression where the progression would not update frequently enough
- Cleaned up avatar related code to prevent errors in the game logs

- Removed random boosting logic for the bodysearch since it didnt really work
- Removed statistics analysis related code from repo 
- Removed CrazyCops feature as it felt excessive / unnecessary

# Version v1.9.1
- Compiled mod against latest default and alternate game versions to make it work

# Version v1.9.0
- Added configuration support for adding custom Foot Patrols and Sentry positions for officers for weekdays independently
- Added support for changing total officers count into the officer.json file
- Moved Officer Override configuration booleans from config.json to officer.json
- Increased the Private Investigator minimum and maximum random cooldown time  
- Changed Lethal Cops to use random player instead of using player.local for evaluation
- Changed Body Search random speed boosts to be less frequent
- Changed default officer.json values to be more balanced
- Changed WeedInvestigator feature to now also include Meth and Cocaine consumption to trigger it
- Changed WeedInvestigator to search for player for a shorter duration
- Removed Docks Raids feature since its not scalable and dont want to maintain that part of code
- Removed IncludeSpawned feature since its now redundant after adding Foot Patrols and Sentries config support
- Cleaned up code and tweaks to improve performance
- Fixed to support latest 0.4.0f8 source code
- Fixed bug in crazy cops, lethal cops, nearby crazy cops and drug apprehender being able to pick the same officer for simultaneous evaluation
- Fixed a bug in WeedInvestigator where it could have overlapping evaluations for selecting an officer to search for player
- Fixed bugs where Foot Patrol or Body search would not be initiated
- Debug builds now provide statistics for officer usage to help with balancing generated patrols and sentries with officer count; see DebugModule for more

# Version v1.8.0
- Increased smallest distance at which drug apprehender can be selected from nearby officers to search for player on foot.
- Officers field-of-view is now checked after player drug consumption and if at 50 units distance. If player is in field-of-view they get immediate bodysearch. Old behaviour but added a cap of 50 units distance to prevent useless vision cone calls lagging the run.
- Decreased the time drug apprehender needs to wait before starting to search for player to 4 seconds instead of time relative to distance
- Drug apprehender function now evaluates officers at a slower pace to fix a bug where player encounters lag during this evaluation
- Drug apprehender can now end foot search early if 5% chance is triggered during consecutive attempts after 6 seconds into search
- Drug apprehender total time slightly increased to 22.5 seconds in max total search length
- Changed drug apprehender foot search to have more consistent behaviour and attempts to traverse for full max length instead of breaking search if target location is unreachable.
- Increased cooldown time for officers to become drug apprehenders again to 30 seconds. During cooldown officer wont respond to nearby drug consumption.
- Body search behaviour has now randomized search speed each time between 8 and 20 seconds, and has 3% chance every frame to randomly toggle speed up during the search. This feature can be disabled by setting OverrideBodySearch to false.
- Added defensive programming measures to prevent type cast errors from breaking a coroutine in WeedInvestigator feature in IL2Cpp version (no reprod bug)
- Added full configuration support for the default values of NACops Officers via officer.json, this includes movement, combat, gun and health variables. See README.md or Description or wiki for info.

- Fixed miscellanious class and variable namings to match the latest available patch
- Fixed a null reference error that was caused by player dying and loading last save in singleplayer
- Fixed a bug that caused duplicate OnLoadComplete callbacks after loading last save in singleplayer.
- Fixed a bug that caused duplicate OnLoadComplete callbacks after quitting to main menu and reloading any save
- Fixed a bug where quitting the game to main menu would cause coroutines to keep running
- Removed a RemoveListener callback function from the save load completion due to being redundant after above fixes

- known bugs:
* While changing costumes of officers during Buy Bust, Private Investigator and Docks Raids, the original object for police officer (which is cloned for the events) retains the costume that was set for the clone. A Random officer in the world has a red cap or a PI costume, but they are NOT a Buy Bust Officer or a PI.

# Version v1.7.3
- Reworked the Private Investigator system to spawn its own cop instead of randomly selecting existing -> Fixes a bunch of miscellanious bugs
- Private investigator now tracks the times seen during investigation, times in proximity and times player spent in docks warehouse, evaluated every 5 sec
- Fixed Private Investigator not adding Session Heat properly after the investigation concludes
- Session Heat can now also be gained even if the player is not in the Docks Warehouse after the investigation concludes
- Private Investigator spawns less often since now the spawning and following process is more consistent and reliable
- Private Investigator eyes start glowing Yellow when halfway, Red when reaching close to triggering a Docks Raid -> Good indicator of Session Heat
- Adjusted the Game Progression values and added new caps for Private investigator spawning threshold
- Adjusted the Game Progression values and added new caps for Private investigator Curfew attention probability
- Adjusted the Game Progression values and added new caps for New Customers Snitch Probability
- Patched the Exit To Menu function to stop coroutines and clear the mod state to prevent errors when exiting the save.
- Slightly decreased the threshold at which raids are evaluated
- Docks Raids now spawns armed cops in the building and attack the player if they misbehave
- Enhanced the cinematic visuals in Docks Raids
- Fixed Bugs with Docks Raids starting and also event run
- Removed the 30% chance requirement to trigger Docks Raids
- Divided BuyBust, PI and Raid cops Avatar settings to their own functions
- Fixed miscellanious bugs that would set officer travel destinations to Vector3.zero

# Version v1.7.2
- Modified WeedInvestigator function to account for the police officer vision -> Consuming in their vision will trigger bodysearch
- Private Investigator now stores FootPatrolBehaviour groups and re-assigns them after state ends
- Fixed Private Investigator Vision during curfew. Now disables Vision Cone totally (or enables it if rolls random chance)
- Added a new Config value DocksRaids
- Modified the Private Investigator to add Session Heat when the investigator sees player enter the Docks Warehouse and reduce Session Heat when the investigation ends.
- Session Heat is cleared to 0 every time save is exited.
- Added a new event that triggers a Raid in the Docks Warehouse (Triggered by Heat reaching 20 and player being in the warehouse and also having dispatch officers available)
  - First the screen will show a red warning
  - Police car is dispatched to the Warehouse
  - Cinematic view of the police arriving will play and the event begins
  - During the Event:
    - 2 Cops will spawn in the warehouse next to Pots and the officers will begin destroying them.
    - You must kill the cops to stop them from doing this (or if they reach max pots destroyed limit)

# Version v1.7.1
- Added a new Config value Include Spawned
- Added a new coroutine to refresh currently active officers and conditionally apply the mod preferred settings (override settings)
- Changed every coroutine and patch that used to reference the officers static array to use a new point-in-time copy of either recently refreshed officers on scene or officers evaluated at scene start.
- Tied the Crazy Cops coroutine range to game progression total earnings -> More earnings larger evaluation range
- Wrapped the Investigation + Car Dispatch logic into its own coroutine and added safety checks to prevent dispatching from Police Station with 0 occupants
- Fixed IL2Cpp version JSON config loading, now using correct Newtonsoft assembly and reverted back to previous config loading logic.
- Added safety checks to prevent game objects or PoliceOfficer variables being null in evaluations
- Removed safety check in Private investigator where the officer is forced to exit vehicle -> condition will never be met since active behaviour and assigned vehicle are caught earlier
- Removed safety check in Crazy cops that would not take into evaluation cops with Vehicle behaviour -> Was previously needed but is now correctly handled by the function so removed.
- Added fast sleeps into the SetOfficers function it was causing lag when ran periodically through Include Spawned feature
- Fixed a bug in the DrugConsumed coro where the function would keep executing even if no officers nearby were picked for apprehending

# Version v1.7.0
- Added Customer Buy Busts -> When dealing based on customer relation rolls a chance to spawn a cop behind you that attempts to apprehend you with taser
- Adjusted the game progression based thresholds and frequencies for Crazy cops and Nearby crazy cops to be harder
- Added more levels to check against in Lethal Cop Range -> Bigger lifetime earning results in larger lethal cop range and caps at 1mil
- IL2Cpp version has a temporary fix for json config loading until next update

# Version v1.6.2
- Adjusted difficulty curve for game progression slightly to be less harder for all events
- Added a new patch for Sample consumption on new customers to call the police and snitch on you -> Dispatch a vehicle patrol to your location and start investigation.
- Snitch Probability tied to game progression
- Added checks for Lethal cops to not allow player being lethally targeted if officer is currently in a Checkpoint Behaviour.
- Added support for all coroutines and patches to be disabled / enabled accordingly from config.json
- Changed function logic that gives more crime charges to player
- PI Evaluates attention at curfew time based on game progression -> Random roll chance might enable the attention during curfew and allow it to see player.
- Adjusted Nearby Crazy Cop movement speed when traveling to player previous location from 7 -> 5

# Version v1.6.1
- Caches previous behaviours and parameters accordingly to allow other mods compatibilities
- Added support for config.json settings to allow other mods compatibilities
- Added checks to prevent Vehicle Patrols from glitching
- Added fixes for other miscellanious bugs
- PI vision changed to not "notice" player during curfew time

# Version v1.6.0
- PI Evaluates Crime status on player -> Undisquises and stops state
- Fixed previously broken logic with on load complete
- Added vision checks for alot of functions to make the cops more "forgiving" on pursuits
- Further tied the game into progression to increment Random Range upper and lower boundaries -> Increase evaluation range the more money player makes
- Fixed Player dying in Police Station -> Distance to Object must be atleast 25f (might be lowered on future updates)
- Added Investigating behaviour to be evaluated in Crazy cops with 30% of occuring.

# Version v1.5.2
- Fixing melon loader logic and fixing bugs

# Version v1.5.1
- Fixing startup logic and fixing bugs

# Version v1.5.0
- Fixed Crazy cop movement not resetting after state ends
- Fixed Nearby Crazy cop not Turning towards player before evaluating player visibility
- PI Evaluation forces officer to end behaviour, exit buildings and cars -> warp random nearby mesh position on player location
- Removed the PI Eye light glowing
- Adjusted PI Threshold upper random range to be lower -> PI appointed atleast 3 times a day

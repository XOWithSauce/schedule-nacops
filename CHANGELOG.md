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

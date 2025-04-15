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
- Added fast sleeps into the SetOfficers function to make it less laggy
- Fixex a bug in the DrugConsumed coro where the function would keep executing even if no officers nearby were picked for apprehending

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

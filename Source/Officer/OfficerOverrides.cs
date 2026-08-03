using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using MelonLoader;

using static NACops.NACops;
using static NACops.DebugModule;

#if MONO
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.Police;
using ScheduleOne.Vision;
using ScheduleOne.DevUtilities;
using ScheduleOne.Vehicles;
#else
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Vision;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Vehicles;
#endif


namespace NACops
{


    #region Harmony Arrest Update
    [HarmonyPatch(typeof(PursuitBehaviour), "UpdateArrest")]
    public static class PursuitBehaviour_UpdateArrest_Patch
    {
        public static bool Prefix(PursuitBehaviour __instance, float tick)
        {
            // if config is false dont patch this method
            if (!officerConfig.OverrideArresting) return true;

            // else its identical to source code but bound to config
            // with range and speed
            if (__instance.TargetPlayer == null) return false;
            if (!__instance.arrestingEnabled) return false;
            if (Vector3.Distance(__instance.Npc.CenterPoint, __instance.TargetPlayer.Avatar.CenterPoint) < officerConfig.ArrestRange && __instance.IsTargetRecentlyVisible)
            {
                __instance.timeWithinArrestRange += tick;
                if (__instance.timeWithinArrestRange > 0.5f) 
                    __instance.wasInArrestCircleLastFrame = true;
            }
            else
            {
                if (__instance.wasInArrestCircleLastFrame)
                {
                    __instance.leaveArrestCircleCount++;
                    __instance.wasInArrestCircleLastFrame = false;
                }
                __instance.timeWithinArrestRange = Mathf.Clamp(__instance.timeWithinArrestRange - tick, 0f, float.MaxValue);
            }

            if (__instance.TargetPlayer.IsOwner && __instance.timeWithinArrestRange / officerConfig.ArrestTime > __instance.TargetPlayer.CrimeData.CurrentArrestProgress)
            {
                __instance.TargetPlayer.CrimeData.SetArrestProgress(__instance.timeWithinArrestRange / officerConfig.ArrestTime);
            }

            // dont run since mod handles identical logic
            return false;
        }
    }
    #endregion

    #region Harmony Patch weapon equip for override
    [HarmonyPatch(typeof(PursuitBehaviour), "UpdateLethalBehaviour")]
    public static class PursuitBehaviour_UpdateLethalBehaviour_Patch
    {
        private static readonly string functionName = "UpdateLethalBehaviour";
        public static bool Prefix(PursuitBehaviour __instance)
        {
            // if config is false or gun is default dont patch this method
            if (!officerConfig.OverrideWeapon) return true;
            if (officerConfig.RangedWeapon.ToLower() == "m1911") return true;

            // Otherwise identical except for the set weapon function
            float num = Vector3.Distance(__instance.transform.position, __instance.TargetPlayer.Avatar.CenterPoint);
            __instance.SetMovementSpeed(Mathf.Lerp(0.7f, 0.9f, Mathf.Clamp01(num / 6f)), "combat", 5);

            // Setweapon function needs to be different so that it instantiates from that gunprefab
            // instead of loading from resources because otherwise it wont have the custom stats
            // applied (or needs to apply them each time again)
            if (__instance.currentWeapon != null)
            {
                if (__instance.officer.GunPrefab.AssetPath == __instance.currentWeapon.AssetPath)
                {
                    return false;
                }
                __instance.ClearWeapon();
            }

            if (__instance.VirtualPunchWeapon == null)
            {
                //Log("Officer is missing VirtualPunchWeapon!", functionName);
                return true;
            }
            if (__instance.VirtualPunchWeapon.onSuccessfulHit == null)
            {
                //Log("Officer is missing VirtualPunchWeapon.onSuccesfulHit event!", functionName);
                __instance.VirtualPunchWeapon.onSuccessfulHit = new();
            }
            __instance.VirtualPunchWeapon.onSuccessfulHit.RemoveListener((UnityAction)__instance.SucessfulHit);

            // Avatar setequip instantiated from custom prefab base
            if (__instance.officer.Avatar.CurrentEquippable != null)
            {
                __instance.officer.Avatar.CurrentEquippable.Unequip();
            }

            __instance.officer.Avatar.CurrentEquippable = UnityEngine.Object.Instantiate<GameObject>(OfficerOverrides.rangedWeaponPrefab.gameObject, null).GetComponent<AvatarEquippable>();
            __instance.officer.Avatar.CurrentEquippable.Equip(__instance.officer.Avatar);

            // weapon equip
#if MONO
            __instance.currentWeapon = __instance.officer.Avatar.CurrentEquippable as AvatarWeapon;
#else
            __instance.currentWeapon = __instance.officer.Avatar.CurrentEquippable.TryCast<AvatarWeapon>();
#endif
            if (__instance.currentWeapon.onSuccessfulHit == null)
            {
                //Log("New weapon is missing onSuccesfulHit Event", functionName);
                __instance.currentWeapon.onSuccessfulHit = new();
            }
            __instance.currentWeapon.onSuccessfulHit.AddListener((UnityAction)__instance.SucessfulHit);
            if (__instance.currentWeapon == null)
            {
                //Log("Failed to equip weapon", functionName);
                return false;
            }
            __instance.OnCurrentWeaponChanged(__instance.currentWeapon);

            // dont run since mod handles identical logic
            return false;
        }
    }
#endregion

    public static class OfficerOverrides
    {
        public static AvatarEquippable rangedWeaponPrefab;
        public static IEnumerator SetOfficers()
        {
            bool hasInstantiatedRangedWeapon = false;
            bool hasOverridenWeaponPrefab = false;
            bool hasOverridenTaserPrefab = false;

            Log("Set officers foreach stats for " + allActiveOfficers.Count);
            foreach (PoliceOfficer officer in allActiveOfficers)
            {
                yield return Wait01;

                officer.Awareness.VisionCone.WorldspaceIconsEnabled = officerConfig.ShowNoticeIcons;

                if (officerConfig.CanEnterBuildings)
                    officer.Movement.Agent.areaMask = 57; // identical to employee

                if (officerConfig.OverrideBodySearch)
                {
                    officer.BodySearchDuration = officerConfig.BodySearchDuration;
                    officer.BodySearchChance = officerConfig.BodySearchChance;
                }

                if (officerConfig.OverrideMovement)
                    officer.Movement.MoveSpeedMultiplier = officerConfig.MovementSpeedMultiplier;

                if (officerConfig.OverrideCombatBeh)
                {
                    officer.Behaviour.CombatBehaviour.GiveUpRange = officerConfig.CombatGiveUpRange;
                    officer.Behaviour.CombatBehaviour.DefaultSearchTime = officerConfig.CombatSearchTime;
                    officer.Behaviour.CombatBehaviour.DefaultMovementSpeed = officerConfig.CombatMoveSpeed;
                    officer.Behaviour.CombatBehaviour.GiveUpAfterSuccessfulHits = officerConfig.CombatEndAfterHits;
                }

                if (officerConfig.OverrideMaxHealth)
                {
                    officer.NPCData.Health.MaxHealth = officerConfig.OfficerMaxHealth;
                    officer.Health.Health = officerConfig.OfficerMaxHealth;
                }

                if (officerConfig.OverrideWeapon && !hasOverridenWeaponPrefab)
                {
                    // instantiate if not default
                    Log("Setup Override Weapon");
                    string resourcePath = "";

                    switch (officerConfig.RangedWeapon.ToLower())
                    {
                        case "m1911":
                            resourcePath = string.Empty;
                            break;

                        case "goldenm1911":
                            resourcePath = "Avatar/Equippables/M1911_Gold";
                            break;

                        case "revolver":
                            resourcePath = "Avatar/Equippables/Revolver";
                            break;

                        case "shotgun":
                            resourcePath = "Avatar/Equippables/PumpShotgun";
                            break;

                        default:
                            resourcePath = string.Empty;
                            break;
                    }

                    // if not default instantiate
                    if (!hasInstantiatedRangedWeapon && resourcePath != string.Empty)
                    {
                        Log("Instantiating custom weapon from path: " + resourcePath);
#if MONO
                        GameObject gameObject = Resources.Load(resourcePath) as GameObject;
#else
                        UnityEngine.Object obj = Resources.Load(resourcePath);
                        GameObject gameObject = obj.TryCast<GameObject>();
#endif
                        if (gameObject == null)
                        {
                            Log($"Custom weapon was not found in built resources");
                        }
                        else
                        {
                            rangedWeaponPrefab = UnityEngine.Object.Instantiate<GameObject>(gameObject, new Vector3(0f, -5f, 0f), Quaternion.identity, null).GetComponent<AvatarEquippable>();
                            if (!rangedWeaponPrefab.gameObject.activeSelf)
                                rangedWeaponPrefab.gameObject.SetActive(true);
                        }
                        hasInstantiatedRangedWeapon = true;
                    }

                    // Override stats
                    AvatarRangedWeapon rangedWeapon = null;

                    if (hasInstantiatedRangedWeapon && rangedWeaponPrefab != null)
                        officer.GunPrefab = rangedWeaponPrefab;
#if MONO
                    rangedWeapon = officer.GunPrefab as AvatarRangedWeapon;
#else
                    rangedWeapon = officer.GunPrefab.TryCast<AvatarRangedWeapon>();
#endif
                    if (rangedWeapon != null)
                    {
                        rangedWeapon.MagazineSize = officerConfig.WeaponMagSize;
                        rangedWeapon.MaxFireRate = officerConfig.WeaponFireRate;
                        rangedWeapon.CooldownDuration = officerConfig.WeaponFireRate;
                        rangedWeapon.MaxUseRange = officerConfig.WeaponMaxRange;
                        rangedWeapon.ReloadTime = officerConfig.WeaponReloadTime;
                        rangedWeapon.RaiseTime = officerConfig.WeaponRaiseTime;
                        rangedWeapon.HitChance_MaxRange = officerConfig.WeaponHitChanceMax;
                        rangedWeapon.HitChance_MinRange = officerConfig.WeaponHitChanceMin;
                        rangedWeapon.Damage = officerConfig.WeaponDamage;
                        rangedWeapon.AimTime_Max = officerConfig.WeaponAimTimeMax;
                        rangedWeapon.AimTime_Min = officerConfig.WeaponAimTimeMin;
                    }
                    hasOverridenWeaponPrefab = true;
                }

                if (hasInstantiatedRangedWeapon && rangedWeaponPrefab != null)
                {
                    // override the field
                    officer.GunPrefab = rangedWeaponPrefab;

                    // Fix belt so that it shows the custom weapon
                    // Find within the instantiated object, just the model gameobject to use for belt
                    // and assign the correct local orientation + location 
                    string modelTransformName = "";
                    Vector3 selectedScale = Vector3.zero;
                    Vector3 customPos = Vector3.zero;
                    Vector3 customRot = Vector3.zero;
                    switch (officerConfig.RangedWeapon.ToLower())
                    {
                        case "goldenm1911":
                            modelTransformName = "M1911";
                            selectedScale = new(0.008f, 0.008f, 0.008f);
                            customPos = new(0.0016f, -0.0001f, -0.0007f);
                            customRot = new(290f, 330f, 220f);
                            break;

                        case "revolver":
                            modelTransformName = "Revolver_";
                            selectedScale = new(0.008f, 0.008f, 0.008f);
                            customPos = new(0.0016f, -0.0001f, -0.0008f);
                            customRot = new(90f, 10f, 0f);
                            break;

                        case "shotgun":
                            modelTransformName = "Shotgun";
                            selectedScale = new(0.008f, 0.008f, 0.008f);
                            customPos = new(0.0016f, -0.0005f, -0.0005f);
                            customRot = new(80f, 150f, 150f);
                            break;
                    }
                    Transform modelTr = rangedWeaponPrefab.transform.Find(modelTransformName);
                    if (modelTr == null)
                    {
                        Log($"Failed to instantiate belt gun model! Missing prefab gun model transform object at {rangedWeaponPrefab.transform.GetScenePath()} / {modelTransformName}");
                    }
                    GameObject model = UnityEngine.Object.Instantiate(modelTr.gameObject);
                    model.transform.parent = officer.belt.transform.GetChild(0);
                    model.transform.localScale = selectedScale;
                    model.transform.SetLocalPositionAndRotation(customPos, Quaternion.Euler(customRot));
                    officer.belt.GunObject.SetActive(false);
                    officer.belt.GunObject = model;
                    if (!model.gameObject.activeSelf)
                        model.gameObject.SetActive(true);
                }

                if (officerConfig.OverrideTaser && !hasOverridenTaserPrefab)
                {
                    Log("Overriding taser prefab");
                    AvatarRangedWeapon rangedWeapon = null;
#if MONO
                    rangedWeapon = officer.TaserPrefab as AvatarRangedWeapon;
#else
                    rangedWeapon = officer.TaserPrefab.TryCast<AvatarRangedWeapon>();
#endif
                    if (rangedWeapon != null)
                    {
                        rangedWeapon.MaxFireRate = officerConfig.TaserFireRate;
                        rangedWeapon.CooldownDuration = officerConfig.TaserFireRate;
                        rangedWeapon.MaxUseRange = officerConfig.TaserMaxRange;
                        rangedWeapon.ReloadTime = officerConfig.TaserReloadTime;
                        rangedWeapon.RaiseTime = officerConfig.TaserRaiseTime;
                        rangedWeapon.HitChance_MaxRange = officerConfig.TaserHitChanceMax;
                        rangedWeapon.HitChance_MinRange = officerConfig.TaserHitChanceMin;
                        rangedWeapon.Damage = officerConfig.TaserDamage;
                        rangedWeapon.AimTime_Max = officerConfig.TaserAimTimeMax;
                        rangedWeapon.AimTime_Min = officerConfig.TaserAimTimeMin;
                    }
                    Log("  Overridden Taser");
                    hasOverridenTaserPrefab = true;
                }

                if (officerConfig.OverrideVision)
                {
                    // apply the range
                    officer.Awareness.VisionCone.RangeMultiplier = officerConfig.VisionRangeMultiplier;

                    // apply a callback to the onExitVehicle due to a bug where the rangeMultiplier resets during it
                    void OfficerExitedVehicle(LandVehicle _)
                    {
                        MelonCoroutines.Start(ResetVisionRange(officer));
                    }

#if MONO
                    officer.onExitVehicle += (Action<LandVehicle>)OfficerExitedVehicle;
#else
                    officer.onExitVehicle += (Il2CppSystem.Action<LandVehicle>)OfficerExitedVehicle;
#endif

                    // for each mapped player within the current settings
                    foreach (var keyPlayerValDict in officer.Awareness.VisionCone.stateSettings)
                    {
                        // foreach of the state settings entry
                        foreach (var kvp in officer.Awareness.VisionCone.stateSettings[keyPlayerValDict.Key])
                        {
                            if (kvp.Key == EVisualState.Visible) continue; // skip

                            // change based on the key string representation to match the config value
                            if (officerConfig.VisionSpeed.TryGetValue(kvp.Key.ToString(), out float newSpeed))
                            {
                                // Notice time itself is unassignable but deterministic because of the code
                                // so reverse that logic here
                                float noticeTimeMult = newSpeed / 0.2f;
                                officer.Awareness.VisionCone.stateSettings[keyPlayerValDict.Key][kvp.Key].NoticeTimeMultiplier = noticeTimeMult;
                            }
                            else
                            {
                                Log("Failed to find matching state container entry from config vision state settings: " + kvp.Key.ToString());
                            }
                        }
                    }

                    Log("  Overridden Vision");
                }


            }
            Log("Officer properties complete");
            yield break;
        }

        public static IEnumerator ResetVisionRange(PoliceOfficer offc)
        {
            yield return Wait05;
            if (!registered) yield break;
            offc.Awareness.VisionCone.RangeMultiplier = officerConfig.VisionRangeMultiplier;
            yield break;
        }
    }

}
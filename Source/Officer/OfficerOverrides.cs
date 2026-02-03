
using System.Collections;
using HarmonyLib;
using UnityEngine;

using static NACopsV1.NACops;
using static NACopsV1.DebugModule;
using static NACopsV1.AvatarUtility;

#if MONO
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.Police;
using ScheduleOne.AvatarFramework.Customization;
using FishNet.Object;
#else
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.AvatarFramework.Customization;
using Il2CppFishNet.Object;
#endif


namespace NACopsV1
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

    public static class OfficerOverrides
    {
#if MONO
        public static List<PoliceOfficer> generatedOfficerPool = new(); // track ref
#else
        public static Il2CppSystem.Collections.Generic.List<PoliceOfficer> generatedOfficerPool = new();
#endif
        public static IEnumerator ExtendOfficerPool()
        {
#if MONO
            PoliceStation station = PoliceStation.PoliceStations.FirstOrDefault();
#else
            if (PoliceStation.PoliceStations.Count == 0)
                yield break;
            PoliceStation station = PoliceStation.PoliceStations[0];
#endif
            for (int i = 0; i < station.OfficerPool.Count; i++)
                generatedOfficerPool.Add(station.OfficerPool[i]);

            for (int i = 0; i < officerConfig.ModAddedOfficersCount; i++)
            {
                PoliceOfficer offc = SpawnOfficerRuntime(i);
                station.NPCEnteredBuilding(offc);
                generatedOfficerPool.Add(offc);
                PoliceOfficer.Officers.Add(offc);
                
            }
            station.OfficerPool = generatedOfficerPool;
            yield return null;
        }

        public static PoliceOfficer SpawnOfficerRuntime(int i, bool autoDeactivate = true)
        {
            NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(policeBase);
            PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
            offc.AutoDeactivate = autoDeactivate; // Prevent from returning to station and from being added to officer pool

            NPC myNpc = copNet.gameObject.GetComponent<NPC>();
            myNpc.ID = $"NACops_RuntimeOfficer_{i}";
            myNpc.FirstName = "Officer";
            myNpc.LastName = "";
            myNpc.transform.parent = NPCManager.Instance.NPCContainer;

            NPCManager.NPCRegistry.Add(myNpc);
            networkManager.ServerManager.Spawn(copNet);
            copNet.gameObject.SetActive(true);
            copNet.name = $"NACops_RuntimeOfficer_{i}";
            return offc;
        }
     
        public static PoliceOfficer SpawnOfficerRuntime(bool autoDeactivate = true)
        {
            return SpawnOfficerRuntime(UnityEngine.Random.Range(1000, 6000), autoDeactivate);
        }

        public static IEnumerator SetOfficers()
        {
            // offc belt is always unassigned on spawned ones 
            foreach (PoliceOfficer officer in allActiveOfficers)
            {
                if (officer.belt != null) continue;
                for (int j = 0; j < officer.Avatar.appliedAccessories.Length; j++)
                {
                    if (officer.Avatar.appliedAccessories[j] == null) continue;
                    if (officer.Avatar.appliedAccessories[j].AssetPath == "Avatar/Accessories/Waist/PoliceBelt/PoliceBelt")
                    {
                        PoliceBelt beltComp = officer.Avatar.appliedAccessories[j].gameObject.GetComponent<PoliceBelt>();
                        if (beltComp != null)
                            officer.belt = beltComp;
                        else
                            Log("Belt component is null and cant assign");
                        break;
                    }
                }
            }
            

            Log("Set officers foreach stats for " + allActiveOfficers.Count);
            foreach (PoliceOfficer officer in allActiveOfficers)
            {
                yield return Wait01;
                officer.Leniency = 0.1f;
                officer.Suspicion = 1f;

                SetRandomAvatar(officer);

                if (officerConfig.CanEnterBuildings)
                {
                    officer.Movement.Agent.areaMask = 57; // identical to employee
                }

                if (officerConfig.OverrideBodySearch)
                {
                    officer.BodySearchDuration = officerConfig.BodySearchDuration;
                    officer.BodySearchChance = officerConfig.BodySearchChance;
                }

                if (officerConfig.OverrideMovement)
                {
                    officer.Movement.MoveSpeedMultiplier = officerConfig.MovementSpeedMultiplier;
                }

                if (officerConfig.OverrideCombatBeh)
                {
                    officer.Behaviour.CombatBehaviour.GiveUpRange = officerConfig.CombatGiveUpRange;
                    officer.Behaviour.CombatBehaviour.DefaultSearchTime = officerConfig.CombatSearchTime;
                    officer.Behaviour.CombatBehaviour.DefaultMovementSpeed = officerConfig.CombatMoveSpeed;
                    officer.Behaviour.CombatBehaviour.GiveUpAfterSuccessfulHits = officerConfig.CombatEndAfterHits;
                }

                if (officerConfig.OverrideMaxHealth)
                {
                    officer.Health.MaxHealth = officerConfig.OfficerMaxHealth;
                    officer.Health.Health = officerConfig.OfficerMaxHealth;
                    if (officer.Health.IsDead || officer.Health.IsKnockedOut)
                        officer.Health.Revive();
                }

                if (officerConfig.OverrideWeapon)
                {
                    AvatarRangedWeapon rangedWeapon = null;
#if MONO
                    rangedWeapon = officer.GunPrefab as AvatarRangedWeapon;
#else
                    rangedWeapon = officer.GunPrefab.TryCast<AvatarRangedWeapon>();
#endif
                    if (rangedWeapon != null)
                    {
                        rangedWeapon.CanShootWhileMoving = true;
                        rangedWeapon.MagazineSize = officerConfig.WeaponMagSize;
                        rangedWeapon.MaxFireRate = officerConfig.WeaponFireRate;
                        rangedWeapon.MaxUseRange = officerConfig.WeaponMaxRange;
                        rangedWeapon.ReloadTime = officerConfig.WeaponReloadTime;
                        rangedWeapon.RaiseTime = officerConfig.WeaponRaiseTime;
                        rangedWeapon.HitChance_MaxRange = officerConfig.WeaponHitChanceMax;
                        rangedWeapon.HitChance_MinRange = officerConfig.WeaponHitChanceMin;
                        rangedWeapon.CooldownDuration = officerConfig.WeaponFireRate;
                        rangedWeapon.Damage = officerConfig.WeaponDamage;
                    }

                    if (rangedWeapon != null)
                    {
                        if (officer.Behaviour.CombatBehaviour.DefaultWeapon == null)
                            officer.Behaviour.CombatBehaviour.DefaultWeapon = rangedWeapon;
                    }
                    Log("  Overridden weapon");
                }
            }
            Log("Officer properties complete");
            yield break;
        }

        
    }

}
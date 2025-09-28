
using System.Collections;
using HarmonyLib;
using UnityEngine;

using static NACopsV1.NACops;
using static NACopsV1.DebugModule;

#if MONO
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.Map;
using ScheduleOne.NPCs;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.Police;
using ScheduleOne.UI;
using FishNet.Managing;
using FishNet.Object;
#else
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.UI;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Object;
#endif


namespace NACopsV1
{

    #region Harmony Bodysearch
    [HarmonyPatch(typeof(BodySearchScreen), "Open")]
    public static class BodySearch_Open_Patch
    {
        public static bool Prefix(BodySearchScreen __instance, NPC _searcher, ref float searchTime)
        {
            if (officerConfig.OverrideBodySearch)
            {
                searchTime = UnityEngine.Random.Range(8f, 20f);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(BodySearchScreen), "Update")]
    public static class BodySearch_Update_Patch
    {
        private static float randomSpeedTarget = 0f;
        private static float timeUntilNextRandomChange = 0f;
        private static bool isBoostingRandomly = false;
        public static void Postfix(BodySearchScreen __instance)
        {
            if (!__instance.IsOpen || !officerConfig.OverrideBodySearch)
                return;

            if (!isBoostingRandomly)
            {
                if (UnityEngine.Random.Range(0f, 1f) > 0.98f)
                {
                    isBoostingRandomly = true;
                    timeUntilNextRandomChange = UnityEngine.Random.Range(0.8f, 1.5f);
                    randomSpeedTarget = UnityEngine.Random.Range(3.5f, 4.5f);
                }
            }

            if (isBoostingRandomly)
            {
                timeUntilNextRandomChange -= Time.deltaTime;
                if (timeUntilNextRandomChange <= 0f)
                {
                    isBoostingRandomly = false;
                }
                __instance.speedBoost = Mathf.MoveTowards(__instance.speedBoost, randomSpeedTarget, Time.deltaTime * 6f);
            }

            return;
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
            NetworkManager netManager = UnityEngine.Object.FindObjectOfType<NetworkManager>(true);

            for (int i = 0; i < station.OfficerPool.Count; i++)
                generatedOfficerPool.Add(station.OfficerPool[i]);

            for (int i = 0; i < officerConfig.ModAddedOfficersCount; i++)
            {
                NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(policeBase);
                NPC myNpc = copNet.gameObject.GetComponent<NPC>();
                myNpc.ID = $"NACops_Officer_{i}";
                myNpc.FirstName = "Cop";
                myNpc.LastName = "";
                myNpc.transform.parent = NPCManager.Instance.NPCContainer;
                NPCManager.NPCRegistry.Add(myNpc);
                netManager.ServerManager.Spawn(copNet);
                copNet.gameObject.SetActive(true);
                copNet.name = $"NACops_Officer_{i}";
                PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
                generatedOfficerPool.Add(offc);
                station.NPCEnteredBuilding(offc);
            }
            station.OfficerPool = generatedOfficerPool;
            yield return null;
        }

        public static IEnumerator SetOfficers()
        {
            Log("Set officers foreach stats for " + allActiveOfficers.Count);
            foreach (PoliceOfficer officer in allActiveOfficers)
            {
                yield return Wait01;
                officer.Leniency = 0.1f;
                officer.Suspicion = 1f;

                if (officerConfig.OverrideBodySearch)
                {
                    officer.BodySearchDuration = 20f;
                    officer.BodySearchChance = 1f;
                }

                if (officerConfig.OverrideMovement)
                {
                    officer.Movement.RunSpeed = officerConfig.MovementRunSpeed;
                    officer.Movement.WalkSpeed = officerConfig.MovementWalkSpeed;
                }

                if (officerConfig.OverrideCombatBeh)
                {
                    officer.Behaviour.CombatBehaviour.GiveUpRange = officerConfig.CombatGiveUpRange;
                    officer.Behaviour.CombatBehaviour.GiveUpTime = officerConfig.CombatGiveUpTime;
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

                    var gun = officer.GunPrefab; // these 2
                    var pursuitGun = officer.PursuitBehaviour.Weapon_Gun; // ? point to the same object?
#if MONO
                    if (gun != null && gun is AvatarRangedWeapon rangedWeapon)
                    {
                        rangedWeapon.CanShootWhileMoving = true;
                        rangedWeapon.MagazineSize = officerConfig.WeaponMagSize;
                        rangedWeapon.MaxFireRate = officerConfig.WeaponFireRate;
                        rangedWeapon.MaxUseRange = officerConfig.WeaponMaxRange;
                        rangedWeapon.ReloadTime = officerConfig.WeaponReloadTime;
                        rangedWeapon.RaiseTime = officerConfig.WeaponRaiseTime;
                        rangedWeapon.HitChance_MaxRange = officerConfig.WeaponHitChanceMax;
                        rangedWeapon.HitChance_MinRange = officerConfig.WeaponHitChanceMin;

                        if (officer.Behaviour.CombatBehaviour.DefaultWeapon == null)
                            officer.Behaviour.CombatBehaviour.DefaultWeapon = rangedWeapon;
                    }
                    if (gun != null && gun is AvatarWeapon weapon)
                    {
                        weapon.CooldownDuration = officerConfig.WeaponFireRate;
                    }
#else
                    if (gun != null)
                    {
                        AvatarRangedWeapon temp = gun.TryCast<AvatarRangedWeapon>();
                        if (temp != null)
                        {
                            temp.CanShootWhileMoving = true;
                            temp.MagazineSize = officerConfig.WeaponMagSize;
                            temp.MaxFireRate = officerConfig.WeaponFireRate;
                            temp.MaxUseRange = officerConfig.WeaponMaxRange;
                            temp.ReloadTime = officerConfig.WeaponReloadTime;
                            temp.RaiseTime = officerConfig.WeaponRaiseTime;
                            temp.HitChance_MaxRange = officerConfig.WeaponHitChanceMax;
                            temp.HitChance_MinRange = officerConfig.WeaponHitChanceMin;
                        }
                        AvatarWeapon temp2 = gun.TryCast<AvatarWeapon>();
                        if (temp2 != null)
                        {
                            temp2.CooldownDuration = officerConfig.WeaponFireRate;
                        }
                    }
#endif
                }
            }
            Log("Officer properties complete");
            yield break;
        }

    }

}
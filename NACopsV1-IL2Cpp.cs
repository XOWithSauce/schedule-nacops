using System.Collections;
using System.Reflection;
using HarmonyLib;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Managing.Object;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.AvatarFramework.Equipping;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Noise;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Handover;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.VoiceOver;
using Il2CppTMPro;
using Il2CppVLB;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;
using static Il2CppScheduleOne.AvatarFramework.AvatarSettings;

[assembly: MelonInfo(typeof(NACopsV1_IL2Cpp.NACops), NACopsV1_IL2Cpp.BuildInfo.Name, NACopsV1_IL2Cpp.BuildInfo.Version, NACopsV1_IL2Cpp.BuildInfo.Author, NACopsV1_IL2Cpp.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace NACopsV1_IL2Cpp
{
    public static class BuildInfo
    {
        public const string Name = "NACopsV1-IL2Cpp";
        public const string Description = "Crazyyyy cops";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.8.0";
        public const string DownloadLink = null;
    }

    #region Configuration loading
    public class ModConfig
    {
        public bool OverrideMovement = true;
        public bool OverrideCombatBeh = true;
        public bool OverrideBodySearch = true;
        public bool OverrideWeapon = true;
        public bool OverrideMaxHealth = true;
        public bool LethalCops = true;
        public bool NearbyCrazyCops = true;
        public bool CrazyCops = true;
        public bool PrivateInvestigator = true;
        public bool WeedInvestigator = true;
        public bool CorruptCops = true;
        public bool SnitchingSamples = true;
        public bool BuyBusts = true;
        public bool DocksRaids = true;
        public bool IncludeSpawned = false;
    }

    public class NAOfficerConfig
    {
        public float MovementRunSpeed = 9f;
        public float MovementWalkSpeed = 2.4f;
        public float CombatGiveUpRange = 40f;
        public float CombatGiveUpTime = 60f;
        public float CombatSearchTime = 60f;
        public float CombatMoveSpeed = 9f;
        public int CombatEndAfterHits = 40;
        public float OfficerMaxHealth = 175f;
        public int WeaponMagSize = 20;
        public float WeaponFireRate = 0.1f;
        public float WeaponMaxRange = 20f;
        public float WeaponReloadTime = 0.5f;
        public float WeaponRaiseTime = 0.2f;
        public float WeaponHitChanceMax = 0.3f;
        public float WeaponHitChanceMin = 0.8f;
    }

    public static class ConfigLoader
    {
        private static string modConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "config.json");
        private static string officerConfig = Path.Combine(MelonEnvironment.ModsDirectory, "NACops", "officer.json");
        #region Mod Configurations JSON
        public static ModConfig LoadModConfig()
        {
            ModConfig config;
            if (File.Exists(modConfig))
            {
                try
                {
                    string json = File.ReadAllText(modConfig);
                    // MelonLogger.Msg(json);
                    config = JsonConvert.DeserializeObject<ModConfig>(json);
                }
                catch (Exception ex)
                {
                    config = new ModConfig();
                    MelonLogger.Warning("Failed to read NACops config: " + ex);
                }
            }
            else
            {
                MelonLogger.Msg("File config.json does not exist at Mods/NACops/config.json");
                config = new ModConfig();
                Save(config);
            }
            return config;
        }
        public static void Save(ModConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config);
                Directory.CreateDirectory(Path.GetDirectoryName(modConfig));
                File.WriteAllText(modConfig, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops config: " + ex);
            }
        }
        #endregion
        #region Officers Configurations JSON
        public static NAOfficerConfig LoadOfficerConfig()
        {
            NAOfficerConfig config;
            if (File.Exists(officerConfig))
            {
                try
                {
                    string json = File.ReadAllText(officerConfig);
                    // MelonLogger.Msg(json);
                    config = JsonConvert.DeserializeObject<NAOfficerConfig>(json);
                }
                catch (Exception ex)
                {
                    config = new NAOfficerConfig();
                    MelonLogger.Warning("Failed to read NACops config: " + ex);
                }
            }
            else
            {
                MelonLogger.Msg("File config.json does not exist at Mods/NACops/officer.json");
                config = new NAOfficerConfig();
                Save(config);
            }
            return config;
        }
        public static void Save(NAOfficerConfig config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config);
                Directory.CreateDirectory(Path.GetDirectoryName(officerConfig));
                File.WriteAllText(officerConfig, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save NACops config: " + ex);
            }
        }
        #endregion

    }
    #endregion

    public class NACops : MelonMod
    {
        public static NACops Instance { get; set; }
        public static readonly HashSet<PoliceOfficer> allActiveOfficers = new();
        private static readonly object _officerLock = new();
        public static List<object> coros = new();
        public static HashSet<PoliceOfficer> currentDrugApprehender = new HashSet<PoliceOfficer>();
        public static HashSet<PoliceOfficer> currentSummoned = new HashSet<PoliceOfficer>();
        public static int currentPICount = 0;
        public static bool registered = false;
        public static bool lastSaveLoad = false;
        public static bool firstTimeLoad = false;
        public static ModConfig currentConfig;
        public static NAOfficerConfig currentOfficerConfig;
        public static NetworkManager netManager;
        private NetworkObject policeBase = new();
        public static HashSet<Pot> toBeDestroyed = new();
        public static int sessionPropertyHeat = 0;
        public static bool raidedDuringSession = false;

        #region Unity
        public override void OnApplicationStart()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                if (LoadManager.Instance != null && !registered && !lastSaveLoad && !firstTimeLoad)
                {
                    //MelonLogger.Msg("SceneInitAdd");
                    firstTimeLoad = true;
                    LoadManager.Instance.onLoadComplete.AddListener((UnityEngine.Events.UnityAction)OnLoadCompleteCb);
                }
            }
            
        }

        private void OnLoadCompleteCb()
        {
            //MelonLogger.Msg("Start State");
            if (registered) return;
            registered = true;
            currentConfig = ConfigLoader.LoadModConfig();
            currentOfficerConfig = ConfigLoader.LoadOfficerConfig();
            //MelonLogger.Msg(currentConfig.LethalCops);
            // Officers original populate and start coro
            lock (_officerLock)
            {
                allActiveOfficers.Clear();
                foreach (var o in UnityEngine.Object.FindObjectsOfType<PoliceOfficer>(true))
                {
                    allActiveOfficers.Add(o);
                }
            }
            if (currentConfig.IncludeSpawned)
                coros.Add(MelonCoroutines.Start(UpdateOfficerList()));

            netManager = UnityEngine.Object.FindObjectOfType<NetworkManager>(true);

            SetPoliceNPC();

            coros.Add(MelonCoroutines.Start(this.SetOfficers()));
            if (currentConfig.CrazyCops)
                coros.Add(MelonCoroutines.Start(this.CrazyCops()));
            if (currentConfig.NearbyCrazyCops)
                coros.Add(MelonCoroutines.Start(this.NearbyCrazyCop()));
            if (currentConfig.LethalCops)
                coros.Add(MelonCoroutines.Start(this.NearbyLethalCop()));
            if (currentConfig.PrivateInvestigator)
                coros.Add(MelonCoroutines.Start(this.PrivateInvestigator()));
            if (currentConfig.DocksRaids)
                coros.Add(MelonCoroutines.Start(this.RaidEvaluator()));

        }
        #endregion

        #region Raid Demo

        private IEnumerator RaidEvaluator()
        {
            yield return new WaitForSeconds(20f);
            for (; ; )
            {
                yield return new WaitForSeconds(120f);
                if (!registered || raidedDuringSession) yield break;
                Player player = UnityEngine.Object.FindObjectsOfType<Player>(true).FirstOrDefault();
                //MelonLogger.Msg(player.transform.position.ToString());

                if (player.CurrentProperty == null)
                    continue;
                if (player.CurrentProperty.PropertyName != "Docks Warehouse")
                    continue;
                Property docks = player.CurrentProperty;

                PoliceStation station = PoliceStation.GetClosestPoliceStation(player.transform.position);
                if (sessionPropertyHeat >= 20 && station.OccupantCount >= 2 && station.deployedVehicleCount < station.VehicleLimit)
                {
                    //MelonLogger.Msg("Session Raid Accepted");
                    //MelonLogger.Msg("Occp Count: " + station.OccupantCount);
                    //MelonLogger.Msg("Curr depl: " + station.deployedVehicleCount);
                    //MelonLogger.Msg("Veh Limit: " + station.VehicleLimit);
                    raidedDuringSession = true;
                    coros.Add(MelonCoroutines.Start(RaidRunner(player, docks)));
                }
                else
                {
                    //MelonLogger.Msg("Session Raid Denied");
                    //MelonLogger.Msg("Occp Count: " + station.OccupantCount);
                    //MelonLogger.Msg("Curr depl: " + station.deployedVehicleCount);
                    //MelonLogger.Msg("Veh Limit: " + station.VehicleLimit);
                }
            }
        }
        public static IEnumerator RaidNotification()
        {
            // Show in UI
            HUD hud = UnityEngine.Object.FindObjectOfType<HUD>();
            GameObject textObj = new GameObject("CrimeStatusText");
            textObj.transform.SetParent(hud.canvas.transform, false);
            textObj.transform.SetAsLastSibling();
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "The cops are preparing to raid Docks Warehouse!\n";
            textComponent.fontSize = 24;
            textComponent.color = Color.red;
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(600, 200);
            yield return new WaitForSeconds(6f);
            UnityEngine.Object.Destroy(textObj);
            yield break;
        }
        private IEnumerator RaidRunner(Player player, Property docks)
        {
            PoliceStation station = PoliceStation.GetClosestPoliceStation(player.transform.position);

            coros.Add(MelonCoroutines.Start(RaidNotification()));

            LandVehicle[] origVehs = UnityEngine.Object.FindObjectsOfType<LandVehicle>();
            player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Investigating);
            player.CrimeData.RecordLastKnownPosition(false);

            List<PoliceOfficer> listRd = new List<PoliceOfficer>();
            PoliceOfficer rdOffc = station.PullOfficer();
            LandVehicle veh = station.CreateVehicle();
            //MelonLogger.Msg("Spawned");
            rdOffc.AssignedVehicle = veh;
            rdOffc.EnterVehicle(null, veh);
            //MelonLogger.Msg("Send it");
            rdOffc.VehiclePursuitBehaviour.initialContactMade = true;
            rdOffc.BeginVehiclePursuit_Networked(player.NetworkObject, veh.NetworkObject, true);
            rdOffc.VehiclePursuitBehaviour.SetAggressiveDriving(true);
            rdOffc.VehiclePursuitBehaviour.DriveTo(new Vector3(-67.69f, -1.53f, -57.27f));
            rdOffc.VehiclePursuitBehaviour.Enable();

            yield return new WaitForSeconds(1f);
            //MelonLogger.Msg("Occp Count: " + station.OccupantCount);
            //MelonLogger.Msg("Curr depl: " + station.deployedVehicleCount);
            //MelonLogger.Msg("Veh Limit: " + station.VehicleLimit);
            //MelonLogger.Msg("Cinema----");
            float gOrig = PlayerSingleton<PlayerMovement>.Instance.gravityMultiplier;
            Quaternion rot = Quaternion.identity;

            //MelonLogger.Msg("Awaiting car to arrive");
            // Wait to be rendered active
            while (Vector3.Distance(veh.transform.position, player.transform.position) > 55f)
            {
                //MelonLogger.Msg("Car: " + veh.transform.position.ToString());
                if(rdOffc.AssignedVehicle == null)
                    rdOffc.AssignedVehicle = veh;

                if (!rdOffc.IsInVehicle)
                    rdOffc.EnterVehicle(null, veh);

                if (rdOffc.Behaviour.activeBehaviour != null && rdOffc.Behaviour.activeBehaviour.ToString().ToLower().Contains("schedule"))
                {
                    rdOffc.VehiclePursuitBehaviour.initialContactMade = true;
                    rdOffc.BeginVehiclePursuit_Networked(player.NetworkObject, veh.NetworkObject, true);
                    rdOffc.VehiclePursuitBehaviour.SetAggressiveDriving(true);
                    rdOffc.VehiclePursuitBehaviour.DriveTo(new Vector3(-67.69f, -1.53f, -57.27f));
                    rdOffc.VehiclePursuitBehaviour.Enable();
                }
                yield return new WaitForSeconds(0.3f);
            }

            //MelonLogger.Msg("Car Dispatch");
            HUD.Instance.StartCoroutine(HUD.Instance.FadeBlackOverlay(true, 0.3f));
            yield return new WaitForSeconds(0.3f);
            rot = player.transform.rotation;
            player.SetGravityMultiplier(0f);
            PlayerSingleton<PlayerMovement>.Instance.Controller.enabled = false;
            float durationPerSegment = 3f;
            float elapsed = 0f;
            bool fade1In = false;

            player.transform.position = veh.transform.position + Vector3.up * 40f;
            yield return new WaitForEndOfFrame();
            player.transform.LookAt(veh.transform.position + veh.transform.forward * 6f);
            while (elapsed < durationPerSegment)
            {
                yield return new WaitForEndOfFrame();

                elapsed += Time.deltaTime;
                Vector3 targetPos = veh.transform.position + Vector3.up * 40f;
                player.transform.position = Vector3.Lerp(player.transform.position, targetPos, Time.deltaTime * 2f);
                Vector3 lookTarget = veh.transform.position + veh.transform.forward * 6f;
                Quaternion targetRotation = Quaternion.LookRotation(lookTarget - player.transform.position);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, Time.deltaTime * 2f);
                if (!fade1In)
                {
                    fade1In = true;
                    HUD.Instance.StartCoroutine(HUD.Instance.FadeBlackOverlay(false, 0.2f));
                }
            }
            durationPerSegment = 3f;
            elapsed = 0f;
            bool fade2In = false;
            bool fade2 = false;
            yield return new WaitForSeconds(0.1f);
            while (elapsed < durationPerSegment)
            {
                elapsed += Time.deltaTime;

                Vector3 targetPos = veh.transform.position + Vector3.up * 5f - veh.transform.forward * 6f;
                player.transform.position = Vector3.Lerp(player.transform.position, targetPos, Time.deltaTime * 1.8f);
                Vector3 lookTarget = veh.transform.position + veh.transform.forward * 9f;
                Quaternion targetRot = Quaternion.LookRotation(lookTarget - player.transform.position);
                player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRot, Time.deltaTime * 1.8f);

                if (!fade2 && elapsed > 3f)
                {
                    fade2 = true;
                    HUD.Instance.StartCoroutine(HUD.Instance.FadeBlackOverlay(true, 0.2f));
                }
                if (!fade2In)
                {
                    fade2In = true;
                    HUD.Instance.StartCoroutine(HUD.Instance.FadeBlackOverlay(false, 0.2f));
                }

                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSeconds(1f);
            //MelonLogger.Msg("Summoning all");
            player.transform.rotation = rot;
            Vector3 pos = docks.SpawnPoint.position;

            int maxSummoned = 2;
            for (int i = 0; i < maxSummoned; i++)
            {
                // Create NPC to destroy Pots
                NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(NACopsV1_IL2Cpp.NACops.Instance.policeBase);
                NPC myNpc = copNet.gameObject.GetComponent<NPC>();
                NavMeshAgent nav = copNet.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (nav != null && nav.enabled == true || !nav.isStopped)
                {
                    nav.Stop();
                    nav.enabled = false;
                    UnityEngine.Object.Destroy(nav);
                }
                PhysicsDamageable damageable = copNet.GetComponent<PhysicsDamageable>();
                if (damageable != null)
                {
                    damageable.ForceMultiplier = 0f;
                }
                myNpc.ID = $"NACop_{Guid.NewGuid()}";
                myNpc.FirstName = $"NACop_{Guid.NewGuid()}";
                myNpc.LastName = "";
                myNpc.transform.parent = NPCManager.Instance.NPCContainer;
                NPCManager.NPCRegistry.Add(myNpc);
                yield return new WaitForSeconds(0.1f);

                netManager.ServerManager.Spawn(copNet);
                yield return new WaitForSeconds(0.3f);

                copNet.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);
                PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
                coros.Add(MelonCoroutines.Start(RaidAvatar(offc)));

                AvatarEquippable gun = offc.GunPrefab;
                if (gun != null && gun?.Cast<AvatarRangedWeapon>() is AvatarRangedWeapon rangedWeapon)
                    rangedWeapon.RaiseTime = 0.6f;
                currentSummoned.Add(offc);

                //MelonLogger.Msg("Summoned");
                coros.Add(MelonCoroutines.Start(DestroyPots(offc, docks, 2f)));
            }

            foreach (Vector3 sentryPos in sentryPositions)
            {
                // Create NPC With shooting
                NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(NACopsV1_IL2Cpp.NACops.Instance.policeBase);
                NPC myNpc = copNet.gameObject.GetComponent<NPC>();
                NavMeshAgent nav = copNet.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (nav != null)
                {
                    nav.Stop();
                    nav.enabled = false;
                    UnityEngine.Object.Destroy(nav);
                }
                PhysicsDamageable damageable = copNet.GetComponent<PhysicsDamageable>();
                if (damageable != null)
                {
                    damageable.ForceMultiplier = 0f;
                }
                myNpc.ID = $"NACop_{Guid.NewGuid()}";
                myNpc.FirstName = $"NACop_{Guid.NewGuid()}";
                myNpc.LastName = "";
                myNpc.transform.parent = NPCManager.Instance.NPCContainer;
                NPCManager.NPCRegistry.Add(myNpc);
                yield return new WaitForSeconds(0.1f);

                netManager.ServerManager.Spawn(copNet);
                yield return new WaitForSeconds(0.3f);

                copNet.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);

                PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
                offc.transform.position = sentryPos;
                coros.Add(MelonCoroutines.Start(RaidAvatar(offc)));
                
                AvatarEquippable gun = offc.GunPrefab;
                if (gun != null && gun?.Cast<AvatarRangedWeapon>() is AvatarRangedWeapon rangedWeapon)
                    rangedWeapon.RaiseTime = 0.6f;
                currentSummoned.Add(offc);

                //MelonLogger.Msg("Summoned");
                coros.Add(MelonCoroutines.Start(SentryShooting(offc, player, sentryPos)));
            }

            // End Cinema
            player.SetGravityMultiplier(gOrig);
            PlayerSingleton<PlayerMovement>.Instance.Controller.enabled = true;

            foreach (ConstantForce constantForce in player.ragdollForceComponents)
                constantForce.force = Vector3.zero;

            PlayerSingleton<PlayerMovement>.Instance.Teleport(new(pos.x, pos.y + 1f, pos.z));
            HUD.Instance.StartCoroutine(HUD.Instance.FadeBlackOverlay(false, 1f));
        }
        private IEnumerator SentryShooting(PoliceOfficer offc, Player player, Vector3 pos)
        {
            AvatarRangedWeapon wepRef = null;
            offc.transform.position = pos;
            yield return new WaitForSeconds(0.1f);
  
            offc.Behaviour.activeBehaviour = offc.PursuitBehaviour;
            offc.Behaviour.AddEnabledBehaviour(offc.PursuitBehaviour);
            offc.PursuitBehaviour.SendEnable();
            yield return new WaitForSeconds(0.1f);
            offc.Behaviour.enabled = true;
            offc.PursuitBehaviour.TargetPlayer = player;
            offc.Avatar.SetEquippable("Avatar/Equippables/M1911");
            //offc.PursuitBehaviour.rangedWeaponRoutine = offc.PursuitBehaviour.StartCoroutine(offc.PursuitBehaviour.RangedWeaponRoutine());
            yield return new WaitForSeconds(0.1f);
            if (offc.GunPrefab != null && offc.GunPrefab?.Cast<AvatarRangedWeapon>() is AvatarRangedWeapon rangedWeapon1)
            {
                offc.Movement.FacePoint(player.transform.position, 0.8f);
                offc.PursuitBehaviour.Weapon_Gun = rangedWeapon1;
                rangedWeapon1.Equip(offc.Avatar);
                rangedWeapon1.SetIsRaised(false);
                wepRef = offc.PursuitBehaviour.Weapon_Gun as AvatarRangedWeapon;
            }

            float elapsed = 0f;
            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 3f));
                elapsed += Time.deltaTime;
                if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled || elapsed > 40f)
                    break;

                offc.Movement.FacePoint(player.transform.position, lerpTime: 0.8f);

                if (UnityEngine.Random.Range(0f, 1f) > 0.85f)
                    if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                        offc.ChatterVO.Play(EVOLineType.Command);
                    else
                        offc.ChatterVO.Play(EVOLineType.Alerted);

                if (player.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.Lethal)
                    continue;

                if (offc.Avatar.CurrentEquippable == null)
                    offc.Avatar.SetEquippable("Avatar/Equippables/M1911");

                if (!wepRef.IsRaised)
                    wepRef.SetIsRaised(true);

                offc.Avatar.Anim.SetCrouched(false);
                offc.Movement.FacePoint(player.transform.position, lerpTime: 0.8f);
                yield return new WaitForSeconds(1f);
                if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled || elapsed > 40f)
                    break;
                if (!offc.Awareness.VisionCone.IsPlayerVisible(player))
                {
                    //MelonLogger.Msg("No VisionCone");
                    offc.Avatar.Anim.SetCrouched(true);
                    continue;
                }

                //MelonLogger.Msg("Wep Shoot");
                Vector3 vector = player.Avatar.CenterPoint;
                vector += UnityEngine.Random.insideUnitSphere * 4f;
                Vector3 normalized = (vector - wepRef.MuzzlePoint.position).normalized;
                RaycastHit raycastHit;
                if (Physics.Raycast(wepRef.MuzzlePoint.position, normalized, out raycastHit, 100f, NetworkSingleton<CombatManager>.Instance.RangedWeaponLayerMask))
                    vector = raycastHit.point;

                if (player.Health.CanTakeDamage && wepRef.IsTargetInLoS(offc.Behaviour.CombatBehaviour.Target) && UnityEngine.Random.Range(0f, 1f) > 0.4f)
                {
                    //MelonLogger.Msg("TakeDamage");
                    player.Health.TakeDamage(UnityEngine.Random.Range(8, 16), true, true);
                }
                NoiseUtility.EmitNoise(wepRef.MuzzlePoint.position, ENoiseType.Gunshot, 12f, offc.PursuitBehaviour.Npc.gameObject);
                offc.PursuitBehaviour.Npc.SendEquippableMessage_Networked_Vector(null, "Shoot", vector);

                yield return new WaitForSeconds(0.8f);
                if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled || elapsed > 40f)
                    break;
                offc.Avatar.Anim.SetCrouched(true);
                yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 6f));
            }
            if (!registered) yield break;

            if (offc is NPC npc)
                NPCManager.NPCRegistry.Remove(npc);
            GameObject.Destroy(offc.gameObject);

            yield return null;
        }
        private IEnumerator DestroyPots(PoliceOfficer offc, Property property, float durationPerSegment)
        {
            Pot[] pots = property.transform.GetComponentsInChildren<Pot>();
            int maxLen = pots.Length;
            
            Pot pot = null;
            do
            {
                pot = pots[UnityEngine.Random.Range(0, maxLen)];
            } 
            while (toBeDestroyed.Contains(pot));
            toBeDestroyed.Add(pot);
            Vector3 spawnPos = pot.accessPoints.FirstOrDefault().position;
            offc.transform.position = spawnPos;
            yield return new WaitForSeconds(3f);

            int maxDestroyed = 6;
            // Traverse Pots
            for (int i = 0; i < maxLen; i++)
            {
                if (i == maxDestroyed) break;

                float elapsed = 0f;
                Vector3 fixedDest = Vector3.zero;
                while (elapsed < durationPerSegment)
                {
                    yield return new WaitForEndOfFrame();
                    if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled)
                        break;

                    fixedDest = pot.accessPoints.FirstOrDefault().position;
                    float baseDist = Vector3.Distance(offc.transform.position, fixedDest);
                    foreach (Transform ap in pot.AccessPoints)
                    {
                        float dist = Vector3.Distance(offc.transform.position, ap.position);
                        if (dist < baseDist)
                        {
                            baseDist = dist;
                            fixedDest = ap.position;
                        }
                    }

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / durationPerSegment);
                    offc.transform.position = Vector3.Lerp(spawnPos, fixedDest, t);
                    Vector3 direction = pot.transform.position - offc.transform.position;
                    direction.y = 0;
                    offc.transform.rotation = Quaternion.LookRotation(direction);
                }

                elapsed = 0f;
                while (elapsed < durationPerSegment / 2f)
                {
                    yield return new WaitForEndOfFrame();
                    if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled)
                        break;

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / durationPerSegment);
                    offc.transform.position = Vector3.Lerp(fixedDest, pot.transform.position, t);
                    Vector3 direction = pot.transform.position - offc.transform.position;
                    direction.y = 0;
                    offc.transform.rotation = Quaternion.LookRotation(direction);
                }
                yield return new WaitForSeconds(2f);
                if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled)
                    break;
                offc.Avatar.Anim.SetCrouched(true);

                if (pot.Configuration is PotConfiguration cnf)
                {
                    if (cnf.AssignedBotanist != null)
                        cnf.AssignedBotanist.SetNPC(null, false);
                }
                yield return new WaitForSeconds(2f);
                if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled)
                    break;
                if (pot.TryGetComponent<BuildableItem>(out BuildableItem bi))
                    bi.DestroyItem();
                toBeDestroyed.Remove(pot);

                //MelonLogger.Msg("Object permanently destroyed.");
                yield return new WaitForSeconds(2f);
                if (!registered || offc.Health.IsKnockedOut || offc.Health.IsDead || !offc.IsConscious || offc.Avatar.Ragdolled)
                    break;
                offc.Avatar.Anim.SetCrouched(false);

                pots = property.transform.GetComponentsInChildren<Pot>();
                float minDistance = 5f;
                if (pots.Length < 2) break;
                pot = null;
                foreach (Pot poti in pots)
                {
                    float dist = Vector3.Distance(offc.transform.position, poti.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        pot = poti;
                    }
                }
                if (pot == null) break;
                toBeDestroyed.Add(pot);
                spawnPos = offc.transform.position;
            }
            yield return new WaitForSeconds(2f);
            if (!registered) yield break;
            if (offc is NPC npc)
                NPCManager.NPCRegistry.Remove(npc);
            GameObject.Destroy(offc.gameObject);
            yield return null;
        }
        private IEnumerator RaidAvatar(PoliceOfficer offc)
        {
            #region Raid Avatar
            var originalBodySettings = offc.Avatar.CurrentSettings.BodyLayerSettings;
            Il2CppSystem.Collections.Generic.List<LayerSetting> bodySettings = new();
            foreach (var layer in originalBodySettings)
            {
                bodySettings.Add(new LayerSetting
                {
                    layerPath = layer.layerPath,
                    layerTint = layer.layerTint
                });
            }

            var originalAccessorySettings = offc.Avatar.CurrentSettings.AccessorySettings;
            Il2CppSystem.Collections.Generic.List<AccessorySetting> accessorySettings = new();
            foreach (var acc in originalAccessorySettings)
            {
                accessorySettings.Add(new AccessorySetting
                {
                    path = acc.path,
                    color = acc.color
                });
            }

            for (int i = 0; i < bodySettings.Count; i++)
            {
                var layer = bodySettings[i];
                layer.layerPath = "";
                layer.layerTint = Color.white;
                bodySettings[i] = layer;
            }

            for (int i = 0; i < accessorySettings.Count; i++)
            {
                var acc = accessorySettings[i];
                acc.path = "";
                acc.color = Color.white;
                accessorySettings[i] = acc;
            }

            var jeans = bodySettings[2];
            jeans.layerPath = "Avatar/Layers/Bottom/Jeans";
            jeans.layerTint = new Color(0.396f, 0.396f, 0.396f);
            bodySettings[2] = jeans;
            var shirt = bodySettings[3];
            shirt.layerPath = "Avatar/Layers/Top/RolledButtonUp";
            shirt.layerTint = Color.black;
            bodySettings[3] = shirt;

            var cap = accessorySettings[0];
            cap.path = "Avatar/Accessories/Head/Cap/Cap";
            cap.color = Color.black;
            accessorySettings[0] = cap;
            var sneakers = accessorySettings[1];
            sneakers.path = "Avatar/Accessories/Feet/Sneakers/Sneakers";
            sneakers.color = Color.black;
            accessorySettings[1] = sneakers;

            offc.Avatar.CurrentSettings.BodyLayerSettings = bodySettings;
            offc.Avatar.CurrentSettings.AccessorySettings = accessorySettings;
            offc.Avatar.ApplyBodyLayerSettings(offc.Avatar.CurrentSettings);
            offc.Avatar.ApplyAccessorySettings(offc.Avatar.CurrentSettings);
            offc.Avatar.SetRagdollPhysicsEnabled(false);
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider> rcs = offc.Avatar.RagdollColliders;
            for (int i = 0; i < rcs.Length; i++)
            {
                rcs[i].enabled = false;
            }


            #endregion
            yield return null;
        }

        #endregion

        #region Harmony Patches for exiting coros
        static void ExitPreTask()
        {
            //MelonLogger.Msg("Pre-Exit Task");
            registered = false;
            foreach (object coro in coros)
            {
                if (coro != null)
                    MelonCoroutines.Stop(coro);
            }
            lock (_officerLock)
                allActiveOfficers.Clear();
            coros.Clear();
            currentSummoned.Clear();
            currentDrugApprehender.Clear();
            toBeDestroyed.Clear();
            raidedDuringSession = false;
            sessionPropertyHeat = 0;
        }

        [HarmonyPatch(typeof(LoadManager), "ExitToMenu")]
        public static class LoadManager_ExitToMenu_Patch
        {
            public static bool Prefix(SaveInfo autoLoadSave = null, Il2CppScheduleOne.UI.MainMenu.MainMenuPopup.Data mainMenuPopup = null, bool preventLeaveLobby = false)
            {
                //MelonLogger.Msg("Exit Menu");
                lastSaveLoad = false;
                ExitPreTask();
                return true;
            }
        }

        [HarmonyPatch(typeof(DeathScreen), "LoadSaveClicked")]
        public static class DeathScreen_LoadSaveClicked_Patch
        {
            public static bool Prefix(DeathScreen __instance)
            {
                //MelonLogger.Msg("LoadLastSave");
                lastSaveLoad = true;
                ExitPreTask();
                return true;
            }
        }
        #endregion

        #region Harmony Consume Product Player

        [HarmonyPatch(typeof(Player), "ConsumeProduct")]
        public static class Player_ConsumeProduct_Patch
        {
            public static bool Prefix(Player __instance, ProductItemInstance product)
            {
                //MelonLogger.Msg("ConsumePrefix");
                coros.Add(MelonCoroutines.Start(DrugConsumedCoro(__instance, product)));
                return true;
            }
        }

        private static IEnumerator DrugConsumedCoro(Player player, ProductItemInstance product)
        {
            if (!currentConfig.WeedInvestigator) yield break;
            dynamic inst = null;
            try
            {
                inst = product.Cast<WeedInstance>();
            } 
            catch (InvalidCastException ex)
            {
                //MelonLogger.Error("NACops IL2Cpp failed to cast item instance: " + ex);
                yield break;
            }

            if (inst != null && inst is WeedInstance)
            {
                //MelonLogger.Msg("Productinstance is weed");
                yield return new WaitForSeconds(1f);
                if (!registered || currentDrugApprehender.Count >= 1) yield break;

                PoliceOfficer[] officersSnapshot;
                lock (_officerLock)
                {
                    //MelonLogger.Msg("Drug Consumed Lock");
                    officersSnapshot = allActiveOfficers.Where(o => o != null && o.gameObject != null).ToArray();
                }

                PoliceOfficer noticeOfficer = null;
                float smallestDistance = 35f;
                bool direct = false;

                //MelonLogger.Msg("Total Officers: " + officers.Length);
                for (int i = 0; i < officersSnapshot.Length; i++)
                {
                    yield return new WaitForSeconds(0.3f);
                    PoliceOfficer offc = officersSnapshot[i];
                    if (currentDrugApprehender.Contains(offc) || currentSummoned.Contains(offc)) continue;
                    if (Vector3.Distance(offc.transform.position, player.transform.position) > 50f) continue;
                    if (offc.Awareness.VisionCone.IsPlayerVisible(player))
                    {
                        offc.BeginBodySearch_Networked(player.NetworkObject);
                        coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player)));
                        direct = true;
                        break;
                    } else
                    {
                        //MelonLogger.Msg("ParseCandidate");
                        float distance = Vector3.Distance(offc.transform.position, player.transform.position);
                        if (distance < smallestDistance && offc.Movement.CanMove() && !offc.IsInVehicle && !offc.isInBuilding)
                        {
                            smallestDistance = distance;
                            noticeOfficer = offc;
                        }
                    }
                }

                if (noticeOfficer == null || direct) yield break;

                currentDrugApprehender.Add(noticeOfficer);

                yield return new WaitForSeconds(4f);
                if (!registered) yield break;

                coros.Add(MelonCoroutines.Start(ApprehenderOfficerClear(noticeOfficer)));

                bool apprehending = false;
                noticeOfficer.Movement.FacePoint(player.transform.position, lerpTime: 0.2f);
                yield return new WaitForSeconds(0.2f);
                if (noticeOfficer.Awareness.VisionCone.IsPointWithinSight(player.transform.position))
                {
                    //MelonLogger.Msg("Point within immediate sight apprehend drug user");
                    noticeOfficer.BeginBodySearch_Networked(player.NetworkObject);
                    coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player)));
                    apprehending = true;
                }

                if (noticeOfficer != null && !apprehending)
                {
                    for (int i = 0; i <= 15; i++)
                    {
                        if (!registered) yield break;

                        // End foot search early if 5% random roll hits
                        if (i > 6 && UnityEngine.Random.Range(1f, 0f) > 0.95f)
                            break;

                        //MelonLogger.Msg($"Officer searching for drug user for {i} times");
                        noticeOfficer.Movement.FacePoint(player.transform.position, lerpTime: 0.3f);
                        yield return new WaitForSeconds(0.5f);
                        if (noticeOfficer.Awareness.VisionCone.IsPlayerVisible(player))
                        {
                            //MelonLogger.Msg("PlayerInVision, apprehend drug user");
                            noticeOfficer.BeginBodySearch_Networked(player.NetworkObject);
                            if (UnityEngine.Random.Range(1f, 0f) > 0.8f)
                                coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 2, player)));
                            if (UnityEngine.Random.Range(1f, 0f) > 0.8f)
                                coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 1, player)));
                            break;
                        }

                        yield return new WaitForSeconds(0.5f);
                        noticeOfficer.Movement.GetClosestReachablePoint(player.transform.position, out Vector3 pos);
                        if (pos != Vector3.zero && noticeOfficer.Movement.CanMove() && noticeOfficer.Movement.CanGetTo(pos))
                            noticeOfficer.Movement.SetDestination(pos);
                        else
                            continue;
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
            yield return null;
        }

        private static IEnumerator ApprehenderOfficerClear(PoliceOfficer offc)
        {
            if (offc == null) yield break;
            yield return new WaitForSeconds(30f);
            if (currentDrugApprehender.Contains(offc))
                currentDrugApprehender.Remove(offc);
        }

        #endregion

        #region Harmony Snitch Samples
        [HarmonyPatch(typeof(Customer), "SampleOffered")]
        public static class Customer_SampleOffered_Patch
        {
            public static bool Prefix(Customer __instance)
            {
                //MelonLogger.Msg("SampleConsumed Customer Postfix");
                coros.Add(MelonCoroutines.Start(PreSampleOffered(__instance)));
                return true;
            }
        }

        private static IEnumerator PreSampleOffered(Customer customer)
        {
            if (!currentConfig.SnitchingSamples) yield break;
            yield return new WaitForSeconds(10f);
            if (!registered) yield break;

            Player closestPlayer = Player.GetClosestPlayer(customer.transform.position, out float distance);
            if (closestPlayer == null) yield break;
            (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.SnitchProbability, TimeManager.Instance.ElapsedDays);
            if (UnityEngine.Random.Range(min, max) < 0.8f) yield break;
            //MelonLogger.Msg("Snitching Samples");
            coros.Add(MelonCoroutines.Start(LateInvestigation(closestPlayer)));
            coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 1, player: closestPlayer)));
        }
        #endregion

        #region Harmony Customer Buy Bust
        [HarmonyPatch(typeof(Customer), "ProcessHandover")]
        public static class Customer_ProcessHandover_Patch
        {
            public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
            {
                //MelonLogger.Msg("ProcessHandover Customer Postfix");
                coros.Add(MelonCoroutines.Start(PreProcessHandover(__instance, handoverByPlayer)));
                return true;
            }
        }

        public static IEnumerator PreProcessHandover(Customer __instance, bool handoverByPlayer)
        {
            if (!handoverByPlayer) yield break;
            if (currentConfig.BuyBusts)
                coros.Add(MelonCoroutines.Start(SummonBustCop(__instance)));
            yield return null;
        }
        public static IEnumerator SummonBustCop(Customer customer)
        {
            int relation = Mathf.RoundToInt(customer.NPC.RelationData.RelationDelta * 10f);
            (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.BuyBustProbability, relation);
            if (UnityEngine.Random.Range(min, max) < 0.5) yield break;
            NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(NACopsV1_IL2Cpp.NACops.Instance.policeBase);
            NPC myNpc = copNet.gameObject.GetComponent<NPC>();
            NavMeshAgent nav = copNet.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nav != null && !nav.enabled || nav.isStopped)
            {
                nav.enabled = true;
                nav.isStopped = false;
            }

            myNpc.ID = $"NACop_{Guid.NewGuid()}";
            myNpc.FirstName = $"NACop_{Guid.NewGuid()}";
            myNpc.LastName = "";
            myNpc.transform.parent = NPCManager.Instance.NPCContainer;
            NPCManager.NPCRegistry.Add(myNpc);
            yield return new WaitForSeconds(0.1f);

            netManager.ServerManager.Spawn(copNet);
            yield return new WaitForSeconds(0.3f);

            copNet.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);

            PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
            currentSummoned.Add(offc);
            coros.Add(MelonCoroutines.Start(NACopsV1_IL2Cpp.NACops.Instance.BustCopAvatar(offc)));
            Player target = null;
            Vector3 spawnPos = customer.transform.position + customer.transform.forward * 3f;
            bool flag = offc.Movement.GetClosestReachablePoint(spawnPos, out Vector3 closest);
            if (flag && closest != Vector3.zero)
            {
                coros.Add(MelonCoroutines.Start(SetTaser(offc)));
                yield return new WaitForSeconds(0.1f);
                offc.Movement.Warp(closest);
                yield return new WaitForSeconds(1f);
                offc.ChatterVO.Play(EVOLineType.Command);
                offc.Movement.FacePoint(customer.transform.position);
                yield return new WaitForSeconds(0.5f);
                target = Player.GetClosestPlayer(closest, out float distance);
                target.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.NonLethal);
                offc.Movement.SetAgentType(NPCMovement.EAgentType.IgnoreCosts);
                offc.BeginFootPursuit_Networked(target.NetworkObject, false);
                offc.PursuitBehaviour.SendEnable();
                target.CrimeData.AddCrime(new AttemptingToSell(), 10);
            }
            else
            {
                //MelonLogger.Msg("Failed to Get closest reachable position for drug bust");
                coros.Add(MelonCoroutines.Start(DisposeSummoned(myNpc, offc, true, target)));
            }
            coros.Add(MelonCoroutines.Start(DisposeSummoned(myNpc, offc, false, target)));
            yield return null;
        }
        private IEnumerator BustCopAvatar(PoliceOfficer offc)
        {
            #region Bust cop Avatar
            var originalBodySettings = offc.Avatar.CurrentSettings.BodyLayerSettings;
            Il2CppSystem.Collections.Generic.List<LayerSetting> bodySettings = new();
            foreach (var layer in originalBodySettings)
            {
                bodySettings.Add(new LayerSetting
                {
                    layerPath = layer.layerPath,
                    layerTint = layer.layerTint
                });
            }

            var originalAccessorySettings = offc.Avatar.CurrentSettings.AccessorySettings;
            Il2CppSystem.Collections.Generic.List<AccessorySetting> accessorySettings = new();
            foreach (var acc in originalAccessorySettings)
            {
                accessorySettings.Add(new AccessorySetting
                {
                    path = acc.path,
                    color = acc.color
                });
            }

            for (int i = 0; i < bodySettings.Count; i++)
            {
                var layer = bodySettings[i];
                layer.layerPath = "";
                layer.layerTint = Color.white;
                bodySettings[i] = layer;
            }

            for (int i = 0; i < accessorySettings.Count; i++)
            {
                var acc = accessorySettings[i];
                acc.path = "";
                acc.color = Color.white;
                accessorySettings[i] = acc;
            }

            var jeans = bodySettings[2];
            jeans.layerPath = "Avatar/Layers/Bottom/Jeans";
            jeans.layerTint = Color.black;
            bodySettings[2] = jeans;
            var shirt = bodySettings[3];
            shirt.layerPath = "Avatar/Layers/Top/RolledButtonUp";
            shirt.layerTint = Color.black;
            bodySettings[3] = shirt;

            var cap = accessorySettings[0];
            cap.path = "Avatar/Accessories/Head/Cap/Cap";
            cap.color = Color.red;
            accessorySettings[0] = cap;
            var sneakers = accessorySettings[1];
            sneakers.path = "Avatar/Accessories/Feet/Sneakers/Sneakers";
            sneakers.color = Color.white;
            accessorySettings[1] = sneakers;

            offc.Avatar.CurrentSettings.BodyLayerSettings = bodySettings;
            offc.Avatar.CurrentSettings.AccessorySettings = accessorySettings;
            offc.Avatar.ApplyBodyLayerSettings(offc.Avatar.CurrentSettings);
            offc.Avatar.ApplyAccessorySettings(offc.Avatar.CurrentSettings);
            #endregion
            yield return null;
        }
        public static IEnumerator SetTaser(PoliceOfficer offc)
        {
            var taser = offc.TaserPrefab;
            if (taser != null && taser?.Cast<AvatarRangedWeapon>() is AvatarRangedWeapon rangedWeapon)
            {
                rangedWeapon.CanShootWhileMoving = true;
                rangedWeapon.MagazineSize = 20;
                rangedWeapon.MaxFireRate = 0.3f;
                rangedWeapon.MaxUseRange = 24f;
                rangedWeapon.ReloadTime = 0.2f;
                rangedWeapon.RaiseTime = 0.1f;
                rangedWeapon.HitChance_MaxRange = 0.6f;
                rangedWeapon.HitChance_MinRange = 0.9f;
            }
            yield return null;
        }
        public static IEnumerator DisposeSummoned(NPC npc, PoliceOfficer offc, bool instant, Player target)
        {
            yield return new WaitForSeconds(1f);
            int lifeTime = 0;
            int maxTime = 20;
            if (!instant && target != null && npc != null)
            {
                while (lifeTime <= maxTime || target.IsArrested || npc.Health.IsDead || npc.Health.IsKnockedOut)
                {
                    lifeTime++;
                    yield return new WaitForSeconds(1f);
                }
            }
            try
            {
                if (currentSummoned.Contains(offc))
                    currentSummoned.Remove(offc);
                if (npc != null && NPCManager.NPCRegistry.Contains(npc))
                    NPCManager.NPCRegistry.Remove(npc);
                if (npc != null && npc.gameObject != null)
                    UnityEngine.Object.Destroy(npc.gameObject);
            } catch (Exception ex)
            {
                MelonLogger.Error(ex);
            }
        }
        #endregion

        #region Harmony Bodysearch
        [HarmonyPatch(typeof(BodySearchScreen), "Open")]
        public static class BodySearch_Open_Patch
        {
            public static bool Prefix(BodySearchScreen __instance, NPC _searcher, ref float searchTime)
            {
                //MelonLogger.Msg("BodySearchOpen");
                if (currentConfig.OverrideBodySearch)
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
                if (!__instance.IsOpen || !currentConfig.OverrideBodySearch)
                    return;

                if (!isBoostingRandomly)
                {
                    if (UnityEngine.Random.Range(0f, 1f) > 0.97f)
                    {
                        isBoostingRandomly = true;
                        timeUntilNextRandomChange = UnityEngine.Random.Range(0.5f, 2.5f);
                        randomSpeedTarget = UnityEngine.Random.Range(2.5f, 3.5f);
                        //MelonLogger.Msg($"RandomBoost ON -> {randomSpeedTarget}");
                    }
                }

                if (isBoostingRandomly)
                {
                    timeUntilNextRandomChange -= Time.deltaTime;
                    if (timeUntilNextRandomChange <= 0f)
                    {
                        isBoostingRandomly = false;
                        //MelonLogger.Msg("RandomBoost off");
                    }
                    __instance.speedBoost = Mathf.MoveTowards(__instance.speedBoost, randomSpeedTarget, Time.deltaTime * 6f);
                }

                return;
            }
        }

        #endregion

        #region Base Coroutines
        private IEnumerator NearbyLethalCop()
        {
            //MelonLogger.Msg("Nearby Lethal Cop Enabled");
            for (; ; )
            {
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.LethalCopFreq, TimeManager.Instance.ElapsedDays);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                if (!registered) yield break;

                //MelonLogger.Msg("Nearby Lethal Cop Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                (float minRang, float maxRang) = ThresholdUtils.Evaluate(ThresholdMappings.LethalCopRange, (int)MoneyManager.Instance.LifetimeEarnings);
                float minDistance = UnityEngine.Random.Range(minRang, maxRang);

                PoliceOfficer[] officersSnapshot;
                lock (_officerLock)
                {
                    //MelonLogger.Msg("Lethal Cop Lock");
                    officersSnapshot = allActiveOfficers.Where(o => o != null && o.gameObject != null).ToArray();
                }

                foreach (Player player in players)
                {
                    foreach (PoliceOfficer officer in officersSnapshot)
                    {
                        yield return new WaitForSeconds(0.01f);
                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);

                        if (distance < minDistance && !currentSummoned.Contains(officer) && !currentDrugApprehender.Contains(officer) && !IsStationNearby(player.transform.position) && !player.CrimeData.BodySearchPending && !officer.IsInVehicle && officer.Behaviour.activeBehaviour != officer.CheckpointBehaviour && !officer.isInBuilding)
                        {
                            officer.Movement.FacePoint(player.transform.position, lerpTime: 0.2f);
                            yield return new WaitForSeconds(0.3f);
                            if (officer.Awareness.VisionCone.IsPlayerVisible(player))
                            {
                                player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Lethal);
                                officer.BeginFootPursuit_Networked(player.NetworkObject, false);
                            }
                            break;
                        }
                    }
                }
            }
        }
        private IEnumerator NearbyCrazyCop()
        {
            //MelonLogger.Msg("Nearby Crazy Cop Enabled");
            for (; ; )
            {
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.NearbyCrazThres, TimeManager.Instance.ElapsedDays);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                if (!registered) yield break;
                //MelonLogger.Msg("Nearby Crazy Cop Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                (float minRang, float maxRang) = ThresholdUtils.Evaluate(ThresholdMappings.NearbyCrazRange, (int)MoneyManager.Instance.LifetimeEarnings);
                float minDistance = UnityEngine.Random.Range(minRang, maxRang);

                float cacheWalkSpeed = 0f;

                PoliceOfficer[] officersSnapshot;
                lock (_officerLock)
                {
                    //MelonLogger.Msg("Nearby Crazy Cop Lock");
                    officersSnapshot = allActiveOfficers.Where(o => o != null && o.gameObject != null).ToArray();
                }

                foreach (Player player in players)
                {
                    if (player.CurrentProperty != null)
                        continue;

                    foreach (PoliceOfficer officer in officersSnapshot)
                    {
                        if (officer.IsInVehicle)
                            continue;

                        if (officer.Behaviour.activeBehaviour && officer.Behaviour.activeBehaviour is VehiclePatrolBehaviour)
                            continue;

                        if (officer.Behaviour.activeBehaviour && officer.Behaviour.activeBehaviour is VehiclePursuitBehaviour)
                            continue;

                        yield return new WaitForSeconds(0.01f);
                        float distance = Vector3.Distance(officer.transform.position, player.transform.position);
                        if (distance < minDistance && !currentSummoned.Contains(officer) && !currentDrugApprehender.Contains(officer) && !IsStationNearby(player.transform.position) && !officer.IsInVehicle && !officer.isInBuilding)
                        {
                            officer.ChatterVO.Play(EVOLineType.PoliceChatter);
                            officer.Movement.FacePoint(player.transform.position, lerpTime: 0.6f);
                            yield return new WaitForSeconds(0.7f);
                            if (officer.Awareness.VisionCone.IsPlayerVisible(player) && !player.CrimeData.BodySearchPending)
                            {
                                officer.BeginBodySearch_Networked(player.NetworkObject);
                                if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
                                    coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 2, player: player)));
                                officer.Movement.WalkSpeed = cacheWalkSpeed;
                                break;
                            }

                            //MelonLogger.Msg("Nearby Crazy Cop Run");
                            cacheWalkSpeed = officer.Movement.WalkSpeed;

                            officer.Movement.GetClosestReachablePoint(player.transform.position, out Vector3 pos);
                            if (pos != Vector3.zero && officer.Movement.CanMove() && officer.Movement.CanGetTo(pos))
                            {
                                officer.Movement.WalkSpeed = 5f;
                                officer.Movement.SetDestination(pos);
                            }

                            yield return new WaitForSeconds(4f);
                            if (!registered || officer == null || officer.gameObject == null) yield break;

                            officer.ChatterVO.Play(EVOLineType.PoliceChatter);
                            officer.Movement.FacePoint(player.transform.position, lerpTime: 0.6f);
                            yield return new WaitForSeconds(0.6f);
                            if (officer.Awareness.VisionCone.IsPlayerVisible(player) && !player.CrimeData.BodySearchPending)
                            {
                                officer.BeginBodySearch_Networked(player.NetworkObject);
                                if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
                                    coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 2, player: player)));
                                officer.Movement.WalkSpeed = cacheWalkSpeed;
                                break;
                            }

                            officer.ChatterVO.Play(EVOLineType.PoliceChatter);
                            officer.Movement.WalkSpeed = cacheWalkSpeed;

                            break;
                        }
                        else { continue; }
                    }
                }
            }

        }
        private IEnumerator PrivateInvestigator()
        {
            //MelonLogger.Msg("Private Investigator Enabled");
            float maxTime = 120f;
            
            for (; ; )
            {
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.PIThres, (int)MoneyManager.Instance.LifetimeEarnings);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                if (!registered) yield break;
                //MelonLogger.Msg("PI Evaluate");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                if (players.Length == 0) 
                    continue;

                EDay currentDay = TimeManager.Instance.CurrentDay;
                if (currentDay.ToString().Contains("Saturday") || currentDay.ToString().Contains("Sunday"))
                    continue;

                if (currentPICount >= 1)
                    continue;

                currentPICount += 1;
                //MelonLogger.Msg("PI Proceed");

                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];

                NetworkObject copNet = UnityEngine.Object.Instantiate<NetworkObject>(policeBase);
                NPC myNpc = copNet.gameObject.GetComponent<NPC>();
                NavMeshAgent nav = copNet.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (nav != null && nav.enabled == false) nav.enabled = true;
                else
                {
                    nav = copNet.GetOrAddComponent<UnityEngine.AI.NavMeshAgent>();
                    nav.enabled = true;
                    nav.isStopped = false;
                }

                myNpc.ID = $"NACop_{Guid.NewGuid()}";
                myNpc.FirstName = $"NACop_{Guid.NewGuid()}";
                myNpc.LastName = "";
                myNpc.transform.parent = NPCManager.Instance.NPCContainer;

                NPCManager.NPCRegistry.Add(myNpc);
                yield return new WaitForSeconds(0.1f);
                netManager.ServerManager.Spawn(copNet);
                yield return new WaitForSeconds(0.3f);

                copNet.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.1f);

                PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
                currentSummoned.Add(offc);
                offc.FootPatrolBehaviour.SendEnable();
                offc.Movement.WalkSpeed = 5f;
                offc.Movement.RunSpeed = 8f;

                coros.Add(MelonCoroutines.Start(PIAvatar(offc)));

                if (!raidedDuringSession)
                {
                    if (sessionPropertyHeat >= 12)
                        offc.Avatar.Effects.SetEyeLightEmission(1f, Color.red);
                    else if (sessionPropertyHeat >= 6)
                        offc.Avatar.Effects.SetEyeLightEmission(1f, Color.yellow);
                }

                Vector3 warpInit = Vector3.zero;
                int maxWarpAttempts = 30;
                for (int i = 0; i < maxWarpAttempts; i++)
                {
                    float xInitOffset = UnityEngine.Random.Range(8f, 30f);
                    float zInitOffset = UnityEngine.Random.Range(8f, 30f);
                    xInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                    zInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                    Vector3 targetWarpPosition = randomPlayer.transform.position + new Vector3(xInitOffset, 0f, zInitOffset);
                    offc.Movement.GetClosestReachablePoint(targetWarpPosition, out warpInit);
                    if (warpInit != Vector3.zero)
                    {
                        yield return new WaitForSeconds(0.1f);
                        //MelonLogger.Msg("Warp to " + warpInit.ToString());
                        offc.Movement.Warp(warpInit);
                        //MelonLogger.Msg("Now at: " + offc.transform.position.ToString());
                        //MelonLogger.Msg("Distance: " + Vector3.Distance(offc.transform.position, randomPlayer.transform.position));
                        offc.Movement.SetAgentType(NPCMovement.EAgentType.IgnoreCosts);
                        break;
                    }
                    yield return new WaitForSeconds(0.1f);
                }
 
                float elapsed = 0f;
                int investigationDelta = 0;
                int proximityDelta = 0;
                int sightedAmount = 0;
                float maxWarpCd = 30f;
                float lastWarp = 0f;
                for (; ; )
                {
                    float distance = Vector3.Distance(offc.transform.position, randomPlayer.transform.position);
                    //MelonLogger.Msg(offc.Behaviour.activeBehaviour?.ToString());

                    if (offc.Behaviour.activeBehaviour != null && offc.Behaviour.activeBehaviour.ToString().ToLower().Contains("schedule"))
                        offc.FootPatrolBehaviour.SendEnable();

                    if (!offc.Movement.CanMove() || elapsed >= maxTime || randomPlayer.CrimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
                        break;

                    lastWarp += Time.deltaTime;

                    if (UnityEngine.Random.Range(0f, 1f) >= 0.98f)
                        offc.PlayVO(EVOLineType.PoliceChatter);

                    if (TimeManager.Instance.CurrentTime > 2100 || TimeManager.Instance.CurrentTime < 0500) // During curfew
                    {
                        if (offc.Awareness.VisionCone.VisionEnabled)
                            offc.Awareness.VisionCone.VisionEnabled = false;
                        // Based on prog, roll random chance for enable
                        (float minRang, float maxRang) = ThresholdUtils.Evaluate(ThresholdMappings.PICurfewAttn, TimeManager.Instance.ElapsedDays);
                        if (UnityEngine.Random.Range(minRang, maxRang) < 0.5f)
                            offc.Awareness.VisionCone.VisionEnabled = true;
                    }
                    else if (!offc.Awareness.VisionCone.VisionEnabled)
                        offc.Awareness.VisionCone.VisionEnabled = true;

                    if (offc.Awareness.VisionCone.VisionEnabled && offc.Awareness.VisionCone.IsPlayerVisible(randomPlayer))
                        sightedAmount += 1;

                    if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 90f && distance < 140)
                    {
                        //MelonLogger.Msg("PI Warp - dist " + distance);
                        if (lastWarp < maxWarpCd) continue;

                        coros.Add(MelonCoroutines.Start(AttemptWarp(offc, randomPlayer.transform)));
                        lastWarp = 0f;
                    }
                    else if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance >= 25f && distance < 140)
                    {
                        //MelonLogger.Msg("PI Traverse - dist " + distance);

                        if (offc.Movement.IsPaused)
                            offc.Movement.ResumeMovement();
                        float xOffset = UnityEngine.Random.Range(6f, 24f);
                        float zOffset = UnityEngine.Random.Range(6f, 24f);
                        xOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                        zOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                        
                        Vector3 targetPosition = randomPlayer.transform.position + new Vector3(xOffset, 0f, zOffset);
                        offc.Movement.GetClosestReachablePoint(targetPosition, out Vector3 pos);
                        if (pos != Vector3.zero && Vector3.Distance(pos, randomPlayer.transform.position) < distance)
                            offc.Behaviour.activeBehaviour.SetDestination(pos);
                    }
                    else if (offc.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 100f) && distance <= 25f)
                    {
                        //MelonLogger.Msg("PI Monitoring");
                        if (!offc.Movement.IsPaused)
                            offc.Movement.PauseMovement();
                        offc.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.9f);

                        if (UnityEngine.Random.Range(0f, 1f) > 0.95f)
                            coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 1, player: randomPlayer)));

                        proximityDelta += 1;
                        if (randomPlayer.CurrentProperty != null && UnityEngine.Random.Range(0f, 1f) > 0.1f && randomPlayer.CurrentProperty.PropertyName == "Docks Warehouse")
                            investigationDelta += 1;
                    }
                    else
                    {
                        //MelonLogger.Msg("Exit condition");
                        break;
                    }

                    yield return new WaitForSeconds(5f);
                    if (!registered) yield break;
                    elapsed += 5f;
                }

                // Evaluate result, if 40 sec spent observing in docks, and player still in property add 6-8
                if (investigationDelta >= 8 && randomPlayer.CurrentProperty != null && randomPlayer.CurrentProperty.PropertyName == "Docks Warehouse" && sightedAmount >= 1)
                    sessionPropertyHeat += UnityEngine.Random.Range(6, 9);
                // else if player spent major amount of time inside warehouse, not in any property, and PI has sighted twice outside, add 1-3
                else if (investigationDelta >= 12 && randomPlayer.CurrentProperty == null && sightedAmount > 2)
                    sessionPropertyHeat += UnityEngine.Random.Range(1, 4);
                // else if the property heat is high enough, PI was alive for atleast 1min, player was nearby atleast 4 times and was sighted atleast once, reduce 1-3
                else if (sessionPropertyHeat > 3 && elapsed > 60f && proximityDelta > 4 && sightedAmount >= 1)
                    sessionPropertyHeat -= UnityEngine.Random.Range(1, 4);

                //MelonLogger.Msg("PI Finished");
                //MelonLogger.Msg("Investigation delta: " + investigationDelta);
                //MelonLogger.Msg("Sighted amnt: " + sightedAmount);
                //MelonLogger.Msg("Proximity delta: " + proximityDelta);
                //MelonLogger.Msg("Session Heat ->" + sessionPropertyHeat);
                try
                {
                    if (currentSummoned.Contains(offc))
                        currentSummoned.Remove(offc);
                    currentPICount -= 1;
                    NPC npc = offc.gameObject.GetComponent<NPC>();
                    if (npc != null && NPCManager.NPCRegistry.Contains(npc))
                        NPCManager.NPCRegistry.Remove(npc);
                    if (npc != null && npc.gameObject != null)
                        UnityEngine.Object.Destroy(npc.gameObject);
                }
                catch (Exception ex)
                {
                    MelonLogger.Error(ex);
                }

            }
        }
        private IEnumerator CrazyCops()
        {
            //MelonLogger.Msg("Crazy Cops Enabled");

            for (; ; )
            {
                (float min, float max) = ThresholdUtils.Evaluate(ThresholdMappings.CrazyCopsFreq, TimeManager.Instance.ElapsedDays);
                yield return new WaitForSeconds(UnityEngine.Random.Range(min, max));
                if (!registered) yield break;

                //MelonLogger.Msg("Crazy Cops Evaluate");

                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);

                //MelonLogger.Msg("Crazy Cops Run");
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                if (randomPlayer.CurrentProperty != null)
                    continue;

                Vector3 playerPosition = randomPlayer.transform.position;

                PoliceOfficer nearestOfficer = null;
                float closestDistance = 100f;

                PoliceOfficer[] officersSnapshot;
                lock (_officerLock)
                {
                    //MelonLogger.Msg("Crazy Cop Lock");
                    officersSnapshot = allActiveOfficers.Where(o => o != null && o.gameObject != null).ToArray();
                }
                foreach (PoliceOfficer officer in officersSnapshot)
                {
                    yield return new WaitForSeconds(0.01f);

                    float distance = Vector3.Distance(officer.transform.position, playerPosition);
                    if (distance < closestDistance && !currentSummoned.Contains(officer) && !currentDrugApprehender.Contains(officer) && !IsStationNearby(playerPosition) && !officer.isInBuilding)
                    {
                        closestDistance = distance;
                        nearestOfficer = officer;
                    }
                }

                (float minRang, float maxRang) = ThresholdUtils.Evaluate(ThresholdMappings.CrazyCopsRange, (int)MoneyManager.Instance.LifetimeEarnings);
                if (nearestOfficer != null && closestDistance < UnityEngine.Random.Range(minRang, maxRang))
                {
                    // Vehicle patrols we need diff Behaviour
                    if (nearestOfficer.Behaviour.activeBehaviour && nearestOfficer.Behaviour.activeBehaviour is VehiclePatrolBehaviour)
                    {
                        nearestOfficer.BeginVehiclePursuit_Networked(randomPlayer.NetworkObject, nearestOfficer.AssignedVehicle.NetworkObject, true);
                        coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player: randomPlayer)));
                        continue;
                    }

                    // Foot patrols checkpoints etc
                    if (UnityEngine.Random.Range(0f, 1f) > 0.3f)
                    {
                        nearestOfficer.Movement.FacePoint(randomPlayer.transform.position, lerpTime: 0.2f);
                        yield return new WaitForSeconds(0.3f);
                        if (nearestOfficer.Awareness.VisionCone.IsPlayerVisible(randomPlayer))
                        {
                            nearestOfficer.BeginFootPursuit_Networked(randomPlayer.NetworkObject, true);
                            coros.Add(MelonCoroutines.Start(GiveFalseCharges(severity: 3, player: randomPlayer)));
                        }
                    }
                    else
                    {
                        // Police called -> dispatch vehicle and sleep 4s -> investigation status
                        coros.Add(MelonCoroutines.Start(LateInvestigation(randomPlayer)));
                    }
                }
            }
        }

        #endregion

        #region Base Utils
        public static IEnumerator AttemptWarp(PoliceOfficer offc, Transform target)
        {
            Vector3 warpInit = Vector3.zero;
            int maxWarpAttempts = 30;
            for (int i = 0; i < maxWarpAttempts; i++)
            {
                float xInitOffset = UnityEngine.Random.Range(8f, 30f);
                float zInitOffset = UnityEngine.Random.Range(8f, 30f);
                xInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                zInitOffset *= UnityEngine.Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
                Vector3 targetWarpPosition = target.position + new Vector3(xInitOffset, 0f, zInitOffset);
                offc.Movement.GetClosestReachablePoint(targetWarpPosition, out warpInit);
                if (warpInit != Vector3.zero)
                {
                    offc.Movement.Warp(warpInit);
                    //MelonLogger.Msg("Warped Pos " + offc.transform.position.ToString());
                    //MelonLogger.Msg("Distance: " + Vector3.Distance(offc.transform.position, target.position));
                    break;
                }
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
        private IEnumerator UpdateOfficerList()
        {
            for (; ; )
            {
                yield return new WaitForSeconds(200f);
                //MelonLogger.Msg("Refreshing officers...");
                if (!registered) yield break;
                PoliceOfficer[] all = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>(true);
                if (all.Length == allActiveOfficers.Count) continue; // No change

                lock (_officerLock)
                {
                    allActiveOfficers.Clear();
                    foreach (var officer in all)
                    {
                        if (officer != null && !currentSummoned.Contains(officer))
                            allActiveOfficers.Add(officer);
                    }
                }
                yield return new WaitForSeconds(0.5f);
                coros.Add(MelonCoroutines.Start(SetOfficers()));
            }
        }
        private IEnumerator PIAvatar(PoliceOfficer offc)
        {
            #region PI Avatar
            var originalBodySettings = offc.Avatar.CurrentSettings.BodyLayerSettings;
            Il2CppSystem.Collections.Generic.List<LayerSetting> bodySettings = new();
            foreach (var layer in originalBodySettings)
            {
                bodySettings.Add(new LayerSetting
                {
                    layerPath = layer.layerPath,
                    layerTint = layer.layerTint
                });
            }

            var originalAccessorySettings = offc.Avatar.CurrentSettings.AccessorySettings;
            Il2CppSystem.Collections.Generic.List<AccessorySetting> accessorySettings = new();
            foreach (var acc in originalAccessorySettings)
            {
                accessorySettings.Add(new AccessorySetting
                {
                    path = acc.path,
                    color = acc.color
                });
            }

            for (int i = 0; i < bodySettings.Count; i++)
            {
                var layer = bodySettings[i];
                layer.layerPath = "";
                layer.layerTint = Color.white;
                bodySettings[i] = layer;
            }

            for (int i = 0; i < accessorySettings.Count; i++)
            {
                var acc = accessorySettings[i];
                acc.path = "";
                acc.color = Color.white;
                accessorySettings[i] = acc;
            }

            var jeans = bodySettings[2];
            jeans.layerPath = "Avatar/Layers/Bottom/Jeans";
            jeans.layerTint = new Color(0.396f, 0.396f, 0.396f);
            bodySettings[2] = jeans;
            var shirt = bodySettings[3];
            shirt.layerPath = "Avatar/Layers/Top/RolledButtonUp";
            shirt.layerTint = new Color(0.326f, 0.578f, 0.896f);
            bodySettings[3] = shirt;

            var cap = accessorySettings[0];
            cap.path = "Avatar/Accessories/Head/Cap/Cap";
            cap.color = new Color(0.613f, 0.493f, 0.344f);
            accessorySettings[0] = cap;
            var blazer = accessorySettings[1];
            blazer.path = "Avatar/Accessories/Chest/Blazer/Blazer";
            blazer.color = new Color(0.613f, 0.493f, 0.344f);
            accessorySettings[1] = blazer;
            var sneakers = accessorySettings[2];
            sneakers.path = "Avatar/Accessories/Feet/Sneakers/Sneakers";
            sneakers.color = new Color(0.151f, 0.151f, 0.151f);
            accessorySettings[2] = sneakers;

            offc.Avatar.CurrentSettings.BodyLayerSettings = bodySettings;
            offc.Avatar.CurrentSettings.AccessorySettings = accessorySettings;
            offc.Avatar.ApplyBodyLayerSettings(offc.Avatar.CurrentSettings);
            offc.Avatar.ApplyAccessorySettings(offc.Avatar.CurrentSettings);
            yield return null;
        }
        #endregion
        private bool IsStationNearby(Vector3 pos)
        {
            float distToStation = Vector3.Distance(PoliceStation.GetClosestPoliceStation(pos).transform.position, pos);
            return distToStation < 20f;
        }
        public static void SetPoliceNPC()
        {
            PrefabObjects spawnablePrefabs = netManager.SpawnablePrefabs;
            for (int i = 0; i < spawnablePrefabs.GetObjectCount(); i++)
            {
                NetworkObject prefab = spawnablePrefabs.GetObject(true, i);
                if (prefab?.gameObject?.name == "PoliceNPC")
                {
                    NACopsV1_IL2Cpp.NACops.Instance.policeBase = prefab;
                    break;
                }
            }
        }
        private IEnumerator SetOfficers()
        {
            PoliceOfficer[] officersSnapshot;
            lock (_officerLock)
            {
                officersSnapshot = allActiveOfficers.Where(o => o != null && o.gameObject != null).ToArray();
            }

            foreach (PoliceOfficer officer in officersSnapshot)
            {
                yield return new WaitForSeconds(0.01f);
                officer.Leniency = 0.1f;
                officer.Suspicion = 1f;

                yield return new WaitForSeconds(0.01f);
                if (currentConfig.OverrideBodySearch)
                {
                    officer.BodySearchDuration = 20f;
                    officer.BodySearchChance = 1f;
                    BodySearchBehaviour bodySearch = officer.GetComponent<BodySearchBehaviour>();
                    OverrideBodySearchEscalation(bodySearch);
                }

                yield return new WaitForSeconds(0.01f);
                if (currentConfig.OverrideMovement)
                {
                    officer.Movement.RunSpeed = currentOfficerConfig.MovementRunSpeed;
                    officer.Movement.WalkSpeed = currentOfficerConfig.MovementWalkSpeed;
                }

                yield return new WaitForSeconds(0.01f);
                if (currentConfig.OverrideCombatBeh)
                {
                    officer.Behaviour.CombatBehaviour.GiveUpRange = currentOfficerConfig.CombatGiveUpRange;
                    officer.Behaviour.CombatBehaviour.GiveUpTime = currentOfficerConfig.CombatGiveUpTime;
                    officer.Behaviour.CombatBehaviour.DefaultSearchTime = currentOfficerConfig.CombatSearchTime;
                    officer.Behaviour.CombatBehaviour.DefaultMovementSpeed = currentOfficerConfig.CombatMoveSpeed;
                    officer.Behaviour.CombatBehaviour.GiveUpAfterSuccessfulHits = currentOfficerConfig.CombatEndAfterHits;
                }

                yield return new WaitForSeconds(0.01f);
                if (currentConfig.OverrideMaxHealth)
                {
                    officer.Health.MaxHealth = currentOfficerConfig.OfficerMaxHealth;
                    officer.Health.Revive();
                }

                yield return new WaitForSeconds(0.01f);
                if (currentConfig.OverrideWeapon)
                {
                    var gun = officer.GunPrefab;
                    if (gun != null && gun?.Cast<AvatarRangedWeapon>() is AvatarRangedWeapon rangedWeapon)
                    {
                        rangedWeapon.CanShootWhileMoving = true;
                        rangedWeapon.MagazineSize = currentOfficerConfig.WeaponMagSize;
                        rangedWeapon.MaxFireRate = currentOfficerConfig.WeaponFireRate;
                        rangedWeapon.MaxUseRange = currentOfficerConfig.WeaponMaxRange;
                        rangedWeapon.ReloadTime = currentOfficerConfig.WeaponReloadTime;
                        rangedWeapon.RaiseTime = currentOfficerConfig.WeaponRaiseTime;
                        rangedWeapon.HitChance_MaxRange = currentOfficerConfig.WeaponHitChanceMax;
                        rangedWeapon.HitChance_MinRange = currentOfficerConfig.WeaponHitChanceMin;
                    }
                }
            }
            //MelonLogger.Msg("Officer properties complete");
            yield break;
        }
        private void OverrideBodySearchEscalation(BodySearchBehaviour bodySearch)
        {
            if (bodySearch == null) return;

            try
            {
                FieldInfo timeOutsideRangeField = typeof(BodySearchBehaviour).GetField("timeOutsideRange", BindingFlags.NonPublic | BindingFlags.Instance);
                if (timeOutsideRangeField != null)
                {
                    timeOutsideRangeField.SetValue(bodySearch, 30f);
                }

                FieldInfo targetDistanceField = typeof(BodySearchBehaviour).GetField("targetDistanceOnStart", BindingFlags.NonPublic | BindingFlags.Instance);
                if (targetDistanceField != null)
                {
                    float currentDistance = (float)targetDistanceField.GetValue(bodySearch);
                    targetDistanceField.SetValue(bodySearch, currentDistance + 30f);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"Failed to change body search Behaviour: {ex}");
            }
        }
        public static IEnumerator GiveFalseCharges(int severity, Player player)
        {
            if (!currentConfig.CorruptCops) yield break;
            switch (severity)
            {
                case 1:
                    player.CrimeData.AddCrime(new DrugTrafficking());
                    player.CrimeData.AddCrime(new AttemptingToSell());
                    player.CrimeData.AddCrime(new Evading());
                    break;
                case 2:
                    player.CrimeData.AddCrime(new FailureToComply(), 10);
                    player.CrimeData.AddCrime(new Evading());
                    break;
                case 3:
                    player.CrimeData.AddCrime(new PossessingHighSeverityDrug(), 60);
                    break;

            }
            yield return null;
        }
        public static IEnumerator LateInvestigation(Player player)
        {
            yield return new WaitForSeconds(1f);
            if (!registered || !Singleton<LawManager>.InstanceExists || PoliceStation.PoliceStations.Count == 0) yield break;
            PoliceStation station = PoliceStation.GetClosestPoliceStation(player.transform.position);
            if (station.OccupantCount < 2) yield break;
            try
            {
                Singleton<LawManager>.Instance.PoliceCalled(player, new DrugTrafficking());
            }
            catch (NullReferenceException ex)
            {
                MelonLogger.Error("Failed to invoke PoliceCalled status " + ex);
            }
            yield return new WaitForSeconds(4f);
            if (!registered) yield break;
            player.CrimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.Investigating);
            yield return null;
        }

        #endregion

        #region Utils for Raid

        // Warehouse
        public static List<Vector3> sentryPositions = new()
        {
             new Vector3(-80.03324f, 0.5f, -49.13721f),
             new Vector3(-89.50997f, 0.35f, -54.900497f),
             new Vector3(-94.14122f, -2.16f, -46.12968f),
             new Vector3(-84.08176f, -2.2f, -38.14134f),
        };
        #endregion

        #region Utils for progression
        public class MinMaxThreshold
        {
            public int MinOf { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }

            public MinMaxThreshold(int minOf, float min, float max)
            {
                MinOf = minOf;
                Min = min;
                Max = max;
            }
        }
        public static class ThresholdUtils
        {
            public static (float min, float max) Evaluate(List<MinMaxThreshold> thresholds, int value)
            {
                MinMaxThreshold bestMatch = thresholds[0];
                foreach (var threshold in thresholds)
                {
                    if (value >= threshold.MinOf)
                        bestMatch = threshold;
                    else
                        break;
                }

                return (bestMatch.Min, bestMatch.Max);
            }
        }
        public static class ThresholdMappings
        {
            // Days total
            public static readonly List<MinMaxThreshold> LethalCopFreq = new()
            {
                new(0,   30f, 60f),
                new(5,   20f, 60f),
                new(10,  20f, 50f),
                new(20,  15f, 40f),
                new(30,  10f, 30f),
                new(40,  10f, 20f),
                new(50,  8f, 18f),
            };
            // Networth - min distance max distance rand range
            public static readonly List<MinMaxThreshold> LethalCopRange = new()
            {
                new(0,      4f, 6f),
                new(8000,   4f, 8f),
                new(30000,  5f, 9f),
                new(100000, 6f, 10f),
                new(300000, 7f, 12f),
                new(600000, 9f, 14f),
                new(1000000, 10f, 15f),

            };

            // Days total
            public static readonly List<MinMaxThreshold> CrazyCopsFreq = new()
            {
                new(0,   300f, 450f),
                new(5,   300f, 400f),
                new(10,  200f, 350f),
                new(20,  150f, 350f),
                new(30,  150f, 300f),
                new(40,  100f, 300f),
                new(50,  100f, 250f),
            };
            // Networth - min distance max distance rand range
            public static readonly List<MinMaxThreshold> CrazyCopsRange = new()
            {
                new(0,      10f, 30f),
                new(8000,   15f, 30f),
                new(30000,  20f, 35f),
                new(100000, 25f, 35f),
                new(300000, 30f, 40f),
                new(500000, 30f, 40f),
                new(1000000, 30f, 45f),
                new(3000000, 30f, 50f),
            };

            // Days total
            public static readonly List<MinMaxThreshold> NearbyCrazThres = new()
            {
                new(0,   400f, 650f),
                new(5,   300f, 600f),
                new(10,  120f, 500f),
                new(20,  120f, 500f),
                new(30,  120f, 400f),
                new(40,  120f, 350f),
                new(50,  120f, 300f),
            };
            // Networth - min distance max distance rand range
            public static readonly List<MinMaxThreshold> NearbyCrazRange = new()
            {
                new(0,      10f, 15f),
                new(8000,   10f, 20f),
                new(30000,  10f, 25f),
                new(100000, 20f, 35f),
                new(300000, 20f, 40f),
                new(500000, 25f, 40f),

            };

            // Networth
            public static readonly List<MinMaxThreshold> PIThres = new()
            {
                new(0,        450f, 800f),
                new(1000,     380f, 800f),
                new(10000,    350f, 700f),
                new(30000,    330f, 690f),
                new(60000,    330f, 650f),
                new(100000,   300f, 600f),
                new(300000,   300f, 570f),
                new(800000,   300f, 480f),
                new(1500000,  270f, 400f),
                new(8000000,  230f, 360f),
            };
            // Days total - probability range of toggling attn ( result > 0.5 = true )
            public static readonly List<MinMaxThreshold> PICurfewAttn = new()
            {
                new(0,   0f, 0.5f),
                new(5,   0f, 0.5f),
                new(10,  0f, 0.55f),
                new(20,  0f, 0.55f),
                new(30,  0f, 0.55f),
                new(40,  0f, 0.60f),
                new(50,  0f, 0.62f),
                new(60,  0f, 0.64f),
                new(70,  0f, 0.66f),
                new(80,  0f, 0.68f),
                new(90,  0f, 0.70f),
            };

            // Days total - probability range of snitching a sample ( result > 0.8 = true )
            public static readonly List<MinMaxThreshold> SnitchProbability = new()
            {
                new(0,   0f, 0.8f),
                new(5,   0f, 0.85f),
                new(10,  0f, 0.88f),
                new(20,  0f, 0.9f),
                new(30,  0f, 0.93f),
                new(40,  0f, 0.95f),
                new(50,  0.05f, 1f),
                new(60,  0.1f, 1f),
                new(70,  0.15f, 1f),
                new(80,  0.2f, 1f),
                new(90,  0.25f, 1f),
            };

            // Customer relation delta*10 (0-50) - probability range of being a drug bust ( result > 0.5 = true )
            public static readonly List<MinMaxThreshold> BuyBustProbability = new()
            {
                new(0,   0.1f, 1f),
                new(5,   0.05f, 0.9f),
                new(10,  0f, 0.8f),
                new(15,  0f, 0.75f),
                new(20,  0f, 0.65f),
                new(30,  0f, 0.6f),
                new(40,  0f, 0.55f),
            };
        }

        #endregion

    }
}

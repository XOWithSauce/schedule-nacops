

using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using HarmonyLib;

using static NACops.NACops;
using static NACops.DebugModule;
using static NACops.AvatarUtility;

#if MONO
using ScheduleOne.AvatarFramework;
using ScheduleOne.NPCs;
using ScheduleOne.Police;
using ScheduleOne.Dialogue;
using ScheduleOne.Map;
using ScheduleOne.NPCs.Framework;
using Behaviour = ScheduleOne.NPCs.Behaviour.Behaviour;
using FishNet.Managing;
using FishNet.Object;
#else
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.Cartel;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.NPCs.Framework;
using Behaviour = Il2CppScheduleOne.NPCs.Behaviour.Behaviour;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Object;
#endif


namespace NACops
{

    public static class CopInitHelper
    {
#if MONO
        public static List<PoliceOfficer> generatedOfficerPool = new();
#else
        public static Il2CppSystem.Collections.Generic.List<PoliceOfficer> generatedOfficerPool = new();
#endif
        public static GameObject copBaseClone;
        public static PoliceOfficer investigator;
        public static int investigatorID;
        public static PoliceOfficer buyBustCop;
        public static int buyBustCopID;
        public static IEnumerator ReplicateCopNPC()
        {
            Log("Replicating COP NPC");
            PoliceOfficer officer = UnityEngine.Object.FindObjectOfType<PoliceOfficer>();

            AvatarSettings copySettings = officer.Avatar.CurrentSettings;

            if (officer == null)
            {
                Log("No officer found");
                yield break;
            }
            GameObject obj = officer.gameObject;
            obj.SetActive(false);

            GameObject clone = UnityEngine.Object.Instantiate(obj);
            copBaseClone = clone;

            clone.transform.position = Vector3.zero;
            clone.transform.rotation = Quaternion.identity;

            NPC npc = clone.GetComponent<NPC>();
            NetworkObject newNob = clone.GetComponent<NetworkObject>();
            PoliceOfficer offc = clone.GetComponent<PoliceOfficer>();
            offc.AutoDeactivate = false; // Prevent from returning to station and from being added to officer pool

            clone.name = $"RuntimeOfficer";
            yield return MelonCoroutines.Start(InitiateClone(newNob, networkManager));

            npc.NPCData.BasicInfo.ID = "officerPrefab";

            if (!NPCManager.NPCRegistry.Contains(npc))
                NPCManager.NPCRegistry.Add(npc);

            npc.Avatar.LoadAvatarSettings(copySettings);
            offc.PursuitBehaviour.arrestingEnabled = false;

            try
            {
                networkManager.ServerManager.Spawn(newNob);
            }
            catch (Exception ex)
            {
                Log($"Failed to spawn officer {ex}");
            }

            offc.Behaviour.ScheduleManager.DisableSchedule();
            offc.Movement.PauseMovement();
            if (PoliceOfficer.Officers.Contains(offc))
                PoliceOfficer.Officers.Remove(offc);
            
            // for some reason the interactable object keeps bugging out
            // maybe due to some collider being in wrong laYer or something is unassigned
            // to fix, increase sphere size here
            DialogueController_Police dg = offc.GetComponentInChildren<DialogueController_Police>();
            Transform sphere = dg.IntObj.transform.Find("Sphere");
            CapsuleCollider cc = sphere.GetComponent<CapsuleCollider>();
            cc.height = 1.85f;
            cc.radius = 0.5f;

            obj.SetActive(true);
            Log("Finished replicating COP NPC");
            yield break;
        }

        public static IEnumerator InitiateClone(NetworkObject newNob, NetworkManager netManager, NPCData dataPreset = null)
        {
            NPC npc = newNob.GetComponent<NPC>();

            // Populate unassigned fields
            newNob.transform.Find("Avatar").gameObject.SetActive(true);
            newNob.transform.Find("Avatar/BodyContainer").gameObject.SetActive(true);
            newNob.GetComponent<NavMeshAgent>().enabled = true;
            newNob.gameObject.SetActive(true);
            yield return Wait01;
            yield return frameEnd;

            newNob.transform.Find("Avatar").gameObject.SetActive(false);
            newNob.transform.Find("Avatar/BodyContainer").gameObject.SetActive(false);
            newNob.GetComponent<NavMeshAgent>().enabled = false;
            if (newNob.gameObject.activeSelf)
                newNob.gameObject.SetActive(false);
            
            // Remove CustomerAttendDealBehaviour since calling disable on it gives nullreference exceptions if in behaviour stack
            Behaviour temp = npc.Behaviour.GetBehaviour("Customer attend deal");
            if (temp)
            {
                UnityEngine.Object.Destroy(temp.gameObject);
                // Refresh the stack
                npc.Behaviour.OnValidate();
            }

            try
            {
#if MONO
                MethodInfo updateNetworkBehMethod = AccessTools.Method(typeof(NetworkObject), "UpdateNetworkBehaviours", new[] {
                    typeof(NetworkObject),
                    typeof(byte).MakeByRefType(), // ref byte componentIndex 
                });

                if (updateNetworkBehMethod == null)
                    Log("updateNetworkBehMethod not found.");
                else
                    updateNetworkBehMethod.Invoke(newNob, new object[] { newNob, (byte)0 });
#else
                byte componentIndex = 0;
                newNob.UpdateNetworkBehaviours(newNob, ref componentIndex);
#endif
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            if (dataPreset != null)
            {
                npc.ApplyNPCData(dataPreset);
            }

            // Invoke pre init to populate the necessary component references
            try
            {
#if MONO
                MethodInfo preInitializeMethod = AccessTools.Method(typeof(NetworkObject), "Preinitialize_Internal", new[] {
                    typeof(FishNet.Managing.NetworkManager),
                    typeof(int),
                    typeof(FishNet.Connection.NetworkConnection),
                    typeof(bool)
                });

                if (preInitializeMethod == null)
                    Log("Method Preinitialize_Internal not found.");
                else
                    preInitializeMethod.Invoke(newNob, new object[] { netManager, 150, null, true });
#else
                newNob.Preinitialize_Internal(netManager, 150, null, true);
#endif
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            // Invoke init to clear self + related behs + init npc
            try
            {
#if MONO
                MethodInfo initializeMethod = AccessTools.Method(typeof(NetworkObject), "Initialize", new[] {
                    typeof(bool),
                    typeof(bool)
                });

                if (initializeMethod == null)
                    Log("NetworkObject.Initialize internal method not found.");
                else
                    initializeMethod.Invoke(newNob, new object[] { true, true });
#else
                newNob.Initialize(true, true);
#endif
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            try
            {
                newNob.SetIsNetworked(false);
            }
            catch (Exception ex)
            {
                Log($"Failed to set network object networking to false: {ex}");
            }

            npc.Awareness.SetAwarenessActive(false);
        }
        
        // Extend the base amount of officers available for daily routines
        public static IEnumerator SpawnOfficersRuntime()
        {
            PoliceStation station = PoliceStation.PoliceStations[0];
            for (int i = 0; i < PoliceOfficer.Officers.Count; i++)
                generatedOfficerPool.Add(PoliceOfficer.Officers[i]);

            for (int i = 0; i < officerConfig.ModAddedOfficersCount; i++)
            {
                Log($"Spawning {i}");
                GameObject copNet = UnityEngine.Object.Instantiate<GameObject>(copBaseClone);
                NetworkObject newNob = copNet.GetComponent<NetworkObject>();
                PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
                offc.AutoDeactivate = false; 

                copNet.name = $"NACop_{i}";
                yield return MelonCoroutines.Start(InitiateClone(newNob, networkManager));

                NPC myNpc = copNet.gameObject.GetComponent<NPC>();
                myNpc.NPCData.BasicInfo.ID = $"NACop_{i}";
                myNpc.NPCData.BasicInfo.FirstName = "Officer";
                myNpc.NPCData.BasicInfo.LastName = "";
                myNpc.NPCData.Inventory.CanBePickpocketed = true;
                myNpc.NPCData.WeatherBehaviour.UseUmbrellaChance = 0f;
                myNpc.Actions._canUseUmbrella = false;

                if (!NPCManager.NPCRegistry.Contains(myNpc))
                    NPCManager.NPCRegistry.Add(myNpc);

                networkManager.ServerManager.Spawn(copNet);
                copNet.gameObject.SetActive(true);

                DialogueController controller = offc.DialogueHandler.GetComponent<DialogueController>();
                controller.Choices.Clear();

                copNet.transform.Find("Avatar").gameObject.SetActive(true);

                AvatarSettings createdSettings = null;
                SetRandomAvatar(offc, ref createdSettings);
                SetVOEmitter(offc);
                yield return GenerateImpostor(offc, createdSettings);

                copNet.GetComponent<NavMeshAgent>().enabled = true;
                offc.AutoDeactivate = true;
                offc.Awareness.SetAwarenessActive(true);
                offc.Movement.Warp(station.SpawnPoint);
                station.NPCEnteredBuilding(offc, station.Doors[0]);
                generatedOfficerPool.Add(offc);

            }
            Log("Extended cops array");
            yield break;
        }

        // Create a reserved cop npc for investigator
        public static IEnumerator CreateInvestigator()
        {
            GameObject copNet = UnityEngine.Object.Instantiate<GameObject>(copBaseClone);
            NetworkObject newNob = copNet.GetComponent<NetworkObject>();
            PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
            offc.AutoDeactivate = false;
            offc.ChatterEnabled = false;
            offc.BodySearchChance = 0f;

            copNet.name = $"NACop_Investigator";
            yield return MelonCoroutines.Start(InitiateClone(newNob, networkManager));

            NPC myNpc = copNet.gameObject.GetComponent<NPC>();
            myNpc.NPCData.BasicInfo.ID = $"NACop_Investigator";
            myNpc.NPCData.BasicInfo.FirstName = "Investigator";
            myNpc.NPCData.BasicInfo.LastName = "";
            myNpc.NPCData.Inventory.CanBePickpocketed = false;
            myNpc.NPCData.WeatherBehaviour.UseUmbrellaChance = 0f;
            myNpc.Actions._canUseUmbrella = false;

            //myNpc.transform.parent = NPCManager.Instance.NPCContainer;

            if (!NPCManager.NPCRegistry.Contains(myNpc))
                NPCManager.NPCRegistry.Add(myNpc);

            networkManager.ServerManager.Spawn(copNet);

            DialogueController controller = offc.DialogueHandler.GetComponent<DialogueController>();
            controller.Choices.Clear();

            // stay inactive until investigation starts
            // copNet.gameObject.SetActive(true);
            // copNet.transform.Find("Avatar").gameObject.SetActive(true);
            // copNet.GetComponent<NavMeshAgent>().enabled = true;
            investigator = offc;
            investigatorID = investigator.transform.root.gameObject.GetInstanceID();
            investigator.Behaviour.ScheduleManager.DisableSchedule();
            if (PoliceOfficer.Officers.Contains(investigator))
                PoliceOfficer.Officers.Remove(investigator);

            offc.Awareness.SetAwarenessActive(false);

            Log("Done spawning Investigator");
            yield break;
        }

        // Create a reserved cop npc for buy bust
        public static IEnumerator CreateBuyBustCop()
        {
            GameObject copNet = UnityEngine.Object.Instantiate<GameObject>(copBaseClone);
            NetworkObject newNob = copNet.GetComponent<NetworkObject>();
            PoliceOfficer offc = copNet.gameObject.GetComponent<PoliceOfficer>();
            offc.AutoDeactivate = false;

            copNet.name = $"NACop_BuyBust";
            yield return MelonCoroutines.Start(InitiateClone(newNob, networkManager));

            NPC myNpc = copNet.gameObject.GetComponent<NPC>();
            myNpc.NPCData.BasicInfo.ID = $"NACop_BuyBust";
            myNpc.NPCData.BasicInfo.FirstName = "Officer";
            myNpc.NPCData.BasicInfo.LastName = "";
            myNpc.NPCData.Inventory.CanBePickpocketed = false;
            myNpc.NPCData.WeatherBehaviour.UseUmbrellaChance = 0f;
            myNpc.Actions._canUseUmbrella = false;

            //myNpc.transform.parent = NPCManager.Instance.NPCContainer;

            if (!NPCManager.NPCRegistry.Contains(myNpc))
                NPCManager.NPCRegistry.Add(myNpc);

            networkManager.ServerManager.Spawn(copNet);

            DialogueController controller = offc.DialogueHandler.GetComponent<DialogueController>();
            controller.Choices.Clear();

            // stay inactive until buy bust starts
            copNet.gameObject.SetActive(true);
            copNet.transform.Find("Avatar").gameObject.SetActive(true);

            AvatarSettings createdSettings = null;
            SetRandomAvatar(offc, ref createdSettings);
            SetVOEmitter(offc);
            yield return GenerateImpostor(offc, createdSettings);

            copNet.gameObject.SetActive(false);
            copNet.transform.Find("Avatar").gameObject.SetActive(false);

            buyBustCop = offc;
            buyBustCopID = buyBustCop.transform.root.gameObject.GetInstanceID();
            buyBustCop.Behaviour.ScheduleManager.DisableSchedule();
            if (PoliceOfficer.Officers.Contains(offc))
                PoliceOfficer.Officers.Remove(offc);

            offc.Awareness.SetAwarenessActive(false);

            Log("Done spawning buy bust officer");
            yield break;
        }
    }
}
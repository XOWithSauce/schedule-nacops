using MelonLoader;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

using static NACops.NACops;
using static NACops.DebugModule;
using static NACops.AvatarUtility;

#if MONO
using HarmonyLib;
using System.Reflection;
using ScheduleOne.NPCs;
using ScheduleOne.Police;
using ScheduleOne.Dialogue;
using ScheduleOne.Map;
using ScheduleOne.NPCs.Framework;
using ScheduleOne.Core.Avatar;
using ScheduleOne.Law;
using Behaviour = ScheduleOne.NPCs.Behaviour.Behaviour;
using FishNet.Managing;
using FishNet.Object;
#else
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.NPCs.Framework;
using Il2CppScheduleOne.Core.Avatar;
using Il2CppScheduleOne.Law;
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
        public static Transform copNPCParent;

        public static IEnumerator ReplicateCopNPC()
        {
            Log("Replicating COP NPC");
            PoliceOfficer officer = UnityEngine.Object.FindObjectOfType<PoliceOfficer>();

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
            if (officer._defaultParent != null)
            {
                clone.transform.parent = officer._defaultParent;
                offc._defaultParent = officer._defaultParent;
                copNPCParent = officer._defaultParent;
            }
            yield return MelonCoroutines.Start(InitiateClone(newNob, networkManager));

            npc.NPCData.BasicInfo.ID = "officerPrefab";

            if (!NPCManager.NPCRegistry.Contains(npc))
                NPCManager.NPCRegistry.Add(npc);

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
            offc.Movement.Stop();

            if (offc.Awareness != null)
            {
                offc.Awareness.SetAwarenessActive(false);
                Log("Awareness disabled");
            }
            else
            {
                Log("Awareness does not exist yet");
            }

            if (PoliceOfficer.Officers.Contains(offc))
                PoliceOfficer.Officers.Remove(offc);

            offc.Awareness.SetAwarenessActive(false);

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

            yield break;
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
                myNpc.HasUmbrella = false;

                if (!NPCManager.NPCRegistry.Contains(myNpc))
                    NPCManager.NPCRegistry.Add(myNpc);

                Log($"Removing unassigned avatar objects countpre: {offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount}");
                Transform target = offc.Avatar.Appearance._avatarObjectsContainer.transform;
                List<AvatarObject> destroyable = new();
                for (int j = offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount - 1; j >= 0; j--)
                {
                    Transform avatarObjectTr = target.GetChild(j);
                    if (avatarObjectTr)
                    {
                        AvatarObject comp = avatarObjectTr.gameObject.GetComponent<AvatarObject>();

                        if (comp != null && !offc.Avatar.Appearance._appliedObjects.Contains(comp))
                            destroyable.Add(comp);
                    }
                }
                int destroyableCount = destroyable.Count;
                foreach (AvatarObject avObj in destroyable)
                {
                    UnityEngine.Object.Destroy(avObj.gameObject);
                }
                Log($"Removed unassigned avatar objects count: {destroyableCount}");

                networkManager.ServerManager.Spawn(copNet);
                copNet.gameObject.SetActive(true);

                DialogueController controller = offc.DialogueHandler.GetComponent<DialogueController>();
                controller.Choices.Clear();

                copNet.transform.Find("Avatar").gameObject.SetActive(true);

                SetRandomOfficerAvatar(offc);
                SetVOEmitter(offc);

                // Retains AvatarAttachment and SkinnedAvatarAttachment type objects within Avatar transform hierarchy
                // for the cloned objects, probably unassigned worn/naked objects after cloned so must remove those?
                AvatarAttachment[] avatarAttachments = offc.Avatar.GetComponentsInChildren<AvatarAttachment>(true);
                SkinnedAvatarAttachment[] skinnedAvatarAttachments = offc.Avatar.GetComponentsInChildren<SkinnedAvatarAttachment>(true);

                Log("Removing unassigned instantiated avatar objects");
                if (avatarAttachments != null && avatarAttachments.Length > 0)
                {
                    Log($"Found {avatarAttachments.Length} avatar attachment type objects");
                    foreach (AvatarAttachment obj in avatarAttachments)
                    {
                        if (obj.Parent == null)
                            UnityEngine.Object.Destroy(obj.gameObject);
                    }
                }
                if (skinnedAvatarAttachments != null && skinnedAvatarAttachments.Length > 0)
                {
                    Log($"Found {skinnedAvatarAttachments.Length} skinned avatar attachment type objects");
                    foreach (SkinnedAvatarAttachment obj in skinnedAvatarAttachments)
                    {
                        if (obj.Parent == null)
                            UnityEngine.Object.Destroy(obj.gameObject);
                    }
                }

                yield return GenerateImpostor(offc);

                offc.transform.parent = copNPCParent;
                offc._defaultParent = copNPCParent;

                // Validate because after impostor create
                // the cloned objects refresh the avatar and have it unassigned...
                if (offc.PoliceBelt == null)
                {
                    // fetch the correct one from worn objects
                    foreach (AvatarObject obj in offc.Avatar.Appearance._wornObjects)
                    {
                        if (obj.Id == "policebelt")
                        {
                            offc.PoliceBelt = obj.GetComponent<PoliceBelt>();
                            break;
                        }
                    }
                }

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
            myNpc.HasUmbrella = false;

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

            offc.transform.parent = copNPCParent;
            offc._defaultParent = copNPCParent;

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
            myNpc.HasUmbrella = false;

            //myNpc.transform.parent = NPCManager.Instance.NPCContainer;

            if (!NPCManager.NPCRegistry.Contains(myNpc))
                NPCManager.NPCRegistry.Add(myNpc);

            Log($"Removing unassigned avatar objects countpre: {offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount}");
            Transform target = offc.Avatar.Appearance._avatarObjectsContainer.transform;
            List<AvatarObject> destroyable = new();
            for (int j = offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount - 1; j >= 0; j--)
            {
                Transform avatarObjectTr = target.GetChild(j);
                if (avatarObjectTr)
                {
                    AvatarObject comp = avatarObjectTr.gameObject.GetComponent<AvatarObject>();

                    if (comp != null && !offc.Avatar.Appearance._appliedObjects.Contains(comp))
                        destroyable.Add(comp);
                }
            }
            int destroyableCount = destroyable.Count;
            foreach (AvatarObject avObj in destroyable)
            {
                UnityEngine.Object.Destroy(avObj.gameObject);
            }
            Log($"Removed unassigned avatar objects count: {destroyableCount}");


            networkManager.ServerManager.Spawn(copNet);

            DialogueController controller = offc.DialogueHandler.GetComponent<DialogueController>();
            controller.Choices.Clear();

            // stay inactive until buy bust starts
            copNet.gameObject.SetActive(true);
            copNet.transform.Find("Avatar").gameObject.SetActive(true);

            SetRandomOfficerAvatar(offc);
            SetVOEmitter(offc);

            // Retains AvatarAttachment and SkinnedAvatarAttachment type objects within Avatar transform hierarchy
            // for the cloned objects, probably unassigned worn/naked objects after cloned so must remove those?
            AvatarAttachment[] avatarAttachments = offc.Avatar.GetComponentsInChildren<AvatarAttachment>(true);
            SkinnedAvatarAttachment[] skinnedAvatarAttachments = offc.Avatar.GetComponentsInChildren<SkinnedAvatarAttachment>(true);

            Log("Removing unassigned instantiated avatar objects");
            if (avatarAttachments != null && avatarAttachments.Length > 0)
            {
                Log($"Found {avatarAttachments.Length} avatar attachment type objects");
                foreach (AvatarAttachment obj in avatarAttachments)
                {
                    if (obj.Parent == null)
                        UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            if (skinnedAvatarAttachments != null && skinnedAvatarAttachments.Length > 0)
            {
                Log($"Found {skinnedAvatarAttachments.Length} skinned avatar attachment type objects");
                foreach (SkinnedAvatarAttachment obj in skinnedAvatarAttachments)
                {
                    if (obj.Parent == null)
                        UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            yield return GenerateImpostor(offc);

            // Validate because after impostor create
            // the cloned objects refresh the avatar and have it unassigned...
            if (offc.PoliceBelt == null)
            {
                // fetch the correct one from worn objects
                foreach (AvatarObject obj in offc.Avatar.Appearance._wornObjects)
                {
                    if (obj.Id == "policebelt")
                    {
                        offc.PoliceBelt = obj.GetComponent<PoliceBelt>();
                        break;
                    }
                }
            }

            copNet.gameObject.SetActive(false);
            copNet.transform.Find("Avatar").gameObject.SetActive(false);

            buyBustCop = offc;
            buyBustCopID = buyBustCop.transform.root.gameObject.GetInstanceID();
            buyBustCop.Behaviour.ScheduleManager.DisableSchedule();
            if (PoliceOfficer.Officers.Contains(offc))
                PoliceOfficer.Officers.Remove(offc);

            offc.Awareness.SetAwarenessActive(false);

            offc.transform.parent = copNPCParent;
            offc._defaultParent = copNPCParent;

            Log("Done spawning buy bust officer");
            yield break;
        }
    }
}
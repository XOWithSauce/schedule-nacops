using System.Collections;
using UnityEngine;

using static NACops.PrivateInvestigator;
using static NACops.RuntimeImpostor;
using static NACops.NACops;
using static NACops.DebugModule;

#if MONO
using ScheduleOne.Core.Avatar;
using ScheduleOne.Core.Avatar.Properties;
using ScheduleOne.AvatarFramework;
using ScheduleOne.Police;
using ScheduleOne.VoiceOver;
using ScheduleOne.NPCs;
#else
using Il2CppScheduleOne.Core.Avatar;
using Il2CppScheduleOne.Core.Avatar.Properties;
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.VoiceOver;
using Il2CppScheduleOne.NPCs;
using static Il2CppScheduleOne.AvatarFramework.AvatarSettings;
#endif

namespace NACops
{
    public static class AvatarUtility
    {
        #region Avatar randomization constants
        public static readonly List<Color> skinColors = new()
        {
            new Color(0.729412f, 0.596078f, 0.541176f),
            new Color(0.7768f, 0.5931f, 0.4442f),
            new Color(0.364705f, 0.298039f, 0.270588f),
            new Color(0.454902f, 0.372549f, 0.337255f),
            new Color(0.7768f, 0.5931f, 0.4442f)
        };
        public static readonly List<string> randomFaceLayers = new()
        {
            "smugpout",
            "slightsmile",
            "neutralface",
            "openmouthsmile",
            "slightfrown",
        };

        public static readonly List<string> randomMaleHairLayers = new()
        {
            "balding",
            "buzzcut",
        };

        public static readonly List<string> randomFacialHairLayers = new()
        {
            "beardbushy",
            "beardfull",
            "goatee",
            "swirlymustache",
        };
        public static readonly List<string> randomFemaleHairLayers = new()
        {
            "midfringe",
            "lowbun",
            "doubletopknot"
        };
        public static readonly List<Color> randomHairColors = new()
        {
            new Color(0.141176f, 0.109803f, 0.066666f),
            new Color(0.666666f, 0.533333f, 0.4f),
            new Color(0.501960f, 0.501965f, 0.5019607f),
            new Color(0.294117f, 0.196078f, 0.121568f),
            new Color(0.6071f, 0.3886f, 0.087f),
            new Color(0.0804f, 0.0699f, 0.0624f),
            new Color(0.1278f, 0.1278f, 0.1278f)
        };

		public static Dictionary<int, List<Color>> PIColorPalettes = new()
		{
            // Brown Blazer + Cap, blue shirt, gray pants
            {0, new List<Color>()
			{
				new Color(0.396f, 0.396f, 0.396f), // pants
                new Color(0.326f, 0.578f, 0.896f), // shirt
                new Color(0.151f, 0.151f, 0.151f), // sneakers
                new Color(0.613f, 0.493f, 0.344f), // (rand) blazer
                new Color(0.613f, 0.493f, 0.344f)  // (rand) cap
            }},

            // Gray Blazer + Cap, orange shirt, blue pants
            {1, new List<Color>()
			{
				new Color(0.2588f, 0.3647f, 0.5490f), // pants
                new Color(0.7137f, 0.2941f, 0.1568f), // shirt
                Color.black,                          // sneakers
                new Color(0.1372f, 0.1058f, 0.0823f), // (rand) blazer
                new Color(0.1372f, 0.1058f, 0.0823f)  // (rand) cap
            }},

            // Dark Gray Blazer + Light Gray Cap, blue shirt, blue pants
            {2, new List<Color>()
			{
				new Color(0.258f, 0.364f, 0.549f), // pants
                new Color(0.326f, 0.578f, 0.896f), // shirt
                Color.black,                       // sneakers
                new Color(0.121f, 0.094f, 0.078f), // (rand) blazer
                new Color(0.772f, 0.772f, 0.772f)  // (rand) cap
            }},

            // gray Blazer + Light Gray Cap, black shirt, brown pants
            {3, new List<Color>()
			{
				new Color(0.615f, 0.478f, 0.313f), // pants
                Color.black,                       // shirt
                Color.black,                       // sneakers
                new Color(0.396f, 0.396f, 0.396f), // (rand) blazer
                new Color(0.772f, 0.772f, 0.772f)  // (rand) cap
            }},
		};
        #endregion

        public static Dictionary<int, AvatarSettings> instancedSettings = new();

        // Assign VODatabases
        public static List<VODatabase> maleVOs = new();
        public static List<VODatabase> femaleVOs = new();

        public static void SetRandomOfficerAvatar(PoliceOfficer offc)
        {
            Log("Start random");
            
            NakedAppearance nakedAppearance = offc.Avatar.Appearance.AppliedNakedAppearance.Clone();
            SetRandomLook(ref nakedAppearance);
            Log("Load settings");
            offc.Avatar.Appearance.ApplyNakedAppearance(nakedAppearance);
        }

        public static void SetRandomLook(ref NakedAppearance original)
        {
            Log("Set random look");
            List<SerializedAvatarObject> createdObjects = new();

            SerializedColorProperty[] face0Properties = new SerializedColorProperty[1];
            face0Properties[0] = new SerializedColorProperty("MainColor", new Color(0f, 0f, 0f, 1f));
            SerializedAvatarObject face0 = new(randomFaceLayers[UnityEngine.Random.Range(0, randomFaceLayers.Count)], face0Properties);
            createdObjects.Add(face0);

            original.Gender = UnityEngine.Random.Range(0f, 1f);



            SerializedColorProperty[] face2Properties = new SerializedColorProperty[1];
            face2Properties[0] = new SerializedColorProperty("MainColor", new Color(0f, 0f, 0f, 0.8223f));
            SerializedAvatarObject face2 = new("eyeshadow", face2Properties);
            createdObjects.Add(face2);

            if (UnityEngine.Random.Range(0f, 1f) > 0.3f)
            {
                string faceLayerID = "";
                Color faceLayerColor;
                faceLayerID = "oldpersonwrinkles";
                faceLayerColor = new(0f, 0f, 0f, 1f);
                SerializedColorProperty[] face3Properties = new SerializedColorProperty[1];
                face3Properties[0] = new SerializedColorProperty("MainColor", faceLayerColor);
                SerializedAvatarObject face3 = new(faceLayerID, face3Properties);
                createdObjects.Add(face3);
            }

            original.HairColor = randomHairColors[UnityEngine.Random.Range(0, randomHairColors.Count)];

            if (UnityEngine.Random.Range(0f, 1f) > 0.6f && original.Gender < 0.5f)
            {
                SerializedColorProperty[] face1Properties = new SerializedColorProperty[1];
                face1Properties[0] = new SerializedColorProperty("MainColor", original.HairColor);
                SerializedAvatarObject face1 = new(randomFacialHairLayers[UnityEngine.Random.Range(0, randomFacialHairLayers.Count)], face1Properties);
                createdObjects.Add(face1);
            }

            float scale = UnityEngine.Random.Range(1f, 1.1f);
            float thickness = UnityEngine.Random.Range(1f, 1.4f);
            float restHeight = UnityEngine.Random.Range(-1f, -1.40f);
            EyebrowSettings eyebrowSetting = new(scale, thickness, restHeight, 0f, original.HairColor);

            string hairID = "";
            if (original.Gender < 0.5f)
                hairID = randomMaleHairLayers[UnityEngine.Random.Range(0, randomMaleHairLayers.Count)];
            else
                hairID = randomFemaleHairLayers[UnityEngine.Random.Range(0, randomFemaleHairLayers.Count)];
            SerializedColorProperty[] hairProperties = new SerializedColorProperty[1];
            hairProperties[0] = new SerializedColorProperty("MainColor", original.HairColor);
            SerializedAvatarObject hair = new(hairID, hairProperties);
            createdObjects.Add(hair);

            original.Height = UnityEngine.Random.Range(1.66f, 1.85f);
            original.Weight = UnityEngine.Random.Range(0.66f, 0.9f);

            original.RightEyebrowSettings = eyebrowSetting;
            original.LeftEyebrowSettings = eyebrowSetting;
            original.RightEyelidSettings = EyelidSettings.Default;
            original.LeftEyelidSettings = EyelidSettings.Default;
            original.RightEyeSettings = EyeSettings.Default;
            original.LeftEyeSettings = EyeSettings.Default;

            original.SkinColor = skinColors[UnityEngine.Random.Range(0, skinColors.Count)];
            original.AvatarObjects = createdObjects.ToArray();

            Log("Ret face settings");
            return;
        }

        public static IEnumerator GenerateImpostor(PoliceOfficer offc)
        {
            offc.Movement._agent.enabled = false;
            offc.Movement.enabled = false;
            Vector3 origPos = offc.transform.position;
            Quaternion origRot = offc.transform.rotation;

            offc.transform.SetPositionAndRotation(targetPosition, Quaternion.Euler(targetRotationEuler));
            offc.Avatar.Animation.SetGrounded(true);
            yield return Wait1;
            offc.Avatar.Animation.enabled = false;
            offc.Avatar._impostor.Disable();
            offc.Avatar.BodyContainer.gameObject.SetActive(true);
            yield return Wait01;
            Texture2D tex = CreateImpostor(offc);

            offc.Avatar._impostor.Enable();
            offc.transform.SetPositionAndRotation(origPos, origRot);
            offc.Avatar.Animation.enabled = true;
            offc.Movement._agent.enabled = true;
            offc.Movement.enabled = true;

            offc.Avatar.SetImpostorTexture(tex);
            // fixes a bug where the mesh gets extended to the air after
            offc.Avatar.Animation.PlayStandUpAnimation();
            yield break;
        }

        public static IEnumerator PIAvatar(PoliceOfficer offc)
        {
            Log($"Removing unassigned avatar objects countpre: {offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount}");
            Transform target = offc.Avatar.Appearance._avatarObjectsContainer.transform;

            offc.Avatar.Appearance.RemoveAllWornObjects(false);

            List<AvatarObject> destroyable = new();
            for (int j = offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount - 1; j >= 0; j--)
            {
                Transform avatarObjectTr = target.GetChild(j);
                if (avatarObjectTr)
                {
                    AvatarObject comp = avatarObjectTr.gameObject.GetComponent<AvatarObject>();

                    if (comp != null && !offc.Avatar.Appearance._nakedObjects.Contains(comp))
                        destroyable.Add(comp);
                }
            }
            foreach (AvatarObject avObj in destroyable)
            {
                UnityEngine.Object.Destroy(avObj.gameObject);
            }
            Log($"Removing unassigned avatar objects count post: {offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount}");

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
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            if (skinnedAvatarAttachments != null && skinnedAvatarAttachments.Length > 0)
            {
                Log($"Found {skinnedAvatarAttachments.Length} skinned avatar attachment type objects");
                foreach (SkinnedAvatarAttachment obj in skinnedAvatarAttachments)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            Outfit outfit = new();

            List<Color> palette = PIColorPalettes[UnityEngine.Random.Range(0, PIColorPalettes.Count)];

            NakedAppearance nakedAppearance = offc.Avatar.Appearance.AppliedNakedAppearance.Clone();
            SetRandomLook(ref nakedAppearance);
            Log("Load settings");
            offc.Avatar.Appearance.ApplyNakedAppearance(nakedAppearance);

            List<SerializedAvatarObject> randomWornObjects = new();
            bool hasSkirt = false;
            if (offc.Avatar.Appearance.AppliedNakedAppearance.Gender > 0.5f && UnityEngine.Random.Range(0f, 1f) > 0.8f)
            {
                hasSkirt = true;
            }
            else
            {
                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                {
                    SerializedAvatarObject obj = new();
                    obj.Id = "jeans";
                    obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[0]) };
                    randomWornObjects.Add(obj);
                }
                else
                {
                    SerializedAvatarObject obj = new();
                    obj.Id = "cargopants";
                    obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[0]) };
                    randomWornObjects.Add(obj);
                }
            }

            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                SerializedAvatarObject obj = new();
                obj.Id = "buttonupshirtrolled";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[1]) };
                randomWornObjects.Add(obj);
            }
            else
            {
                SerializedAvatarObject obj = new();
                obj.Id = "flannelbuttonupshirt";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[1]) };
                randomWornObjects.Add(obj);
            }

            SerializedAvatarObject sneakers = new();
            sneakers.Id = "sneakers";
            sneakers.Colors = new[] { new SerializedColorProperty("MainColor", palette[2]) };
            randomWornObjects.Add(sneakers);

            if (UnityEngine.Random.Range(0f, 1f) > 0.3f && !hasSkirt)
            {
                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                {
                    SerializedAvatarObject obj = new();
                    obj.Id = "blazer";
                    obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[3]) };
                    randomWornObjects.Add(obj);
                }
                else
                {
                    SerializedAvatarObject obj = new();
                    obj.Id = "collarjacket";
                    obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[3]) };
                    randomWornObjects.Add(obj);
                }
            }

            // check for hair layer not blocking the hat
            bool hasHatBlockingHair = false;
            foreach (AvatarObject obj in offc.Avatar.Appearance._nakedObjects)
            {
                if (obj.Id == "doubletopknot")
                {
                    hasHatBlockingHair = true;
                    break;
                }
            }

            if (UnityEngine.Random.Range(0f, 1f) > 0.75f && !hasHatBlockingHair)
            {
                SerializedAvatarObject obj = new();
                obj.Id = "cap";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[4]) };
                randomWornObjects.Add(obj);
            }
            else if (UnityEngine.Random.Range(0f, 1f) > 0.90f && offc.Avatar.Appearance.AppliedNakedAppearance.Gender < 0.5f && !hasHatBlockingHair)
            {
                SerializedAvatarObject obj = new();
                obj.Id = "beanie";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[4]) };
                randomWornObjects.Add(obj);
            }

            if (hasSkirt)
            {
                SerializedAvatarObject obj = new();
                obj.Id = "skirt";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", palette[0]) };
                randomWornObjects.Add(obj);
            }

            if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
            {
                SerializedAvatarObject obj = new();
                obj.Id = "thelegendsunglasses";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.70f, 0.717f, 0.76f)) };
                randomWornObjects.Add(obj);
            }

            Log("Avatar outfit settings created apply...");
            //offc.Avatar.LoadAvatarSettings(newSettings);
            outfit.AvatarObjects = randomWornObjects.ToArray();
            offc.Avatar.Appearance.ApplyOutfit(outfit);

            offc.NPCData.BasicInfo.FirstName = offc.Avatar.Appearance.AppliedNakedAppearance.Gender < 0.5f ? randomMaleNames[UnityEngine.Random.Range(0, randomMaleNames.Count)] : randomFemaleNames[UnityEngine.Random.Range(0, randomFemaleNames.Count)];

            Log("Create impostor");
            yield return GenerateImpostor(offc);
            yield break;
        }

        public static IEnumerator SetRaiderAvatar(PoliceOfficer offc)
        {
            offc.Avatar.Appearance.RemoveAllWornObjects(false);
            Transform target = offc.Avatar.Appearance._avatarObjectsContainer.transform;

            List<AvatarObject> destroyable = new();
            for (int j = offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount - 1; j >= 0; j--)
            {
                Transform avatarObjectTr = target.GetChild(j);
                if (avatarObjectTr)
                {
                    AvatarObject comp = avatarObjectTr.gameObject.GetComponent<AvatarObject>();

                    if (comp != null && !offc.Avatar.Appearance._nakedObjects.Contains(comp))
                        destroyable.Add(comp);
                }
            }
            foreach (AvatarObject avObj in destroyable)
            {
                UnityEngine.Object.Destroy(avObj.gameObject);
            }
            Log($"Removing unassigned avatar objects count post: {offc.Avatar.Appearance._avatarObjectsContainer.transform.childCount}");

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
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }
            if (skinnedAvatarAttachments != null && skinnedAvatarAttachments.Length > 0)
            {
                Log($"Found {skinnedAvatarAttachments.Length} skinned avatar attachment type objects");
                foreach (SkinnedAvatarAttachment obj in skinnedAvatarAttachments)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            Outfit outfit = new();

            NakedAppearance nakedAppearance = offc.Avatar.Appearance.AppliedNakedAppearance.Clone();
            SetRandomLook(ref nakedAppearance);
            Log("Load settings");
            offc.Avatar.Appearance.ApplyNakedAppearance(nakedAppearance);
            List<SerializedAvatarObject> raiderObjects = new();

            SerializedAvatarObject jeans = new();
            jeans.Id = "jeans";
            jeans.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.063f, 0.102f, 0.141f)) };
            raiderObjects.Add(jeans);

            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                SerializedAvatarObject obj = new();
                obj.Id = "buttonupshirtrolled";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.012f, 0.161f, 0.31f)) };
                raiderObjects.Add(obj);
            }
            else
            {
                SerializedAvatarObject obj = new();
                obj.Id = "flannelbuttonupshirt";
                obj.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.012f, 0.161f, 0.31f)) };
                raiderObjects.Add(obj);
            }

            SerializedAvatarObject glove = new();
            glove.Id = "fingerlessgloves";
            glove.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.290f, 0.313f, 0.349f)) };
            raiderObjects.Add(glove);

            SerializedAvatarObject sneakers = new();
            sneakers.Id = "combatboots";
            sneakers.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.470f, 0.462f, 0.400f)) };
            raiderObjects.Add(sneakers);

            SerializedAvatarObject belt = new();
            belt.Id = "policebelt";
            belt.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.063f, 0.102f, 0.141f)) };
            raiderObjects.Add(belt);

            SerializedAvatarObject vest = new();
            vest.Id = "policetacticalvest";
            vest.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.063f, 0.102f, 0.141f)) };
            raiderObjects.Add(vest);

            SerializedAvatarObject cap = new();
            cap.Id = "policecap";
            cap.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.290f, 0.313f, 0.349f)) };
            raiderObjects.Add(cap);

            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                SerializedAvatarObject glasses = new();
                glasses.Id = "legendsunglasses";
                glasses.Colors = new[] { new SerializedColorProperty("MainColor", new Color(0.70f, 0.717f, 0.76f)) };
                raiderObjects.Add(glasses);
            }

            //offc.Avatar.LoadAvatarSettings(newSettings);
            outfit.AvatarObjects = raiderObjects.ToArray();
            offc.Avatar.Appearance.ApplyOutfit(outfit);

            yield return null;
        }

        public static void SetupVODatabaseRefs()
        {
            VODatabase[] dbs = Resources.FindObjectsOfTypeAll<VODatabase>();
            foreach (VODatabase db in dbs)
            {
                if (maleVOs.Contains(db) || femaleVOs.Contains(db)) continue;

                if (db.name == "Tyler VO" || db.name == "Hippie VO" || db.name == "Redneck VO")
                    maleVOs.Add(db);

                if (db.name == "Female1 VO" || db.name == "Female2 VO" || db.name == "Timid VO")
                    femaleVOs.Add(db);
            }
            Log("Finished settting up VODatabase refs");
        }

        public static void SetVOEmitter(NPC npc)
        {
            if (maleVOs.Count == 0 || femaleVOs.Count == 0)
            {
                maleVOs.Clear();
                femaleVOs.Clear();
                SetupVODatabaseRefs();
            }

            if (npc.Avatar.Appearance.AppliedNakedAppearance.Gender < 0.5f)
            {
                npc.VoiceOverEmitter.SetDatabase(maleVOs[UnityEngine.Random.Range(0, maleVOs.Count)]);
                npc.VoiceOverEmitter.SetDefaultPitch(UnityEngine.Random.Range(0.90f, 0.98f));
            }
            else
            {
                npc.VoiceOverEmitter.SetDatabase(femaleVOs[UnityEngine.Random.Range(0, femaleVOs.Count)]);
                npc.VoiceOverEmitter.SetDefaultPitch(UnityEngine.Random.Range(1.15f, 1.4f));
            }
        }
    }
}

using System.Collections;
using UnityEngine;

using static NACops.PrivateInvestigator;
using static NACops.RuntimeImpostor;
using static NACops.NACops;

#if MONO
using ScheduleOne.AvatarFramework;
using ScheduleOne.Police;
using ScheduleOne.VoiceOver;
using ScheduleOne.NPCs;
using static ScheduleOne.AvatarFramework.AvatarSettings;
#else
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
            "Avatar/Layers/Face/Face_SmugPout",
            "Avatar/Layers/Face/Face_SlightSmile",
            "Avatar/Layers/Face/Face_Neutral",
            "Avatar/Layers/Face/Face_NeutralPout",
        };
        public static readonly List<string> randomMaleHairLayers = new()
        {
            "Avatar/Hair/Balding/Balding",
            "Avatar/Hair/BuzzCut/BuzzCut",
            "Avatar/Hair/Peaked/Peaked",
        };
        public static readonly List<string> randomFacialHairLayers = new()
        {
            "Avatar/Layers/Face/FacialHair_Swirl",
            "Avatar/Layers/Face/FacialHair_Goatee"
        };
        public static readonly List<string> randomFemaleHairLayers = new()
        {
            "Avatar/Hair/MidFringe/MidFringe",
            "Avatar/Hair/LowBun/LowBun",
            "Avatar/Hair/DoubleTopKnot/DoubleTopKnot"
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
				new Color(0.396f, 0.396f, 0.396f), // jeans
                new Color(0.326f, 0.578f, 0.896f), // shirt
                new Color(0.151f, 0.151f, 0.151f), // sneakers
                new Color(0.613f, 0.493f, 0.344f), // (rand) blazer
                new Color(0.613f, 0.493f, 0.344f)  // (rand) cap
            }},

            // Gray Blazer + Cap, orange shirt, blue pants
            {1, new List<Color>()
			{
				new Color(0.2588f, 0.3647f, 0.5490f), // jeans
                new Color(0.7137f, 0.2941f, 0.1568f), // shirt
                Color.black,                          // sneakers
                new Color(0.1372f, 0.1058f, 0.0823f), // (rand) blazer
                new Color(0.1372f, 0.1058f, 0.0823f)  // (rand) cap
            }},

            // Dark Gray Blazer + Light Gray Cap, blue shirt, blue pants
            {2, new List<Color>()
			{
				new Color(0.258f, 0.364f, 0.549f), // jeans
                new Color(0.326f, 0.578f, 0.896f), // shirt
                Color.black,                       // sneakers
                new Color(0.121f, 0.094f, 0.078f), // (rand) blazer
                new Color(0.772f, 0.772f, 0.772f)  // (rand) cap
            }},

            // gray Blazer + Light Gray Cap, black shirt, brown pants
            {3, new List<Color>()
			{
				new Color(0.615f, 0.478f, 0.313f), // jeans
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

        public static void SetRandomAvatar(PoliceOfficer offc, ref AvatarSettings createdSettings)
        {
            AvatarSettings newSettings = ScriptableObject.CreateInstance<AvatarSettings>();


            var originalAccessorySettings = offc.Avatar.CurrentSettings.AccessorySettings;
            var originalBodySettings = offc.Avatar.CurrentSettings.BodyLayerSettings;
#if MONO
            List<LayerSetting> faceSettings = SetRandomLook(newSettings);

            newSettings.AccessorySettings = new(originalAccessorySettings);
            newSettings.BodyLayerSettings = new(originalBodySettings);
            newSettings.FaceLayerSettings = faceSettings;
#else
            Il2CppSystem.Collections.Generic.List<LayerSetting> faceSettings = SetRandomLook(newSettings);

            newSettings.AccessorySettings = new();
            for (int i = 0; i < originalAccessorySettings.Count; i++)
                newSettings.AccessorySettings.Add(originalAccessorySettings[i]);

            newSettings.BodyLayerSettings = new();
            for (int i = 0; i < originalBodySettings.Count; i++)
                newSettings.BodyLayerSettings.Add(originalBodySettings[i]);
#endif

            newSettings.FaceLayerSettings = faceSettings;
            offc.Avatar.LoadAvatarSettings(newSettings);

            int id = offc.transform.root.gameObject.GetInstanceID();
            if (instancedSettings.ContainsKey(id))
            {
                if (instancedSettings[id] != null)
                    UnityEngine.Object.DestroyImmediate(instancedSettings[id]);

                instancedSettings[id] = newSettings;
            }
            else
                instancedSettings.Add(offc.transform.root.gameObject.GetInstanceID(), newSettings);

            createdSettings = newSettings;
        }

#if MONO
        public static List<LayerSetting> SetRandomLook(AvatarSettings newSettings)
#else
        public static Il2CppSystem.Collections.Generic.List<LayerSetting> SetRandomLook(AvatarSettings newSettings)
#endif
        {
#if MONO
            List<LayerSetting> faceSettings = new();
            for (int i = 0; i < 6; i++) faceSettings.Add(new LayerSetting() { layerPath = "", layerTint = Color.white });

#else
            var faceSettings = new Il2CppSystem.Collections.Generic.List<LayerSetting>();
            for (int i = 0; i < 6; i++) faceSettings.Add(new LayerSetting() { layerPath = "", layerTint = Color.white });
#endif

            var face0 = faceSettings[0];
            face0.layerPath = randomFaceLayers[UnityEngine.Random.Range(0, randomFaceLayers.Count)];
            face0.layerTint = new Color(0f, 0f, 0f, 1f);
            faceSettings[0] = face0;

            newSettings.Gender = (float)UnityEngine.Random.Range(0, 2);

            if (UnityEngine.Random.Range(0f, 1f) > 0.6f && newSettings.Gender < 0.5f)
            {
                var face1 = faceSettings[1];
                face1.layerPath = randomFacialHairLayers[UnityEngine.Random.Range(0, randomFacialHairLayers.Count)];
                face1.layerTint = new Color(0f, 0f, 0f, 1f);
                faceSettings[1] = face1;
            }

            var face3 = faceSettings[3];
            face3.layerPath = "Avatar/Layers/Face/EyeShadow";
            face3.layerTint = new Color(0f, 0f, 0f, 0.96f);
            faceSettings[3] = face3;

            if (UnityEngine.Random.Range(0f, 1f) > 0.3f)
            {
                var face4 = faceSettings[4];
                string faceLayerPath = "";
                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                    faceLayerPath = "Avatar/Layers/Face/OldPersonWrinkles";
                else
                    faceLayerPath = "Avatar/Layers/Face/Freckles";
                face4.layerPath = faceLayerPath;
                face4.layerTint = new Color(0f, 0f, 0f, 0.55f);
                faceSettings[4] = face4;
            }

            newSettings.UseCombinedLayer = false;

            newSettings.EyebrowScale = UnityEngine.Random.Range(1f, 1.1f);
            newSettings.EyebrowThickness = UnityEngine.Random.Range(1f, 1.4f);
            newSettings.EyebrowRestingHeight = UnityEngine.Random.Range(-1f, -1.40f);
            newSettings.EyeBallTint = new Color(1f, 1f, 1f);
            newSettings.EyeballMaterialIdentifier = "Default";
            newSettings.Height = UnityEngine.Random.Range(0.96f, 1.08f);
            newSettings.HairColor = randomHairColors[UnityEngine.Random.Range(0, randomHairColors.Count)];
            if (newSettings.Gender < 0.5f)
                newSettings.HairPath = randomMaleHairLayers[UnityEngine.Random.Range(0, randomMaleHairLayers.Count)];
            else
                newSettings.HairPath = randomFemaleHairLayers[UnityEngine.Random.Range(0, randomFemaleHairLayers.Count)];
            newSettings.Weight = UnityEngine.Random.Range(0.3f, 0.7f);
            newSettings.PupilDilation = 0.55f;
            newSettings.RightEyeLidColor = new Color(0.4118f, 0.3216f, 0.2471f);
            newSettings.LeftEyeLidColor = new Color(0.4118f, 0.3216f, 0.2471f);
            newSettings.RightEyeRestingState = new Eye.EyeLidConfiguration() { bottomLidOpen = 0.2719f, topLidOpen = 0.4313f };
            newSettings.LeftEyeRestingState = new Eye.EyeLidConfiguration() { bottomLidOpen = 0.2719f, topLidOpen = 0.4313f };
            newSettings.SkinColor = skinColors[UnityEngine.Random.Range(0, skinColors.Count)];
            return faceSettings;
        }

        public static IEnumerator GenerateImpostor(PoliceOfficer offc, AvatarSettings settings)
        {
            offc.Movement.Agent.enabled = false;
            offc.Movement.enabled = false;
            Vector3 origPos = offc.transform.position;
            Quaternion origRot = offc.transform.rotation;

            offc.transform.SetPositionAndRotation(targetPosition, Quaternion.Euler(targetRotationEuler));
            offc.Avatar.Animation.SetGrounded(true);
            yield return Wait1;
            offc.Avatar.Animation.enabled = false;
            offc.Avatar.Impostor.DisableImpostor();
            offc.Avatar.BodyContainer.gameObject.SetActive(true);
            yield return Wait01;
            Texture2D tex = CreateImpostor(offc);

            offc.Avatar.Impostor.EnableImpostor();
            offc.transform.SetPositionAndRotation(origPos, origRot);
            offc.Avatar.Animation.enabled = true;
            offc.Movement.Agent.enabled = true;
            offc.Movement.enabled = true;

            settings.ImpostorTexture = tex;
            offc.Avatar.Impostor.SetAvatarSettings(settings);
            // fixes a bug where the mesh gets extended to the air after
            offc.Avatar.Animation.PlayStandUpAnimation();
            yield break;
        }
        public static IEnumerator PIAvatar(PoliceOfficer offc)
        {
            AvatarSettings newSettings = ScriptableObject.CreateInstance<AvatarSettings>();

            var originalAccessorySettings = offc.Avatar.CurrentSettings.AccessorySettings;
            var originalBodySettings = offc.Avatar.CurrentSettings.BodyLayerSettings;

#if MONO
            List<LayerSetting> bodySettings = new();
            for (int i = 0; i < 6; i++) bodySettings.Add(new LayerSetting() { layerPath = "", layerTint = Color.white });

            List<AccessorySetting> accessorySettings = new();
            for (int i = 0; i < 9; i++) accessorySettings.Add(new AccessorySetting() { path = "", color = Color.white });

#else
            var bodySettings = new Il2CppSystem.Collections.Generic.List<LayerSetting>();
            for (int i = 0; i < 6; i++) bodySettings.Add(new LayerSetting() { layerPath = "", layerTint = Color.white });

            var accessorySettings = new Il2CppSystem.Collections.Generic.List<AccessorySetting>();
            for (int i = 0; i < 9; i++) accessorySettings.Add(new AccessorySetting() { path = "", color = Color.white });
#endif

            List<Color> palette = PIColorPalettes[UnityEngine.Random.Range(0, PIColorPalettes.Count)];
            newSettings.FaceLayerSettings = SetRandomLook(newSettings);

            bool hasSkirt = false;
            if (newSettings.Gender > 0.5f && UnityEngine.Random.Range(0f, 1f) > 0.3f)
            {
                hasSkirt = true;
            }
            else
            {
                var jeans = bodySettings[2];
                jeans.layerPath = "Avatar/Layers/Bottom/Jeans";
                jeans.layerTint = palette[0];
                bodySettings[2] = jeans;
            }

            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                var shirt = bodySettings[3];
                shirt.layerPath = "Avatar/Layers/Top/RolledButtonUp";
                shirt.layerTint = palette[1];
                bodySettings[3] = shirt;
            }
            else
            {
                var shirt = bodySettings[3];
                shirt.layerPath = "Avatar/Layers/Top/FlannelButtonUp";
                shirt.layerTint = palette[1];
                bodySettings[3] = shirt;
            }

            var sneakers = accessorySettings[0];
            sneakers.path = "Avatar/Accessories/Feet/Sneakers/Sneakers";
            sneakers.color = palette[2];
            accessorySettings[0] = sneakers;

            if (UnityEngine.Random.Range(0f, 1f) > 0.3f && !hasSkirt)
            {
                var blazer = accessorySettings[2];
                blazer.path = "Avatar/Accessories/Chest/Blazer/Blazer";
                blazer.color = palette[3];
                accessorySettings[2] = blazer;
            }

            if (UnityEngine.Random.Range(0f, 1f) > 0.75f && newSettings.HairPath != "Avatar/Hair/DoubleTopKnot/DoubleTopKnot")
            {
                var cap = accessorySettings[3];
                cap.path = "Avatar/Accessories/Head/Cap/Cap";
                cap.color = palette[4];
                accessorySettings[3] = cap;
            }
            else if (UnityEngine.Random.Range(0f, 1f) > 0.90f && newSettings.Gender < 0.5f)
            {
                var beanie = accessorySettings[3];
                beanie.path = "Avatar/Accessories/Head/Beanie/Beanie";
                beanie.color = palette[4];
                accessorySettings[3] = beanie;
                newSettings.HairPath = "";
            }

            if (hasSkirt)
            {
                var skirt = accessorySettings[4];
                skirt.path = "Avatar/Accessories/Bottom/MediumSkirt/MediumSkirt";
                skirt.color = palette[0];
                accessorySettings[4] = skirt;
            }

            if (UnityEngine.Random.Range(0f, 1f) > 0.8f)
            {
                var glasses = accessorySettings[5];
                glasses.path = "Avatar/Accessories/Head/LegendSunglasses/LegendSunglasses";
                glasses.color = new Color(0.70f, 0.717f, 0.76f);
                accessorySettings[5] = glasses;
            }

            newSettings.AccessorySettings = accessorySettings;
            newSettings.BodyLayerSettings = bodySettings;

            offc.Avatar.LoadAvatarSettings(newSettings);

            offc.NPCData.BasicInfo.FirstName = newSettings.Gender < 0.5f ? randomMaleNames[UnityEngine.Random.Range(0, randomMaleNames.Count)] : randomFemaleNames[UnityEngine.Random.Range(0, randomFemaleNames.Count)];

            int id = offc.transform.root.gameObject.GetInstanceID();
            if (instancedSettings.ContainsKey(id))
            {
                if (instancedSettings[id] != null) 
                    UnityEngine.Object.DestroyImmediate(instancedSettings[id]);

                instancedSettings[id] = newSettings;
            }
            else
                instancedSettings.Add(offc.transform.root.gameObject.GetInstanceID(), newSettings);

            yield return GenerateImpostor(offc, newSettings);
            yield break;
        }

        public static IEnumerator SetRaiderAvatar(PoliceOfficer offc)
        {
            AvatarSettings newSettings = ScriptableObject.CreateInstance<AvatarSettings>();

            var originalAccessorySettings = offc.Avatar.CurrentSettings.AccessorySettings;
            var originalBodySettings = offc.Avatar.CurrentSettings.BodyLayerSettings;

#if MONO
            List<LayerSetting> bodySettings = new();
            for (int i = 0; i < 6; i++) bodySettings.Add(new LayerSetting() { layerPath = "", layerTint = Color.white });

            List<AccessorySetting> accessorySettings = new();
            for (int i = 0; i < 9; i++) accessorySettings.Add(new AccessorySetting() { path = "", color = Color.white });

#else
            var bodySettings = new Il2CppSystem.Collections.Generic.List<LayerSetting>();
            for (int i = 0; i < 6; i++) bodySettings.Add(new LayerSetting() { layerPath = "", layerTint = Color.white });

            var accessorySettings = new Il2CppSystem.Collections.Generic.List<AccessorySetting>();
            for (int i = 0; i < 9; i++) accessorySettings.Add(new AccessorySetting() { path = "", color = Color.white });
#endif

            var jeans = bodySettings[2];
            jeans.layerPath = "Avatar/Layers/Bottom/Jeans";
            jeans.layerTint = new Color(0.063f, 0.102f, 0.141f);
            bodySettings[2] = jeans;

            var shirt = bodySettings[3];
            shirt.layerPath = "Avatar/Layers/Top/RolledButtonUp";
            shirt.layerTint = new Color(0.012f, 0.161f, 0.31f);
            bodySettings[3] = shirt;

            var glove = bodySettings[4];
            glove.layerPath = "Avatar/Layers/Accessories/FingerlessGloves";
            glove.layerTint = new Color(0.290f, 0.313f, 0.349f);
            bodySettings[4] = glove;

            var sneakers = accessorySettings[0];
            sneakers.path = "Avatar/Accessories/Feet/CombatBoots/CombatBoots";
            sneakers.color = new Color(0.470f, 0.462f, 0.400f);
            accessorySettings[0] = sneakers;

            var belt = accessorySettings[1];
            belt.path = "Avatar/Accessories/Waist/PoliceBelt/PoliceBelt";
            belt.color = new Color(0.063f, 0.102f, 0.141f);
            accessorySettings[1] = belt;

            var vest = accessorySettings[2];
            vest.path = "Avatar/Accessories/Chest/BulletproofVest/BulletproofVest_Police";
            vest.color = new Color(0.063f, 0.102f, 0.141f);
            accessorySettings[2] = vest;

            var cap = accessorySettings[3];
            cap.path = "Avatar/Accessories/Head/PoliceCap/PoliceCap";
            cap.color = new Color(0.290f, 0.313f, 0.349f);
            accessorySettings[3] = cap;

            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                var glasses = accessorySettings[5];
                glasses.path = "Avatar/Accessories/Head/LegendSunglasses/LegendSunglasses";
                glasses.color = new Color(0.70f, 0.717f, 0.76f);
                accessorySettings[5] = glasses;
            }

            newSettings.FaceLayerSettings = SetRandomLook(newSettings);
            newSettings.AccessorySettings = accessorySettings;
            newSettings.BodyLayerSettings = bodySettings;

            offc.Avatar.LoadAvatarSettings(newSettings);
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
            DebugModule.Log("Finished settting up VODatabase refs");
        }

        public static void SetVOEmitter(NPC npc)
        {
            if (maleVOs.Count == 0 || femaleVOs.Count == 0)
            {
                maleVOs.Clear();
                femaleVOs.Clear();
                SetupVODatabaseRefs();
            }

            if (npc.Avatar.CurrentSettings.Gender < 0.5f)
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
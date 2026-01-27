


using UnityEngine;

#if MONO
using ScheduleOne.Police;
#else
using Il2CppScheduleOne.Police;
#endif

namespace NACopsV1
{

    public static class RuntimeImpostor
    {
        private static Vector3 cameraPosition = new Vector3(-1.95f, 501f, 0f);
        private static Vector3 cameraRotationEuler = new Vector3(0f, 90f, 0f);
        public static Vector3 targetPosition = new Vector3(0f, 500f, 0f);
        public static Vector3 targetRotationEuler = new Vector3(0f, 0f, 0f);
        private static Vector3 impostorLightPos = new Vector3(-5f, 504f, 0f);
        private static Vector3 impostorLightRot = new Vector3(0f, 0f, 90f);

        // int object id texture
        public static Dictionary<int, Texture2D> createdTextures = new();
        public static Texture2D CreateImpostor(PoliceOfficer officer)
        {
            Texture2D tex = null;
            GameObject tempLight = new GameObject("TempLight");
            tempLight.transform.SetPositionAndRotation(impostorLightPos, Quaternion.Euler(impostorLightRot));
            Light lightComp = tempLight.AddComponent<Light>();
            lightComp.range = 20f;
            lightComp.intensity = 5f;

            GameObject tempCam = new GameObject("TempCamera");
            tempCam.transform.SetPositionAndRotation(cameraPosition, Quaternion.Euler(cameraRotationEuler));
            Camera camera = tempCam.AddComponent<Camera>();
            camera.enabled = false;
            camera.tag = "Untagged";

            camera.cullingMask = (1 << 11) | (1 << 0);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f , 0f);

            camera.targetTexture = RenderTexture.GetTemporary(128, 128, 24, RenderTextureFormat.ARGB32);
            camera.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;

            tex = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
            tex.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(camera.targetTexture);
            camera.targetTexture = null;

            UnityEngine.Object.Destroy(tempCam);
            UnityEngine.Object.Destroy(tempLight);
            createdTextures.Add(officer.GetInstanceID(), tex);

            return tex;
        }

    }

}
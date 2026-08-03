


using MelonLoader;
using System.Collections;
using UnityEngine;

using static NACops.DebugModule;
using static NACops.NACops;
using static NACops.MassSurveillance;

#if MONO
using ScheduleOne.Combat;
using ScheduleOne.Tools;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Vision;
using ScheduleOne.DevUtilities;
using ScheduleOne.Levelling;
using VLB;
#else
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vision;
using Il2CppScheduleOne.DevUtilities;
using Il2CppVLB;
using Il2CppInterop.Runtime.Injection;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Law;
#endif


namespace NACops
{
    public enum ECameraType
    {
        Unidirectional, Omnidirectional
    }
    public enum ECameraDisableAccess
    {
        None, BusinessComputer
    }

#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    public class HylandFlockInstance : MonoBehaviour
    {
#if IL2CPP
        public HylandFlockInstance(IntPtr ptr) : base(ptr) { }

        public HylandFlockInstance() : base(ClassInjector.DerivedConstructorPointer<HylandFlockInstance>())
            => ClassInjector.DerivedConstructorBody(this);
#endif

        public static readonly float LINE_WIDTH_MIN = 0.0005f;
        public static readonly float LINE_WIDTH_MAX = 0.015f;
        public static readonly int RAYCAST_STEPS = 3; // vision checks for centerpos -> eyepos
        public static readonly float SEEN_CACHE_LIFETIME = 120f; // To give it a cumulative effect in addition to being probabilistic
        private FlockInstanceRunner _runner;
        public bool IsActive = false;
        public bool IsBroken = false;
        public bool IsPlayerNearby = false;
        public bool IsOnCooldown = false;
        public float cooldownElapsed = 0f;
        public float cacheLifetimeElapsed = 0f;
        // Incremented each time evidence already exists in the
        // seen state cache to provide extra weight for consecutive crimes
        public float cacheEvidenceRatio = 0f;


        public FlockActivationZone activationZone;
        public Light cameraLight;
        public LineRenderer lineRenderer;
        public GameObject fxParticles;
        public Rigidbody rb;
        public BoxCollider bc;
        public PhysicsDamageable damageable;

        // Track during the seen window visual states
        public List<string> cameraSeenStateCache = new();

        // while raycasting needed
        public Vector3 toPlayerCenter;
        public Vector3 toPlayerEyes;
        public Vector3 currentCastPos;

        public ECameraType type;
        public ECameraDisableAccess disableType;
        public bool activeToday = false;
        public bool isPlayerSighted = false;
        public float consecutiveHits = 0f;
        public bool isOffline = false;
        public void Awake()
        {
            this.gameObject.GetOrAddComponent<Light>();
            this.gameObject.GetOrAddComponent<Rigidbody>();
            this.gameObject.GetOrAddComponent<BoxCollider>();
            this.gameObject.GetOrAddComponent<PhysicsDamageable>();
        }

        public void Initialize()
        {
            _runner = new(instance: this);

            Transform cameraHead = null;
            Transform lastTransformObjectInCam = null;
            Log($"Setup camera {this.transform.GetScenePath()}");
            if (this.gameObject == null)
            {
                Log("Something went wrong while initializing camera!");
                return;
            }

            if (this.gameObject.GetComponent<Light>() == null)
                this.gameObject.AddComponent<Light>();

            if (this.gameObject.GetComponent<Rigidbody>() == null)
                this.gameObject.AddComponent<Rigidbody>();

            if (this.gameObject.GetComponent<BoxCollider>() == null)
                this.gameObject.AddComponent<BoxCollider>();

            if (this.gameObject.GetComponent<PhysicsDamageable>() == null)
                this.gameObject.AddComponent<PhysicsDamageable>();

            if (type == ECameraType.Unidirectional && this.transform.parent != null && this.transform.parent.childCount > 0)
            {
                // For unidirectional camera find the camera head part of the transform and align self
                Transform trTemp = this.transform.parent;
                if (trTemp != null)
                {
                    while (trTemp.childCount > 0)
                        trTemp = trTemp.GetChild(0);
                    lastTransformObjectInCam = trTemp;
                }
                else
                {
                    Log($"Failed to find transform parent of Unidirectional camera");
                }

            }
            cameraHead = lastTransformObjectInCam;
            if (type == ECameraType.Unidirectional)
            {
                if (cameraHead == null)
                {
                    Log($"Failed to instantiate camera, missing direction reference");
                    return;
                }
                // For Unidirectional align with the forward facing glass part of the cam
                Vector3 compPos = cameraHead.transform.position + Vector3.up * 0.04f + cameraHead.forward * 0.32f;
                this.transform.SetPositionAndRotation(compPos, cameraHead.rotation);
            }
            else if (type == ECameraType.Omnidirectional)
            {
                // for Omnidirectional just under the bottom ppart of sphere
                this.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                this.transform.localPosition = new Vector3(0f, -0.33f, 0.45f);
            }

            cameraLight = this.gameObject.GetComponent<Light>();
            cameraLight.intensity = 0f;
            cameraLight.range = 0.25f;
            cameraLight.color = Color.blue;

            GameObject lineRendererObj = new("LineRenderer");
            lineRendererObj.transform.SetParent(this.transform);
            if (type == ECameraType.Unidirectional)
            {
                lineRendererObj.transform.position = cameraHead.transform.position + Vector3.up * 0.04f + cameraHead.forward * 0.27f;
            }
            else if (type == ECameraType.Omnidirectional)
            {
                lineRendererObj.transform.position = this.transform.position;
            }
            lineRendererObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            lineRenderer = lineRendererObj.AddComponent<LineRenderer>();
            lineRenderer.material = lineRendererMaterial;
            lineRenderer.widthMultiplier = LINE_WIDTH_MIN;
            lineRenderer.SetPosition(0, lineRendererObj.transform.position);

            GameObject zoneObj = new("ActivationZone");
            zoneObj.transform.SetParent(activationZoneParent);
            activationZone = zoneObj.AddComponent<FlockActivationZone>();
            activationZone.transform.position = this.transform.position;
            activationZone.parentInstance = this;
            activationZone.gameObject.SetActive(false);

            rb = this.gameObject.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            bc = this.gameObject.GetComponent<BoxCollider>();
            bc.size = new Vector3(0.45f, 0.45f);

            damageable = this.gameObject.GetComponent<PhysicsDamageable>();
            damageable.Rb = rb;

#if MONO
            damageable.onImpacted = (Action<Impact>)OnCameraImpacted;
#else
            damageable.onImpacted = (Il2CppSystem.Action<Impact>)OnCameraImpacted;
#endif
        }

        public void Update()
        {
            if (!registered || !IsActive || IsBroken) return;

            if (cameraSeenStateCache.Count > 0)
            {
                if (consecutiveHits == 0f)
                {
                    cacheLifetimeElapsed += Time.deltaTime;
                    if (cacheLifetimeElapsed >= SEEN_CACHE_LIFETIME)
                    {
                        ClearSeenCache();
                    }
                }
            }

            if (IsOnCooldown)
            {
                cooldownElapsed += Time.deltaTime;
                if (cooldownElapsed >= surveillanceConfig.CameraNoticeCooldown)
                {
                    cooldownElapsed = 0f;
                    IsOnCooldown = false;
                }

                return;
            }

            if (!IsPlayerNearby || !isPlayerSighted) return;

            lineRenderer.SetPosition(1, Player.Local.Avatar.CenterPoint);
        }

        public void ActivateInstance()
        {
            activeToday = true;
            IsActive = true;
            IsBroken = false;
            IsPlayerNearby = false;
            IsOnCooldown = false;
            cooldownElapsed = 0f;
            cameraLight.intensity = 3f;
            activationZone.gameObject.SetActive(true);
            coros.Add(MelonCoroutines.Start(_runner.SearchForPlayer()));
            coros.Add(MelonCoroutines.Start(_runner.HandleLight()));
            coros.Add(MelonCoroutines.Start(_runner.HandleLineWidth()));
            return;
        }

        public void DeactivateInstance()
        {
            if (IsBroken)
            {
                fxParticles.SetActive(false);
                RandomIntervalEvent randomEvent = fxParticles.GetComponent<RandomIntervalEvent>();
                if (randomEvent.enabled)
                    randomEvent.enabled = false;
            }

            activeToday = false;
            IsActive = false;
            activationZone.gameObject.SetActive(false);
            IsBroken = false;
            IsPlayerNearby = false;
            IsOnCooldown = false;
            cooldownElapsed = 0f;
            consecutiveHits = 0f;
            cacheLifetimeElapsed = 0f;
            cacheEvidenceRatio = 0f;
            cameraSeenStateCache.Clear();
            cameraLight.intensity = 0f;
            return;
        }

        public void SetPlayerNearby(bool isNearby)
        {
            if (!registered) return;

            IsPlayerNearby = isNearby;
            if (lineRenderer != null)
                lineRenderer.loop = isNearby;
        }

        public bool RaycastPlayer()
        {
            if (isEvaluatingRaycast) return false;
            isEvaluatingRaycast = true;

            float castRange = surveillanceConfig.CameraActivationRange;
            float distToPlayer = Vector3.Distance(this.transform.position, Player.Local.Avatar.CenterPoint);
            
            toPlayerCenter = Player.Local.Avatar.CenterPoint - this.transform.position;
            toPlayerEyes = Player.Local.EyePosition - this.transform.position;
            float angleToPlayer = Vector3.Angle(this.transform.forward, toPlayerCenter);

            Log("ANGLE: " + angleToPlayer.ToString());
            bool shouldCast = true;
            switch (type)
            {
                case ECameraType.Unidirectional:
                    if (angleToPlayer > 55f)
                        shouldCast = false;
                    break;

                case ECameraType.Omnidirectional:
                    //if (angleToPlayer > 70f)
                        //shouldCast = false;
                    break;
            }
            bool castHits = true;
            if (shouldCast)
            {
                for (int i = 0; i < RAYCAST_STEPS; i++)
                {
                    currentCastPos = Vector3.Lerp(toPlayerEyes, toPlayerCenter, i / RAYCAST_STEPS - 1);
                    int hitsFound = Physics.RaycastNonAlloc(this.transform.position, currentCastPos, raycastHitBuffer, castRange, raycastIgnoreZone);
                    Array.Sort(raycastHitBuffer, 0, hitsFound, raycastCompare);

                    for (int j = 0; j < hitsFound; j++)
                    {
                        RaycastHit hit = raycastHitBuffer[j];

                        if ((obstacleLayer & 1 << hit.collider.gameObject.layer) != 0)
                        {
                            castHits = false;
                            break;
                        }
                        else if (hit.collider.gameObject.layer == playerLayer)
                        {
                            //Log("hits player");
                            castHits = true;
                            break;
                        }
                    }

                    if (castHits) break;
                }
            }
            else
                castHits = false;


            isEvaluatingRaycast = false;
            return castHits;
        }

        public void CaptureEntityVisualState()
        {
            foreach (EntityVisualState entityVisualState in Player.Local.VisibilityComponent.VisualStates)
            {
                if (entityVisualState.label == "Visible") continue;

                if (!cameraSeenStateCache.Contains(entityVisualState.label))
                    cameraSeenStateCache.Add(entityVisualState.label);
                else
                    cacheEvidenceRatio += UnityEngine.Random.Range(0.001f, 0.0001f);
            }
            return;
        }
       
        public void OnCameraImpacted(Impact impact)
        {
            Log($"Camera impacted with:Type {impact.ImpactType} Dmg {impact.ImpactDamage}");
            if (!IsActive || IsBroken) return;
            IsBroken = true;

            if (fxSparksTemplate == null)
            {
                Log("FX template object is missing reference");
                return;
            }

            if (fxParticles == null)
                fxParticles = UnityEngine.Object.Instantiate(fxSparksTemplate);

            fxParticles.transform.SetParent(this.transform);
            fxParticles.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
            fxParticles.SetActive(true);
            RandomIntervalEvent randomEvent = fxParticles.GetComponent<RandomIntervalEvent>();
            if (!randomEvent.enabled)
                randomEvent.enabled = true;

            lineRenderer.forceRenderingOff = true;
            cameraLight.intensity = 0f;

            NetworkSingleton<LevelManager>.Instance.AddXP(50);
            return;
        }

        public void ClearSeenCache()
        {
            Log("Clear seen states");
            cacheLifetimeElapsed = 0f;
            cacheEvidenceRatio = 0f;
            cameraSeenStateCache.Clear();
        }

        // Whenever the camera is tied to a business, it can be disabled from the computer
        public void SetupCameraDisableAccess()
        {
            if (disableType == ECameraDisableAccess.None)
                return;

            // Get 2nd from root.tr then from that call GetComponent business -> check is owned

            // TODO how to modify the computer UI
            // add button that takes the camera "offline"?
            // on pressed -> remove self from the listed ones so that it cant be selected
            // and then if activE deactive + remove from active listed ones
            isOffline = true;
        }
    }

    public class FlockInstanceRunner
    {
        private HylandFlockInstance _instance;
        public FlockInstanceRunner(HylandFlockInstance instance) { this._instance = instance; }
        public IEnumerator SearchForPlayer()
        {
            for (; ; )
            {
                if (!_instance.IsPlayerNearby || _instance.IsOnCooldown)
                {
                    yield return Wait05;
                    if (!registered || !_instance.IsActive || _instance.IsBroken) yield break;
                    if (_instance.consecutiveHits > 0.0f)
                        _instance.consecutiveHits = Mathf.Clamp(_instance.consecutiveHits - 0.1f, 0f, surveillanceConfig.CameraNoticeSpeed);
                    continue;
                }

                bool hits = false;
                while (_instance.IsActive)
                {
                    yield return Wait05;
                    if (!registered || !_instance.IsActive || _instance.IsBroken) yield break;
                    if (!_instance.IsPlayerNearby)
                        break;
                    hits = _instance.RaycastPlayer();
                    if (hits)
                        break;
                    if (_instance.consecutiveHits > 0.0f)
                        _instance.consecutiveHits = Mathf.Clamp(_instance.consecutiveHits - 0.1f, 0f, surveillanceConfig.CameraNoticeSpeed);
                }

                _instance.lineRenderer.SetPosition(1, Player.Local.Avatar.CenterPoint);
                _instance.isPlayerSighted = true;
                _instance.lineRenderer.forceRenderingOff = false;

                float nextStateCapture = 0.5f;
                float currentVisibility = Player.Local.VisibilityComponent.CurrentVisibility;
                _instance.CaptureEntityVisualState();

                while (hits)
                {
                    yield return Wait01;
                    if (!registered || !_instance.IsActive || _instance.IsBroken) yield break;
                    if (!_instance.IsPlayerNearby) break;
                    hits = _instance.RaycastPlayer();

                    // At low visibility e.g. night time or effects its probabilistic
                    if (UnityEngine.Random.Range(0f, 90f) < currentVisibility)
                        _instance.consecutiveHits += 0.1f;

                    if (_instance.consecutiveHits >= nextStateCapture)
                    {
                        _instance.CaptureEntityVisualState();
                        nextStateCapture += 0.5f;
                        currentVisibility = Player.Local.VisibilityComponent.CurrentVisibility;
                    }

                    if (_instance.consecutiveHits >= surveillanceConfig.CameraNoticeSpeed)
                    {
                        Log("Exceeded max time in sight");
                        break;
                    }
                }
                Log($"Got {_instance.consecutiveHits} time in sight!");
                if (_instance.consecutiveHits >= surveillanceConfig.CameraNoticeSpeed)
                {
                    OnCameraFullyNoticed(_instance.cameraSeenStateCache, _instance.cacheEvidenceRatio);
                    _instance.IsOnCooldown = true;
                    _instance.consecutiveHits = 0f;
                }
                _instance.isPlayerSighted = false;
                _instance.lineRenderer.forceRenderingOff = true;
            }
        }

        public IEnumerator HandleLight()
        {
            float startIntensityPassive = 0.5f;
            float endIntensityPassive = 3f;

            float endIntensityActive = 5f;

            float startRangeActive = 0.25f;
            float endRangeActive = 0.5f;

            float duration = surveillanceConfig.CameraNoticeSpeed;
            float elapsed = 0f;
            for (; ; )
            {
                yield return Wait1;
                if (!registered || !_instance.IsActive) yield break;

                if (!_instance.IsPlayerNearby) continue;

                // state not sighted just static blue or pulsing with intensity?
                while (!_instance.isPlayerSighted)
                {
                    // Reset to initial both on intensity and range
                    if (!Mathf.Approximately(_instance.cameraLight.intensity, startIntensityPassive) || !Mathf.Approximately(_instance.cameraLight.range, startRangeActive))
                    {
                        float currentIntensity = _instance.cameraLight.intensity;
                        float currentRange = _instance.cameraLight.range;
                        elapsed = 0f;
                        while (elapsed < 1f && registered && !_instance.isPlayerSighted && !_instance.IsBroken && _instance.IsActive && _instance.IsPlayerNearby)
                        {
                            float t = elapsed / 1f;
                            _instance.cameraLight.intensity = Mathf.Lerp(currentIntensity, startIntensityPassive, t);
                            _instance.cameraLight.range = Mathf.Lerp(currentRange, startRangeActive, t);
                            elapsed += Time.deltaTime;
                            yield return frameEnd;
                        }
                        if (!registered || _instance.IsBroken || !_instance.IsActive) yield break;
                        if (_instance.isPlayerSighted || !_instance.IsPlayerNearby) break;
                    }

                    elapsed = 0f;
                    while (elapsed < duration && registered && !_instance.isPlayerSighted && !_instance.IsBroken && _instance.IsActive && _instance.IsPlayerNearby)
                    {
                        float t = elapsed / duration;
                        _instance.cameraLight.intensity = Mathf.Lerp(startIntensityPassive, endIntensityPassive, t);
                        elapsed += Time.deltaTime;
                        yield return frameEnd;
                    }
                    if (!registered || _instance.IsBroken || !_instance.IsActive) yield break;
                    if (_instance.isPlayerSighted || !_instance.IsPlayerNearby) break;
                    _instance.cameraLight.intensity = endIntensityPassive;

                    elapsed = 0f;
                    while (elapsed < duration && registered && !_instance.isPlayerSighted && !_instance.IsBroken && _instance.IsActive && _instance.IsPlayerNearby)
                    {
                        float t = elapsed / duration;
                        _instance.cameraLight.intensity = Mathf.Lerp(endIntensityPassive, startIntensityPassive, t);
                        elapsed += Time.deltaTime;
                        yield return frameEnd;
                    }
                    if (!registered || _instance.IsBroken || !_instance.IsActive) yield break;
                    if (_instance.isPlayerSighted || !_instance.IsPlayerNearby) break;
                    _instance.cameraLight.intensity = startIntensityPassive;
                }

                float startIntensity = _instance.cameraLight.intensity;
                while (_instance.isPlayerSighted)
                {
                    if (Mathf.Approximately(startIntensity, endIntensityActive))
                    {
                        Log("Max light reachhed");
                        yield return Wait05;
                        if (!registered || _instance.IsBroken || !_instance.IsActive) yield break;
                        if (!_instance.isPlayerSighted || !_instance.IsPlayerNearby) break;
                        continue;
                    }

                    elapsed = 0f;
                    while (elapsed < duration && registered && _instance.isPlayerSighted && !_instance.IsBroken && _instance.IsActive && _instance.IsPlayerNearby)
                    {
                        float t = elapsed / duration;
                        _instance.cameraLight.intensity = Mathf.Lerp(startIntensity, endIntensityActive, t);
                        _instance.cameraLight.range = Mathf.Lerp(startRangeActive, endRangeActive, t);
                        elapsed += Time.deltaTime;
                        yield return frameEnd;
                    }
                    if (!registered || _instance.IsBroken || !_instance.IsActive) yield break;
                    if (!_instance.isPlayerSighted || !_instance.IsPlayerNearby) break;
                    _instance.cameraLight.intensity = endIntensityActive;
                    break;
                }
            }
        }

        public IEnumerator HandleLineWidth()
        {

            float duration = surveillanceConfig.CameraNoticeSpeed;
            float elapsed = 0f;

            for (; ; )
            {
                yield return Wait05;
                if (!registered || !_instance.IsActive || _instance.IsBroken) yield break;
                if (!_instance.isPlayerSighted || !_instance.IsPlayerNearby) continue;
                while (_instance.isPlayerSighted)
                {
                    elapsed = 0f;
                    if (!Mathf.Approximately(_instance.lineRenderer.widthMultiplier, HylandFlockInstance.LINE_WIDTH_MIN))
                        _instance.lineRenderer.widthMultiplier = HylandFlockInstance.LINE_WIDTH_MIN;

                    while (elapsed < duration / 2f && registered && !_instance.IsBroken && _instance.IsActive && _instance.IsPlayerNearby)
                    {
                        float t = elapsed / (duration / 2f);
                        _instance.lineRenderer.widthMultiplier = Mathf.Lerp(HylandFlockInstance.LINE_WIDTH_MIN, HylandFlockInstance.LINE_WIDTH_MAX, t);
                        elapsed += Time.deltaTime;
                        yield return frameEnd;
                    }
                    if (!registered || _instance.IsBroken || !_instance.IsActive) yield break;
                    if (!_instance.isPlayerSighted || !_instance.IsPlayerNearby) break;

                    _instance.lineRenderer.widthMultiplier = HylandFlockInstance.LINE_WIDTH_MAX;
                    elapsed = 0f;
                    while (elapsed < duration / 2f && registered && !_instance.IsBroken && _instance.IsActive && _instance.IsPlayerNearby)
                    {
                        float t = elapsed / (duration / 2f);
                        _instance.lineRenderer.widthMultiplier = Mathf.Lerp(HylandFlockInstance.LINE_WIDTH_MAX, HylandFlockInstance.LINE_WIDTH_MIN, t);
                        elapsed += Time.deltaTime;
                        yield return frameEnd;
                    }
                    if (!registered || _instance.IsBroken || !_instance.IsActive) yield break;
                    if (!_instance.isPlayerSighted || !_instance.IsPlayerNearby) break;

                    _instance.lineRenderer.widthMultiplier = HylandFlockInstance.LINE_WIDTH_MIN;
                }
                _instance.lineRenderer.widthMultiplier = 0f;
            }
        }

    }

#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    public class FlockActivationZone : MonoBehaviour
    {
#if IL2CPP
        public FlockActivationZone(IntPtr ptr) : base(ptr) { }

        public FlockActivationZone() : base(ClassInjector.DerivedConstructorPointer<FlockActivationZone>())
            => ClassInjector.DerivedConstructorBody(this);
#endif
        public HylandFlockInstance parentInstance;
        public SphereCollider sc;
        public Rigidbody rb;
        public void Awake()
        {
            this.gameObject.layer = activationZoneLayer;
            sc = this.GetOrAddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = surveillanceConfig.CameraActivationRange;
            rb = this.GetOrAddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != 6) return;
            Player playerComp = other.gameObject.GetComponentInParent<Player>();
            if (playerComp != null)
                parentInstance.SetPlayerNearby(true);
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer != 6) return;
            Player playerComp = other.gameObject.GetComponentInParent<Player>();
            if (playerComp != null)
                parentInstance.SetPlayerNearby(false);
        }
    }
}

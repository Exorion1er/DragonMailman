using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace Movement
{
    public class HoverController : MonoBehaviour
    {
        [Header("--- Central References ---")]
        public Rigidbody rb;
        public InputActionAsset inputAsset;
        public FlyingMovement flyingMovement;
        public GroundedMovement groundedMovement;
        public PoseController poseController;
        public DragonAnnoyance dragonAnnoyance;
        public EventReference collisionSfx;
        public EventReference flyingSfx;

        [Header("--- Landing & Hover ---")]
        public bool enableHover = true;
        public float hoverHeight = 0.3f;
        public float hoverRayLength = 3f;
        public float verticalSnapSpeed = 5f;
        public LayerMask groundLayers;
        public LayerMask waterLayer;
        public float maxLandingSpeed = 25f;
        public bool crashOnHardLanding = true;

        [Header("--- Collision ---")]
        public bool collideGoal = true;
        public LayerMask obstacleLayers;
        public float collisionSkin = 0.05f;
        public bool slideAlongWalls = true;

        [HideInInspector]
        public bool isGrounded;
        [HideInInspector]
        public Vector3 horizontalVelocity;
        [HideInInspector]
        public float verticalVelocity;

        private EventInstance flyingInstance;
        private InputActionMap playerMap;

        private void Awake()
        {
            playerMap = inputAsset.FindActionMap("Player");
        }

        private void Update()
        {
            UpdateFlyingSfx();

            switch (isGrounded)
            {
                // Switch which script is active based on grounding
                case true when !groundedMovement.enabled:
                    StopFlyingSfx();
                    groundedMovement.enabled = true;
                    flyingMovement.enabled = false;
                    poseController.SetHoverPose();
                    break;
                case false when !flyingMovement.enabled:
                    StartFlyingSfx();
                    groundedMovement.enabled = false;
                    flyingMovement.enabled = true;
                    poseController.SetFlyPose();
                    break;
            }
        }

        private void OnEnable() => playerMap?.Enable();

        private void OnDisable()
        {
            playerMap?.Disable();
            StopFlyingSfx();
        }

        private void StartFlyingSfx()
        {
            if (flyingInstance.isValid()) return;

            flyingInstance = RuntimeManager.CreateInstance(flyingSfx);
            flyingInstance.start();
        }

        private void UpdateFlyingSfx()
        {
            if (!flyingInstance.isValid()) return;

            float currentSpeed = (horizontalVelocity + Vector3.up * verticalVelocity).magnitude;
            float speedParam = Mathf.InverseLerp(0f, flyingMovement.maxFlightSpeed, currentSpeed) * 100f;
            flyingInstance.setParameterByName("SPEED", speedParam);
        }

        private void StopFlyingSfx()
        {
            if (!flyingInstance.isValid()) return;

            flyingInstance.stop(STOP_MODE.ALLOWFADEOUT);
            flyingInstance.release();
            flyingInstance.clearHandle();
        }

        public event Action OnObstacleHit;

        public Vector3 ApplyHover(Vector3 targetPos, ref float vSpeed, ref Vector3 hVelocity)
        {
            isGrounded = false;
            if (!enableHover) return targetPos;

            // Bypass hover snapping if we are launching upwards
            if (vSpeed > 2f) return targetPos;

            Vector3 rayStart = new(targetPos.x, rb.position.y + 0.1f, targetPos.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, hoverRayLength, groundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                if (((1 << hit.collider.gameObject.layer) & waterLayer) != 0)
                {
                    StartCoroutine(dragonAnnoyance.GameOverSequence());
                    return targetPos;
                }

                float floorY = hit.point.y + hoverHeight;

                if (targetPos.y <= floorY + 1f)
                {
                    // Calculate our total speed right before hitting the ground
                    float currentSpeed = (hVelocity + Vector3.up * vSpeed).magnitude;

                    if (currentSpeed <= maxLandingSpeed)
                    {
                        isGrounded = true;

                        if (vSpeed < 0) vSpeed = 0;

                        // Bleed off excess horizontal momentum upon landing
                        float speed = hVelocity.magnitude;
                        speed = Mathf.MoveTowards(speed, 0, verticalSnapSpeed * Time.fixedDeltaTime);
                        hVelocity = hVelocity.normalized * speed;

                        if (targetPos.y < hit.point.y + 0.05f) targetPos.y = hit.point.y + 0.05f;
                        targetPos.y = Mathf.MoveTowards(targetPos.y, floorY, verticalSnapSpeed * Time.fixedDeltaTime);

                        if (Mathf.Approximately(targetPos.y, floorY)) vSpeed = 0;
                    }
                    else
                    {
                        if (crashOnHardLanding) OnObstacleHit?.Invoke();

                        // Prevent the dragon from clipping through the floor during a nosedive
                        if (targetPos.y < hit.point.y + 0.05f) targetPos.y = hit.point.y + 0.05f;

                        // Kill downward velocity so they "splat" and slide instead of phasing through the earth
                        if (vSpeed < 0) vSpeed = 0;
                    }
                }
            }
            return targetPos;
        }

        public void ForceUnground()
        {
            isGrounded = false;
        }

        public Vector3 SolveCollisions(Vector3 from, Vector3 to)
        {
            Vector3 path = to - from;
            float distance = path.magnitude;
            if (distance < 0.001f) return to;

            Vector3 direction = path / distance;

            bool hitObstacle = false;
            RaycastHit validHit = new();
            float closestDistance = Mathf.Infinity;

            // Primary Check: SweepTestAll
            RaycastHit[] hits = rb.SweepTestAll(direction, distance + collisionSkin, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (((1 << hit.collider.gameObject.layer) & obstacleLayers) == 0 || !(hit.distance < closestDistance))
                    continue;

                closestDistance = hit.distance;
                validHit = hit;
                hitObstacle = true;
            }

            // If SweepTest is blind, back up and SphereCast
            if (!hitObstacle)
            {
                // Start half a meter behind and half a meter up (to avoid scraping the floor)
                Vector3 fallbackStart = from - direction * 0.5f + Vector3.up * 0.5f;

                if (Physics.SphereCast(fallbackStart, 0.4f, direction, out RaycastHit sphereHit,
                        distance + 0.5f + collisionSkin, obstacleLayers, QueryTriggerInteraction.Ignore))
                {
                    validHit = sphereHit;
                    // Compensate for the fact that we started 0.5 meters backward
                    validHit.distance = Mathf.Max(0, sphereHit.distance - 0.5f);
                    hitObstacle = true;
                }
            }

            if (hitObstacle)
            {
                // Only trigger a crash if we hit a wall while flying
                if (!isGrounded)
                {
                    if (((1 << validHit.collider.gameObject.layer) & waterLayer) == 0)
                        RuntimeManager.PlayOneShot(collisionSfx);
                    OnObstacleHit?.Invoke();
                }

                float moveDist = Mathf.Max(0, validHit.distance - collisionSkin);
                Vector3 contactPoint = from + direction * moveDist;

                if (!slideAlongWalls) return contactPoint;

                Vector3 remainingPath = to - contactPoint;
                Vector3 slidePath = Vector3.ProjectOnPlane(remainingPath, validHit.normal);

                if (slidePath.magnitude < 0.001f) return contactPoint;

                // Slide Path Sweep
                bool slideHitObstacle = false;
                float closestSlideDist = Mathf.Infinity;
                RaycastHit validSlideHit = new();

                RaycastHit[] slideHits = rb.SweepTestAll(slidePath.normalized, slidePath.magnitude + collisionSkin,
                    QueryTriggerInteraction.Ignore);
                foreach (RaycastHit slideHit in slideHits)
                {
                    if (((1 << slideHit.collider.gameObject.layer) & obstacleLayers) == 0 ||
                        !(slideHit.distance < closestSlideDist))
                        continue;

                    closestSlideDist = slideHit.distance;
                    validSlideHit = slideHit;
                    slideHitObstacle = true;
                }

                // Slide Anti-Overlap Fallback
                if (!slideHitObstacle)
                {
                    Vector3 slideFallbackStart = contactPoint - slidePath.normalized * 0.5f + Vector3.up * 0.5f;
                    if (Physics.SphereCast(slideFallbackStart, 0.4f, slidePath.normalized,
                            out RaycastHit slideSphereHit, slidePath.magnitude + 0.5f + collisionSkin, obstacleLayers,
                            QueryTriggerInteraction.Ignore))
                    {
                        validSlideHit = slideSphereHit;
                        validSlideHit.distance = Mathf.Max(0, slideSphereHit.distance - 0.5f);
                        slideHitObstacle = true;
                    }
                }

                if (!slideHitObstacle) return contactPoint + slidePath;
                float slideDist = Mathf.Max(0, validSlideHit.distance - collisionSkin);
                return contactPoint + slidePath.normalized * slideDist;
            }

            return to;
        }
    }
}

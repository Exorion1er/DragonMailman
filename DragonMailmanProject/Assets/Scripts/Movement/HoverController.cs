using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    public class HoverController : MonoBehaviour
    {
        [Header("--- Central References ---")]
        public Rigidbody rb;
        public InputActionAsset inputAsset;
        public FlyingMovement flyingMovement;
        public GroundedMovement groundedMovement;

        [Header("--- Landing & Hover ---")]
        public bool enableHover = true;
        public float hoverHeight = 1.4f;
        public float hoverRayLength = 3f;
        public float verticalSnapSpeed = 5f;
        public LayerMask groundLayers;
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

        private InputActionMap playerMap;

        private void Awake()
        {
            playerMap = inputAsset.FindActionMap("Player");
        }

        private void Update()
        {
            // Switch which script is active based on grounding
            if (isGrounded && !groundedMovement.enabled)
            {
                groundedMovement.enabled = true;
                flyingMovement.enabled = false;
            }
            else if (!isGrounded && !flyingMovement.enabled)
            {
                groundedMovement.enabled = false;
                flyingMovement.enabled = true;
            }
        }

        private void OnEnable() => playerMap?.Enable();
        private void OnDisable() => playerMap?.Disable();

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
                float floorY = hit.point.y + hoverHeight;

                if (targetPos.y <= floorY + 0.1f)
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

            if (rb.SweepTest(direction, out RaycastHit hit, distance + collisionSkin, QueryTriggerInteraction.Ignore))
            {
                if (((1 << hit.collider.gameObject.layer) & obstacleLayers) == 0) return to;

                OnObstacleHit?.Invoke();

                float moveDist = Mathf.Max(0, hit.distance - collisionSkin);
                Vector3 contactPoint = from + direction * moveDist;

                if (!slideAlongWalls) return contactPoint;

                Vector3 remainingPath = to - contactPoint;
                Vector3 slidePath = Vector3.ProjectOnPlane(remainingPath, hit.normal);

                if (slidePath.magnitude < 0.001f) return contactPoint;

                if (rb.SweepTest(slidePath.normalized, out RaycastHit slideHit, slidePath.magnitude + collisionSkin,
                        QueryTriggerInteraction.Ignore))
                {
                    float slideDist = Mathf.Max(0, slideHit.distance - collisionSkin);
                    return contactPoint + slidePath.normalized * slideDist;
                }

                return contactPoint + slidePath;
            }
            return to;
        }
    }
}
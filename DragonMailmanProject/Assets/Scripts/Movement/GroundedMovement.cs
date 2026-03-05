using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    public class GroundedMovement : MonoBehaviour
    {
        [Header("--- References ---")]
        public Transform camTransform;

        [Header("--- Ground Speed ---")]
        public float forwardSpeed = 15f;
        public float backpedalSpeed = 7f;
        public float strafeSpeed = 10f;
        [Tooltip(
            "How fast the dragon snaps to face the camera direction. Set extremely high (e.g., 2000) for instant snapping.")]
        public float turnSpeed = 720f;

        [Header("--- Launch Mechanic ---")]
        public float minLaunchVelocity = 15f;
        public float maxLaunchVelocity = 40f;
        public float maxChargeTime = 1.5f;

        private float currentChargeTime;
        private InputAction flyAction;
        private HoverController hover;
        private bool isCharging;

        private InputAction moveAction;

        private void Awake()
        {
            hover = GetComponent<HoverController>();
        }

        private void Start()
        {
            moveAction = hover.inputAsset.FindActionMap("Player").FindAction("Move");
            flyAction = hover.inputAsset.FindActionMap("Player").FindAction("Jump");
        }

        private void Update()
        {
            if (!hover.isGrounded) return;

            if (flyAction.IsPressed())
            {
                isCharging = true;
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, maxChargeTime);
            }
            else if (isCharging) ExecuteLaunch();
        }

        private void FixedUpdate()
        {
            // Get the camera's forward direction, but flatten it to the XZ plane so the dragon stays level
            Vector3 camForwardPlanar = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;

            if (camForwardPlanar != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(camForwardPlanar, Vector3.up);
                hover.rb.MoveRotation(Quaternion.RotateTowards(hover.rb.rotation, targetRot,
                    turnSpeed * Time.fixedDeltaTime));
            }

            // Direct Movement Input (Instant speed, zero inertia)
            Vector2 input = moveAction.ReadValue<Vector2>();
            float currentZSpeed = input.y >= 0 ? forwardSpeed : backpedalSpeed;

            // Because we calculate this directly from input every frame, releasing the key means instant 0 velocity.
            Vector3 forwardVelocity = transform.forward * (input.y * currentZSpeed);
            Vector3 strafeVelocity = transform.right * (input.x * strafeSpeed);

            // Override the shared horizontal velocity directly
            hover.horizontalVelocity = forwardVelocity + strafeVelocity;

            // Apply velocities to calculate the next position
            Vector3 displacement = hover.horizontalVelocity + Vector3.up * hover.verticalVelocity;
            Vector3 targetPos = hover.rb.position + displacement * Time.fixedDeltaTime;

            targetPos = hover.ApplyHover(targetPos, ref hover.verticalVelocity, ref hover.horizontalVelocity);
            if (hover.collideGoal) targetPos = hover.SolveCollisions(hover.rb.position, targetPos);

            hover.rb.MovePosition(targetPos);
        }

        private void OnDisable()
        {
            isCharging = false;
            currentChargeTime = 0f;
        }

        private void ExecuteLaunch()
        {
            isCharging = false;

            float chargePercent = currentChargeTime / maxChargeTime;
            float launchPower = Mathf.Lerp(minLaunchVelocity, maxLaunchVelocity, chargePercent);

            hover.verticalVelocity = launchPower;
            hover.ForceUnground();

            hover.flyingMovement.TriggerLaunchRecovery();

            currentChargeTime = 0f;
        }

        public float GetChargePercent() => Mathf.Clamp01(currentChargeTime / maxChargeTime);
    }
}
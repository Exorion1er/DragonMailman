using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    public class FlyingMovement : MonoBehaviour
    {
        [Header("--- References ---")]
        public Transform camTransform;

        [Header("--- Base Flight Physics ---")]
        public float maxFlightSpeed = 100f;
        public float baseGravity = 15f;
        public float maxTerminalVelocity = 40f;

        [Header("--- Gliding ---")]
        public float pitchResponsiveness = 10f;
        public float diveAcceleration = 60f;
        public float climbDrag = 10f;
        public float stallSpeed = 5f;

        [Header("--- Steering & Momentum ---")]
        public float turnInertia = 3f;
        public float coastingDrag = 10f;
        public float turnDrag = 30f;

        [Header("--- Rotation & Visuals ---")]
        public bool faceCameraYaw = true;
        public float yawTurnSpeedDegPerSec = 720f;
        public float bankIntensity = 5f;

        [Header("--- Launch Transition ---")]
        public float launchRecoveryTime = 1f;
        private HoverController hover;
        private float launchRecoveryTimer;

        private InputAction moveAction;

        private void Awake()
        {
            hover = GetComponent<HoverController>();
        }

        private void Start()
        {
            moveAction = hover.inputAsset.FindActionMap("Player").FindAction("Move");
        }

        private void FixedUpdate()
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector3 moveForwardPlanar = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
            float currentSpeed = hover.horizontalVelocity.magnitude;

            HandleGliderPhysics(camTransform.forward.y, ref currentSpeed);
            HandleSteering(input, moveForwardPlanar, ref currentSpeed);
            HandleRotation(camTransform.forward, moveForwardPlanar, input.x);

            Vector3 displacement = hover.horizontalVelocity + Vector3.up * hover.verticalVelocity;
            Vector3 targetPos = hover.rb.position + displacement * Time.fixedDeltaTime;

            targetPos = hover.ApplyHover(targetPos, ref hover.verticalVelocity, ref hover.horizontalVelocity);
            if (hover.collideGoal) targetPos = hover.SolveCollisions(hover.rb.position, targetPos);

            hover.rb.MovePosition(targetPos);
        }

        public void TriggerLaunchRecovery()
        {
            launchRecoveryTimer = launchRecoveryTime;
        }

        private void HandleGliderPhysics(float pitch, ref float currentHorizontalSpeed)
        {
            if (launchRecoveryTimer > 0f) launchRecoveryTimer -= Time.fixedDeltaTime;

            Vector3 current3DVelocity = hover.horizontalVelocity + Vector3.up * hover.verticalVelocity;
            float trueSpeed = current3DVelocity.magnitude;

            switch (pitch)
            {
                case < -0.01f:
                    trueSpeed += Mathf.Abs(pitch) * diveAcceleration * Time.fixedDeltaTime;
                    break;
                case > 0.01f:
                    trueSpeed -= pitch * climbDrag * Time.fixedDeltaTime;
                    break;
            }

            if (hover.horizontalVelocity.sqrMagnitude > 0.1f)
            {
                Vector3 flightDir = hover.horizontalVelocity.normalized;
                Vector3 lookDir = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized;
                float turnSeverity = Mathf.InverseLerp(0f, 90f, Vector3.Angle(flightDir, lookDir));
                trueSpeed -= turnSeverity * turnDrag * Time.fixedDeltaTime;
            }

            trueSpeed -= coastingDrag * Time.fixedDeltaTime;
            trueSpeed = trueSpeed > maxFlightSpeed
                ? Mathf.MoveTowards(trueSpeed, maxFlightSpeed, coastingDrag * 2f * Time.fixedDeltaTime)
                : Mathf.Clamp(trueSpeed, 0, maxFlightSpeed);

            Vector3 camForward = camTransform.forward;
            float targetVVel = camForward.y * trueSpeed;
            float targetHVel = new Vector3(camForward.x, 0, camForward.z).magnitude * trueSpeed;

            float stallPenalty = Mathf.InverseLerp(stallSpeed, 0f, trueSpeed);
            targetVVel -= baseGravity * stallPenalty;

            float recoveryFactor = launchRecoveryTime > 0f
                ? 1f - Mathf.Clamp01(launchRecoveryTimer / launchRecoveryTime)
                : 1f;
            float effectivePitchResponsiveness = pitchResponsiveness * recoveryFactor;

            hover.verticalVelocity = Mathf.Lerp(hover.verticalVelocity, targetVVel,
                effectivePitchResponsiveness * Time.fixedDeltaTime);
            hover.verticalVelocity = Mathf.Clamp(hover.verticalVelocity, -maxTerminalVelocity, maxTerminalVelocity);

            currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, targetHVel,
                effectivePitchResponsiveness * Time.fixedDeltaTime);
        }

        private void HandleSteering(Vector2 input, Vector3 moveForwardPlanar, ref float currentSpeed)
        {
            if (currentSpeed <= 0.1f)
            {
                hover.horizontalVelocity = Vector3.zero;
                return;
            }

            Vector3 targetHeading = moveForwardPlanar;

            if (Mathf.Abs(input.x) > 0.1f)
            {
                float steerAmount = input.x * (turnInertia * 20f) * Time.fixedDeltaTime;
                Quaternion steerRot = Quaternion.Euler(0, steerAmount, 0);

                Vector3 currentHeading = hover.horizontalVelocity.sqrMagnitude > 0
                    ? hover.horizontalVelocity.normalized
                    : moveForwardPlanar;
                targetHeading = steerRot * currentHeading;
            }

            float agility = Mathf.Lerp(turnInertia, turnInertia * 0.2f, currentSpeed / maxFlightSpeed);
            Vector3 alignedDir = Vector3.RotateTowards(
                hover.horizontalVelocity.sqrMagnitude > 0 ? hover.horizontalVelocity.normalized : moveForwardPlanar,
                targetHeading, agility * Time.fixedDeltaTime, 0f);

            hover.horizontalVelocity = alignedDir * currentSpeed;
        }

        private void HandleRotation(Vector3 lookForward3D, Vector3 moveForwardPlanar, float lateralInput)
        {
            if (!faceCameraYaw || moveForwardPlanar == Vector3.zero) return;

            Quaternion bankRot = Quaternion.Euler(0, 0, -lateralInput * bankIntensity);
            Quaternion targetRot = Quaternion.LookRotation(lookForward3D, Vector3.up) * bankRot;

            hover.rb.MoveRotation(Quaternion.RotateTowards(hover.rb.rotation, targetRot,
                yawTurnSpeedDegPerSec * Time.fixedDeltaTime));
        }

        public void ApplySpeedBoost(float boostAmount)
        {
            Vector3 dir = hover.horizontalVelocity.sqrMagnitude > 0.1f
                ? hover.horizontalVelocity.normalized
                : Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;

            float currentSpeed = hover.horizontalVelocity.magnitude;
            float newSpeed = Mathf.Min(currentSpeed + boostAmount, maxFlightSpeed * 3f);

            hover.horizontalVelocity = dir * newSpeed;
        }
    }
}
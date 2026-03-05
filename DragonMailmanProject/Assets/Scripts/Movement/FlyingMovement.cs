using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    public class FlyingMovement : MonoBehaviour
    {
        [Header("--- References ---")]
        public Transform camTransform;
        public HoverController hover;

        [Header("--- Base Flight Physics ---")]
        public float maxFlightSpeed = 100f;
        public float baseGravity = 15f;
        public float maxTerminalVelocity = 40f;

        [Header("--- Gliding ---")]
        public float pitchResponsiveness = 10f;
        public float diveAcceleration = 60f;
        public float climbDrag = 20f;
        public float stallSpeed = 15f;

        [Header("--- Steering & Momentum ---")]
        public float turnInertia = 2f;
        public float coastingDrag = 2f;
        public float turnDrag = 20f;

        [Header("--- Rotation & Visuals ---")]
        public bool faceCameraYaw = true;
        public float yawTurnSpeedDegPerSec = 720f;
        public float bankIntensity = 5f;

        [Header("--- Launch Transition ---")]
        public float launchRecoveryTime = 2f;

        [Header("--- Belly Flop ---")]
        public float airBrakeDrag = 20f;
        public float maxFlareAngle = 45f;
        public float parachutingFallSpeed = 10f;
        public float airCatchForce = 25f;
        public float momentumRetention = 2f;

        private float launchRecoveryTimer;
        private InputAction moveAction;
        private float smoothedBrakeInput;

        private void Start()
        {
            moveAction = hover.inputAsset.FindActionMap("Player").FindAction("Move");
        }

        private void FixedUpdate()
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            Vector3 moveForwardPlanar = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;

            UpdateFlightState(input.y);

            float forwardSpeed = CalculateForwardEnergy(camTransform.forward);

            Vector3 targetVelocity = CalculateTargetVelocity(camTransform.forward, forwardSpeed);

            ApplyVelocity(targetVelocity);

            float currentHorizontalSpeed = hover.horizontalVelocity.magnitude;
            HandleSteering(input, moveForwardPlanar, ref currentHorizontalSpeed);
            HandleRotation(moveForwardPlanar, input.x);

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

        private void UpdateFlightState(float verticalInput)
        {
            if (launchRecoveryTimer > 0f) launchRecoveryTimer -= Time.fixedDeltaTime;

            float rawBrake = Mathf.Clamp01(-verticalInput);
            smoothedBrakeInput = Mathf.MoveTowards(smoothedBrakeInput, rawBrake, Time.fixedDeltaTime * 2f);
        }

        private float CalculateForwardEnergy(Vector3 camForward)
        {
            Vector3 current3DVelocity = hover.horizontalVelocity + Vector3.up * hover.verticalVelocity;
            float speed = current3DVelocity.magnitude;
            float pitch = camForward.y;

            if (pitch < -0.05f)
                speed += Mathf.Abs(pitch) * diveAcceleration * Time.fixedDeltaTime;
            else if (pitch > 0.05f) speed -= pitch * climbDrag * Time.fixedDeltaTime;

            float turnPenalty = 0f;
            if (hover.horizontalVelocity.sqrMagnitude > 1f)
            {
                Vector3 currentHeading = hover.horizontalVelocity.normalized;
                Vector3 targetHeading = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;

                if (targetHeading.sqrMagnitude > 0.1f)
                {
                    float turnAngle = Vector3.Angle(currentHeading, targetHeading);

                    float turnSeverity = Mathf.InverseLerp(5f, 90f, turnAngle);
                    turnPenalty = turnDrag * turnSeverity;
                }
            }

            float dynamicBrakePower = airBrakeDrag * speed * 0.1f;
            float currentDrag = coastingDrag + dynamicBrakePower * smoothedBrakeInput + turnPenalty;

            speed = Mathf.MoveTowards(speed, 0f, currentDrag * Time.fixedDeltaTime);

            return Mathf.Clamp(speed, 0f, maxFlightSpeed);
        }

        private Vector3 CalculateTargetVelocity(Vector3 camForward, float forwardSpeed)
        {
            float speedFactor = Mathf.InverseLerp(0f, maxFlightSpeed, forwardSpeed);
            float currentFlareAngle = smoothedBrakeInput * maxFlareAngle * speedFactor;
            Vector3 flareDir = Quaternion.AngleAxis(-currentFlareAngle, camTransform.right) * camForward;

            float flareSpeedPenalty = Mathf.Lerp(1f, 0.4f, smoothedBrakeInput);
            Vector3 targetVelocity = flareDir * (forwardSpeed * flareSpeedPenalty);

            float liftFactor = Mathf.InverseLerp(stallSpeed, stallSpeed * 3f, forwardSpeed);
            float effectiveGravity = Mathf.Lerp(baseGravity, 0f, liftFactor);
            targetVelocity.y -= effectiveGravity;

            if (smoothedBrakeInput > 0.01f && forwardSpeed > stallSpeed)
            {
                float liftPotential = Mathf.InverseLerp(stallSpeed, maxFlightSpeed, forwardSpeed);
                targetVelocity.y += liftPotential * airCatchForce * smoothedBrakeInput;
            }

            float maxFallSpeed = Mathf.Lerp(maxTerminalVelocity, parachutingFallSpeed, smoothedBrakeInput);
            if (targetVelocity.y < -maxFallSpeed) targetVelocity.y = -maxFallSpeed;

            return targetVelocity;
        }

        private void ApplyVelocity(Vector3 targetVelocity)
        {
            Vector3 current3DVelocity = hover.horizontalVelocity + Vector3.up * hover.verticalVelocity;

            float recoveryFactor = launchRecoveryTime > 0f
                ? 1f - Mathf.Clamp01(launchRecoveryTimer / launchRecoveryTime)
                : 1f;
            float responsiveness =
                Mathf.Lerp(pitchResponsiveness, momentumRetention, smoothedBrakeInput) * recoveryFactor;

            Vector3 nextVelocity =
                Vector3.Lerp(current3DVelocity, targetVelocity, responsiveness * Time.fixedDeltaTime);

            hover.verticalVelocity = nextVelocity.y;
            hover.horizontalVelocity = new Vector3(nextVelocity.x, 0, nextVelocity.z);
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

        private void HandleRotation(Vector3 moveForwardPlanar, float lateralInput)
        {
            if (!faceCameraYaw || moveForwardPlanar == Vector3.zero) return;

            Vector3 flightDir = hover.horizontalVelocity + Vector3.up * hover.verticalVelocity;
            if (flightDir.sqrMagnitude < 0.1f) flightDir = moveForwardPlanar;

            float speed = flightDir.magnitude;
            float levelOutFactor = Mathf.InverseLerp(stallSpeed * 1.5f, 0f, speed);

            Vector3 visualForward = Vector3.Slerp(flightDir.normalized, moveForwardPlanar, levelOutFactor);

            Quaternion bankRot = Quaternion.Euler(0, 0, -lateralInput * bankIntensity);
            Quaternion targetRot = Quaternion.LookRotation(visualForward, Vector3.up) * bankRot;

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
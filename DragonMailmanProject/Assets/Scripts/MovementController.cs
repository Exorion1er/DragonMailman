using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [Header("--- References ---")]
    public Rigidbody rb;
    public InputActionAsset inputAsset;
    public Transform camTransform;

    [Header("--- Base Flight Physics ---"), Tooltip("The standard forward speed limit before diving")]
    public float maxFlightSpeed = 100f;
    public float baseGravity = 15f;
    public float maxTerminalVelocity = 40f;

    [Header("--- Gliding ---"),
     Tooltip("How quickly the dragon's flight path aligns with the camera. Higher = snappier.")]
    public float pitchResponsiveness = 10f;
    [Tooltip("Speed gained when looking down")]
    public float diveAcceleration = 60f;
    [Tooltip("Speed lost when looking up")]
    public float climbDrag = 10f;
    [Tooltip("Speed below which the dragon loses lift and falls")]
    public float stallSpeed = 5f;

    [Header("--- Steering & Momentum ---")]
    public float turnInertia = 3f;
    public float coastingDrag = 10f;
    [Tooltip("How much speed is lost when you whip the camera away from your current flight path")]
    public float turnDrag = 30f;

    [Header("--- Rotation & Visuals ---")]
    public bool faceCameraYaw = true;
    public float yawTurnSpeedDegPerSec = 720f;
    public float bankIntensity = 5f;

    [Header("--- Landing & Hover ---")]
    public bool enableHover = true;
    public float hoverHeight = 1.4f;
    public float hoverRayLength = 3f;
    public float verticalSnapSpeed = 5f;
    public LayerMask groundLayers;

    [Header("--- Launch Mechanic ---")]
    public float minLaunchVelocity = 15f;
    public float maxLaunchVelocity = 40f;
    [Tooltip("How many seconds you must hold jump to reach max launch velocity")]
    public float maxChargeTime = 1.5f;
    [Tooltip("How long it takes to regain full camera pitch control after launching (prevents snapping)")]
    public float launchRecoveryTime = 1f;

    [Header("--- Collision ---")]
    public bool collideGoal = true;
    public LayerMask obstacleLayers;
    public float collisionSkin = 0.05f;
    public bool slideAlongWalls = true;

    // Launch State
    private float currentChargeTime;

    private InputAction flyAction;
    private Vector3 horizontalVelocity;
    private bool isCharging;
    private float launchRecoveryTimer;
    private InputAction moveAction;
    private float verticalVelocity;

    private void Awake()
    {
        moveAction = inputAsset.FindActionMap("Player").FindAction("Move");
        flyAction = inputAsset.FindActionMap("Player").FindAction("Jump");
    }

    private void Update()
    {
        hSpeed = Mathf.RoundToInt(horizontalVelocity.magnitude);
        vSpeed = Mathf.RoundToInt(verticalVelocity);

        HandleLaunchInput();
    }

    private void FixedUpdate()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 moveForwardPlanar = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        float currentSpeed = horizontalVelocity.magnitude;

        HandleGliderPhysics(camTransform.forward.y, ref currentSpeed);
        HandleSteering(input, moveForwardPlanar, ref currentSpeed);
        HandleRotation(camTransform.transform.forward, moveForwardPlanar, input.x);

        Vector3 displacement = horizontalVelocity + Vector3.up * verticalVelocity;
        Vector3 targetPos = rb.position + displacement * Time.fixedDeltaTime;

        targetPos = HandleGroundingAndHover(targetPos, ref currentSpeed);

        if (collideGoal) targetPos = SolveCollisions(rb.position, targetPos);
        rb.MovePosition(targetPos);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        flyAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        flyAction?.Disable();
    }

    private void HandleLaunchInput()
    {
        if (isGrounded)
        {
            if (flyAction.IsPressed())
            {
                isCharging = true;
                currentChargeTime += Time.deltaTime;
                currentChargeTime = Mathf.Clamp(currentChargeTime, 0f, maxChargeTime);
            }
            else if (isCharging) ExecuteLaunch();
        }
        else
        {
            isCharging = false;
            currentChargeTime = 0f;
        }
    }

    private void ExecuteLaunch()
    {
        isCharging = false;

        float chargePercent = currentChargeTime / maxChargeTime;
        float launchPower = Mathf.Lerp(minLaunchVelocity, maxLaunchVelocity, chargePercent);

        verticalVelocity = launchPower;

        isGrounded = false;
        currentChargeTime = 0f;

        // Trigger the recovery timer so we don't snap to the camera immediately
        launchRecoveryTimer = launchRecoveryTime;
    }

    private void HandleGliderPhysics(float pitch, ref float currentHorizontalSpeed)
    {
        if (isGrounded) return;

        // Process Launch Recovery Timer
        if (launchRecoveryTimer > 0f) launchRecoveryTimer -= Time.fixedDeltaTime;

        Vector3 current3DVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
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

        if (horizontalVelocity.sqrMagnitude > 0.1f)
        {
            Vector3 flightDir = horizontalVelocity.normalized;
            Vector3 lookDir = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized;
            float turnAngle = Vector3.Angle(flightDir, lookDir);
            float turnSeverity = Mathf.InverseLerp(0f, 90f, turnAngle);
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

        // Calculate Effective Pitch Responsiveness
        float recoveryFactor =
            launchRecoveryTime > 0f ? 1f - Mathf.Clamp01(launchRecoveryTimer / launchRecoveryTime) : 1f;
        float effectivePitchResponsiveness = pitchResponsiveness * recoveryFactor;

        // Apply velocities using the dampened responsiveness
        verticalVelocity = Mathf.Lerp(verticalVelocity, targetVVel, effectivePitchResponsiveness * Time.fixedDeltaTime);
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxTerminalVelocity, maxTerminalVelocity);

        currentHorizontalSpeed = Mathf.Lerp(currentHorizontalSpeed, targetHVel,
            effectivePitchResponsiveness * Time.fixedDeltaTime);
    }

    private void HandleSteering(Vector2 input, Vector3 moveForwardPlanar, ref float currentSpeed)
    {
        if (currentSpeed <= 0.1f)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }

        Vector3 targetHeading = moveForwardPlanar;

        if (Mathf.Abs(input.x) > 0.1f)
        {
            float steerAmount = input.x * (turnInertia * 20f) * Time.fixedDeltaTime;
            Quaternion steerRot = Quaternion.Euler(0, steerAmount, 0);

            Vector3 currentHeading =
                horizontalVelocity.sqrMagnitude > 0 ? horizontalVelocity.normalized : moveForwardPlanar;
            targetHeading = steerRot * currentHeading;
        }

        float agility = Mathf.Lerp(turnInertia, turnInertia * 0.2f, currentSpeed / maxFlightSpeed);
        Vector3 alignedDir = Vector3.RotateTowards(
            horizontalVelocity.sqrMagnitude > 0 ? horizontalVelocity.normalized : moveForwardPlanar, targetHeading,
            agility * Time.fixedDeltaTime, 0f);

        horizontalVelocity = alignedDir * currentSpeed;
    }

    private void HandleRotation(Vector3 lookForward3D, Vector3 moveForwardPlanar, float lateralInput)
    {
        if (!faceCameraYaw || moveForwardPlanar == Vector3.zero) return;

        Quaternion bankRot = Quaternion.Euler(0, 0, -lateralInput * bankIntensity);
        Quaternion targetRot = Quaternion.LookRotation(lookForward3D, Vector3.up) * bankRot;

        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, yawTurnSpeedDegPerSec * Time.fixedDeltaTime));
    }

    private Vector3 HandleGroundingAndHover(Vector3 targetPos, ref float currentSpeed)
    {
        isGrounded = false;

        if (!enableHover) return targetPos;

        if (verticalVelocity > 2f) return targetPos;

        Vector3 rayStart = new(targetPos.x, rb.position.y + 0.1f, targetPos.z);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, hoverRayLength, groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            float floorY = hit.point.y + hoverHeight;

            if (targetPos.y <= floorY + 0.1f)
            {
                isGrounded = true;

                if (verticalVelocity < 0) verticalVelocity = 0;

                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, verticalSnapSpeed * Time.fixedDeltaTime);
                horizontalVelocity = horizontalVelocity.normalized * currentSpeed;

                if (targetPos.y < hit.point.y + 0.05f) targetPos.y = hit.point.y + 0.05f;
                targetPos.y = Mathf.MoveTowards(targetPos.y, floorY, verticalSnapSpeed * Time.fixedDeltaTime);

                if (Mathf.Approximately(targetPos.y, floorY)) verticalVelocity = 0;
            }
        }

        return targetPos;
    }

    private Vector3 SolveCollisions(Vector3 from, Vector3 to)
    {
        Vector3 path = to - from;
        float distance = path.magnitude;
        if (distance < 0.001f) return to;

        Vector3 direction = path / distance;

        if (rb.SweepTest(direction, out RaycastHit hit, distance + collisionSkin, QueryTriggerInteraction.Ignore))
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleLayers) == 0) return to;

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

    #region API

    [HideInInspector]
    public bool isGrounded;
    [HideInInspector]
    public float hSpeed;
    [HideInInspector]
    public float vSpeed;

    public float GetChargePercent() => Mathf.Clamp01(currentChargeTime / maxChargeTime);

    public void ApplySpeedBoost(float boostAmount)
    {
        Vector3 dir = horizontalVelocity.sqrMagnitude > 0.1f
            ? horizontalVelocity.normalized
            : Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;

        float currentSpeed = horizontalVelocity.magnitude;
        float newSpeed = Mathf.Min(currentSpeed + boostAmount, maxFlightSpeed * 3f);

        horizontalVelocity = dir * newSpeed;
    }

    #endregion
}
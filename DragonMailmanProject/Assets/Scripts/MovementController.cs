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

    [Header("--- Flapping ---")]
    public float flapLiftForce = 12f;
    public float flapForwardPush = 3f;
    [Tooltip("How long before you can flap again")]
    public float flapCooldown = 0.4f;

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

    [Header("--- Collision ---")]
    public bool collideGoal = true;
    public LayerMask obstacleLayers;
    public float collisionSkin = 0.05f;
    public bool slideAlongWalls = true;

    private InputAction flyAction;
    private Vector3 horizontalVelocity;
    private float lastFlapTime;
    private InputAction moveAction;
    private LineRenderer velocityLine;
    private float verticalVelocity;
    private bool wantsToFlap;

    private void Awake()
    {
        moveAction = inputAsset.FindActionMap("Player").FindAction("Move");
        flyAction = inputAsset.FindActionMap("Player").FindAction("Jump");

        // Velocity Arrow (DEBUG)
        velocityLine = new GameObject("VelocityDebugArrow").AddComponent<LineRenderer>();
        velocityLine.startWidth = 0.3f;
        velocityLine.endWidth = 0.0f;
        velocityLine.material = new Material(Shader.Find("Sprites/Default"));
        velocityLine.startColor = Color.yellow;
        velocityLine.endColor = new Color(1f, 0.5f, 0f);
        velocityLine.positionCount = 2;
        velocityLine.transform.SetParent(transform);
    }

    private void Update()
    {
        if (flyAction.WasPressedThisFrame() && Time.time >= lastFlapTime + flapCooldown)
        {
            wantsToFlap = true;
            lastFlapTime = Time.time;
        }

        hSpeed = Mathf.RoundToInt(horizontalVelocity.magnitude);
        vSpeed = Mathf.RoundToInt(verticalVelocity);
    }

    private void FixedUpdate()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 moveForwardPlanar = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        float currentSpeed = horizontalVelocity.magnitude;

        HandleFlapping(ref currentSpeed);
        HandleGliderPhysics(camTransform.forward.y, ref currentSpeed);
        HandleSteering(input, moveForwardPlanar, ref currentSpeed);
        HandleRotation(camTransform.transform.forward, moveForwardPlanar, input.x);

        Vector3 displacement = horizontalVelocity + Vector3.up * verticalVelocity;
        Vector3 targetPos = rb.position + displacement * Time.fixedDeltaTime;

        targetPos = HandleGroundingAndHover(targetPos, ref currentSpeed);

        if (collideGoal) targetPos = SolveCollisions(rb.position, targetPos);

        // Draw debug velocity arrow
        DrawVelocityDebug(horizontalVelocity + Vector3.up * verticalVelocity);
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

    private void HandleFlapping(ref float currentSpeed)
    {
        if (!wantsToFlap) return;

        // Give a burst of height. Overwrite if falling to save the player.
        verticalVelocity = verticalVelocity < 0 ? flapLiftForce : verticalVelocity + flapLiftForce;

        // Add a tiny forward bump, but don't exceed max speed just by flapping
        if (currentSpeed < maxFlightSpeed) currentSpeed += flapForwardPush;

        wantsToFlap = false;
        isGrounded = false;
    }

    private void HandleGliderPhysics(float pitch, ref float currentHorizontalSpeed)
    {
        if (isGrounded) return;

        // Calculate TRUE 3D speed to manage total energy
        Vector3 current3DVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
        float trueSpeed = current3DVelocity.magnitude;

        switch (pitch)
        {
            // Modify energy based on Pitch
            case < -0.01f:
                // Diving: Gain speed based on steepness
                trueSpeed += Mathf.Abs(pitch) * diveAcceleration * Time.fixedDeltaTime;
                break;
            case > 0.01f:
                // Climbing: Lose speed based on steepness
                trueSpeed -= pitch * climbDrag * Time.fixedDeltaTime;
                break;
        }

        // If we are moving fast, measure the angle between where we are flying and where we are looking
        if (horizontalVelocity.sqrMagnitude > 0.1f)
        {
            Vector3 flightDir = horizontalVelocity.normalized;
            Vector3 lookDir = new Vector3(camTransform.forward.x, 0, camTransform.forward.z).normalized;

            // Calculate the angle (0 = straight ahead, 100 = looking behind us)
            float turnAngle = Vector3.Angle(flightDir, lookDir);

            // Convert the angle to a 0 to 1 scale (Caps out at 90 degrees)
            float turnSeverity = Mathf.InverseLerp(0f, 90f, turnAngle);

            // Bleed speed based on how sharply we are turning
            trueSpeed -= turnSeverity * turnDrag * Time.fixedDeltaTime;
        }

        // Constant air drag over time
        trueSpeed -= coastingDrag * Time.fixedDeltaTime;
        trueSpeed = trueSpeed > maxFlightSpeed
            ? Mathf.MoveTowards(trueSpeed, maxFlightSpeed, coastingDrag * 2f * Time.fixedDeltaTime)
            : Mathf.Clamp(trueSpeed, 0, maxFlightSpeed);

        // Aim the Energy (Calculate goals based on the camera angle)
        // Vertical goal is exactly the Y-axis of the camera * speed
        // Horizontal goal is exactly the XZ plane length of the camera * speed
        Vector3 camForward = camTransform.forward;
        float targetVVel = camForward.y * trueSpeed;
        float targetHVel = new Vector3(camForward.x, 0, camForward.z).magnitude * trueSpeed;

        // Stalling Mechanic
        // If speed drops below 'stallSpeed', gravity takes over and drags the target downward
        float stallPenalty = Mathf.InverseLerp(stallSpeed, 0f, trueSpeed);
        targetVVel -= baseGravity * stallPenalty;

        // Apply smoothly for that "heavy" responsive feel
        verticalVelocity = Mathf.Lerp(verticalVelocity, targetVVel, pitchResponsiveness * Time.fixedDeltaTime);
        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxTerminalVelocity, maxTerminalVelocity);

        // Output the new horizontal speed for the HandleSteering method to use
        currentHorizontalSpeed =
            Mathf.Lerp(currentHorizontalSpeed, targetHVel, pitchResponsiveness * Time.fixedDeltaTime);
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

        if (!enableHover || wantsToFlap) return targetPos;

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

    private void DrawVelocityDebug(Vector3 actualVelocity)
    {
        Vector3 startPos = rb.position + Vector3.up * 1.0f;

        if (actualVelocity.sqrMagnitude < 0.01f)
        {
            if (velocityLine) velocityLine.enabled = false;
        }
        else if (velocityLine)
        {
            velocityLine.enabled = true;
            velocityLine.SetPosition(0, startPos);
            velocityLine.SetPosition(1, startPos + actualVelocity * 0.5f);
        }
    }

    #region API

    [HideInInspector]
    public bool isGrounded;
    [HideInInspector]
    public float hSpeed;
    [HideInInspector]
    public float vSpeed;

    public void ApplySpeedBoost(float boostAmount)
    {
        Vector3 dir = horizontalVelocity.sqrMagnitude > 0.1f
            ? horizontalVelocity.normalized
            : Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;

        float currentSpeed = horizontalVelocity.magnitude;
        // Apply the boost but respect the absolute high-end limit
        float newSpeed = Mathf.Min(currentSpeed + boostAmount, maxFlightSpeed * 3f);

        horizontalVelocity = dir * newSpeed;
    }

    #endregion
}
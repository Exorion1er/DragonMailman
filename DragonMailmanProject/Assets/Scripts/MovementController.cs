using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;

    public InputActionAsset inputAsset;
    public Transform camTransform;

    [Header("Goal Motion")]
    public float moveSpeed;

    [Header("Facing")]
    public bool faceCameraYaw;

    public float yawTurnSpeedDegPerSec;

    [Header("Hover")]
    public bool hover;

    public float hoverHeight;
    public float hoverRayLength;
    public LayerMask groundLayers;

    [Header("Physics")]
    public float gravity;

    public float flyAcceleration;
    public float maxFallSpeed;

    [Header("Smoothing")]
    public float verticalSnapSpeed;

    [Header("Collision")]
    public bool collideGoal;

    public LayerMask obstacleLayers;
    public float collisionSkin;
    public bool slideAlongWalls;

    private InputAction flyAction;
    private bool isGrounded;
    private InputAction moveAction;
    private float verticalVelocity;

    private void Awake()
    {
        moveAction = inputAsset.FindActionMap("Player").FindAction("Move");
        flyAction = inputAsset.FindActionMap("Player").FindAction("Jump");
    }

    private void FixedUpdate()
    {
        // Handle Rotation
        if (faceCameraYaw)
        {
            // Project camera forward onto the horizontal XZ plane
            Vector3 camForward = Vector3.ProjectOnPlane(-camTransform.forward, Vector3.up).normalized;

            if (camForward != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(camForward, Vector3.up);
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot,
                    yawTurnSpeedDegPerSec * Time.fixedDeltaTime));
            }
        }

        bool flyActionPressed = flyAction.IsPressed();
        if (flyActionPressed)
            verticalVelocity += flyAcceleration * Time.fixedDeltaTime;
        else if (!isGrounded) verticalVelocity -= gravity * Time.fixedDeltaTime;

        verticalVelocity = Mathf.Clamp(verticalVelocity, -maxFallSpeed, maxFallSpeed);

        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 moveDir = CalculateCameraRelativeDir(input);
        Vector3 displacement = moveDir * moveSpeed + Vector3.up * verticalVelocity;
        Vector3 targetPos = rb.position + displacement * Time.fixedDeltaTime;

        isGrounded = false;
        Vector3 rayStart = new(targetPos.x, rb.position.y + 0.1f, targetPos.z);
        if (hover && Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, hoverRayLength, groundLayers,
                QueryTriggerInteraction.Ignore))
        {
            float floorY = hit.point.y + hoverHeight;

            if (targetPos.y <= floorY + 0.1f && !flyActionPressed)
            {
                isGrounded = true;

                if (verticalVelocity < 0) verticalVelocity = 0;
                if (targetPos.y < hit.point.y + 0.05f) targetPos.y = hit.point.y + 0.05f;
                targetPos.y = Mathf.MoveTowards(targetPos.y, floorY, verticalSnapSpeed * Time.fixedDeltaTime);

                if (Mathf.Approximately(targetPos.y, floorY)) verticalVelocity = 0;
            }
        }

        // Collision / Sliding Logic
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

    private Vector3 CalculateCameraRelativeDir(Vector2 input)
    {
        if (input.sqrMagnitude < 0.001f) return Vector3.zero;

        Vector3 forward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;

        Vector3 dir = forward * input.y + right * input.x;
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
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
}
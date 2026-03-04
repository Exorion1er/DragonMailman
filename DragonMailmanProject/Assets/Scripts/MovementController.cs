using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;

    public InputActionAsset inputAsset;
    public Camera cam;

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

    [Header("Collision")]
    public bool collideGoal;

    public LayerMask obstacleLayers;
    public float collisionSkin;
    public bool slideAlongWalls;

    private Transform camTransform;
    private InputAction moveAction;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (cam) camTransform = cam.transform;

        moveAction = inputAsset.FindActionMap("Player").FindAction("Move");
    }

    private void FixedUpdate()
    {
        if (!rb || !camTransform) return;

        // 1. Handle Rotation
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

        // 2. Handle Movement
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 moveDir = CalculateCameraRelativeDir(input);

        Vector3 targetPos = rb.position + moveDir * (moveSpeed * Time.fixedDeltaTime);

        // 3. Hover Logic
        if (hover)
        {
            if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, hoverRayLength,
                    groundLayers, QueryTriggerInteraction.Ignore))
                targetPos.y = hit.point.y + hoverHeight;
        }

        // 4. Collision / Sliding Logic
        if (collideGoal) targetPos = SolveCollisions(rb.position, targetPos);

        rb.MovePosition(targetPos);
    }

    private void OnEnable() => moveAction?.Enable();
    private void OnDisable() => moveAction?.Disable();

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
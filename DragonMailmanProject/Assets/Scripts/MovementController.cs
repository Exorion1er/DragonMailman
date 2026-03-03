using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Hover (goal height)")]
    public bool hover;

    public float hoverHeight;
    public float hoverRayLength;
    public LayerMask groundLayers;

    [Header("Goal Collision (anti-ghosting)")]
    public bool collideGoal;

    public LayerMask obstacleLayers;
    public float collisionSkin;
    public bool slideAlongWalls;

    private InputAction moveAction;
    private Vector2 moveInput;

    private void Awake()
    {
        moveAction = inputAsset.FindActionMap("Player").FindAction("Move");
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (!rb || !cam)
            return;

        if (faceCameraYaw)
        {
            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;

            if (camForward.sqrMagnitude > 1e-6f)
            {
                Vector3 desiredForward = -camForward.normalized;

                Quaternion targetYaw = Quaternion.LookRotation(desiredForward, Vector3.up);
                Quaternion nextRot = Quaternion.RotateTowards(rb.rotation, targetYaw,
                    yawTurnSpeedDegPerSec * Time.fixedDeltaTime);
                rb.MoveRotation(nextRot);
            }
        }

        // Camera-relative planar basis
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward = forward.sqrMagnitude > 0f ? forward.normalized : Vector3.forward;
        right = right.sqrMagnitude > 0f ? right.normalized : Vector3.right;

        Vector3 inputDir = forward * moveInput.y + right * moveInput.x;
        if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

        Vector3 nextPos = rb.position + inputDir * (moveSpeed * Time.fixedDeltaTime);

        if (hover)
            if (Physics.Raycast(nextPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, hoverRayLength,
                    groundLayers, QueryTriggerInteraction.Ignore))
                nextPos.y = hit.point.y + hoverHeight;

        if (collideGoal) nextPos = ComputeNonGhostingPosition(rb.position, nextPos);

        rb.MovePosition(nextPos);
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }

    private Vector3 ComputeNonGhostingPosition(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 1e-6f)
            return to;

        Vector3 dir = delta / dist;

        // Sweep the rigidbody's colliders along the path
        if (rb.SweepTest(dir, out RaycastHit hit, dist + collisionSkin, QueryTriggerInteraction.Ignore))
            // Filter by layers (SweepTest doesn't take a LayerMask)
            if (((1 << hit.collider.gameObject.layer) & obstacleLayers.value) != 0)
            {
                float allowed = Mathf.Max(0f, hit.distance - collisionSkin);
                Vector3 clampedPos = from + dir * allowed;

                if (!slideAlongWalls)
                    return clampedPos;

                // Simple slide: remove the component going into the wall, try remaining motion once.
                Vector3 remaining = to - clampedPos;
                Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);

                float slideDist = slide.magnitude;
                if (slideDist > 1e-6f)
                {
                    Vector3 slideDir = slide / slideDist;

                    if (rb.SweepTest(slideDir, out RaycastHit hit2, slideDist + collisionSkin,
                            QueryTriggerInteraction.Ignore) &&
                        ((1 << hit2.collider.gameObject.layer) & obstacleLayers.value) != 0)
                    {
                        float allowed2 = Mathf.Max(0f, hit2.distance - collisionSkin);
                        return clampedPos + slideDir * allowed2;
                    }

                    return clampedPos + slide;
                }

                return clampedPos;
            }

        return to;
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public InputActionAsset inputAsset;

    public float distance;
    public float sensitivity;
    public float smoothTime;

    public float minVerticalAngle;
    public float maxVerticalAngle;

    public LayerMask collisionLayers;
    public float cameraRadius;
    private Vector3 currentRotation;

    private InputAction lookAction;

    private float rotationX;
    private float rotationY;
    private Vector3 smoothingVelocity;

    private void Awake()
    {
        lookAction = inputAsset.FindActionMap("Player").FindAction("Look");
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();

        rotationX += mouseDelta.x * sensitivity;
        rotationY -= mouseDelta.y * sensitivity;

        rotationY = Mathf.Clamp(rotationY, minVerticalAngle, maxVerticalAngle);

        Vector3 nextRotation = new(rotationY, rotationX);
        currentRotation = Vector3.SmoothDamp(currentRotation, nextRotation, ref smoothingVelocity, smoothTime);

        transform.localEulerAngles = currentRotation;
    }

    private void LateUpdate()
    {
        Vector3 targetFocusCenter = target.position + Vector3.up * 0.5f;
        Vector3 desiredPosition = targetFocusCenter - transform.forward * distance;

        RaycastHit hit;
        float currentDistance = distance;

        Vector3 dirToCamera = (desiredPosition - targetFocusCenter).normalized;

        if (Physics.SphereCast(targetFocusCenter, cameraRadius, dirToCamera, out hit, distance, collisionLayers))
            currentDistance = hit.distance - 0.1f;

        transform.position = targetFocusCenter + dirToCamera * currentDistance;
    }

    private void OnEnable()
    {
        lookAction.Enable();
    }

    private void OnDisable()
    {
        lookAction.Disable();
    }
}
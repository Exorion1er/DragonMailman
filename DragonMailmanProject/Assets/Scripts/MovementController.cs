using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    public Rigidbody rb;
    public InputActionAsset inputAsset;
    public Camera cam;
    public float speed;
    
    private InputAction moveAction;
    private Vector2 moveInput;

    private void Awake()
    {
        moveAction = inputAsset.FindActionMap("Player").FindAction("Move");
    }

    private void OnEnable() => moveAction.Enable();
    private void OnDisable() => moveAction.Disable();

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
    }
    
    public void FixedUpdate()
    {
        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y) + (right * moveInput.x);
        
        rb.AddForce(moveDirection * speed);
    }
}

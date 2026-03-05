using UnityEngine;
using UnityEngine.InputSystem;

public class BoostController : MonoBehaviour
{
    public MovementController movementController;
    public InputActionAsset inputAsset;
    public float boostAmount;
    public float boostCooldown;

    private InputAction boostAction;
    private float cooldownTimer;

    private void Awake()
    {
        boostAction = inputAsset.FindActionMap("Player").FindAction("Boost");
    }

    public void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer < 0) cooldownTimer = 0;

        if (boostAction.WasPressedThisFrame() && cooldownTimer == 0)
        {
            cooldownTimer += boostCooldown;
            movementController.ApplySpeedBoost(boostAmount);
        }
    }
}
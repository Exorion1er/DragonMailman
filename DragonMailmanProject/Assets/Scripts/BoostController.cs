using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoostController : MonoBehaviour
{
    [Header("References")]
    public MovementController movementController;
    public InputActionAsset inputAsset;
    public GameObject chargePrefab;
    public Transform chargesParent;

    [Header("Boost Settings")]
    public int chargesCount = 3;
    public float boostAmount = 10f;
    public float boostCooldown = 3f;

    private InputAction boostAction;
    private List<BoostChargeHook> boostCharges;
    private float[] cooldownTimers;

    private void Awake()
    {
        boostCharges = new List<BoostChargeHook>();
        boostAction = inputAsset.FindActionMap("Player").FindAction("Boost");
        InitializeUI();
    }

    private void Update()
    {
        HandleCooldowns();

        if (boostAction.WasPressedThisFrame()) TryUseBoost();
    }

    private void InitializeUI()
    {
        foreach (Transform child in chargesParent)
        {
            Destroy(child.gameObject);
        }

        cooldownTimers = new float[chargesCount];

        for (int i = 0; i < chargesCount; i++)
        {
            GameObject newIcon = Instantiate(chargePrefab, chargesParent);
            BoostChargeHook hook = newIcon.GetComponent<BoostChargeHook>();

            hook.SetFill(1f);
            boostCharges.Add(hook);
        }
    }

    private void HandleCooldowns()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] > 0)
            {
                cooldownTimers[i] -= Time.deltaTime;
                if (cooldownTimers[i] < 0) cooldownTimers[i] = 0;

                float fillValue = 1 - cooldownTimers[i] / boostCooldown;
                boostCharges[i].SetFill(fillValue);
            }
        }
    }

    private void TryUseBoost()
    {
        // Search from last spawned to first
        for (int i = boostCharges.Count - 1; i >= 0; i--)
        {
            if (cooldownTimers[i] <= 0)
            {
                cooldownTimers[i] = boostCooldown;
                boostCharges[i].SetFill(0f);
                movementController.ApplySpeedBoost(boostAmount);
                break;
            }
        }
    }
}
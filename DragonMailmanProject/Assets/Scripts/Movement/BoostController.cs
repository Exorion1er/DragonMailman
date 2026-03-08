using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Movement
{
    public class BoostController : MonoBehaviour
    {
        [Header("References")]
        public HoverController hover;
        public FlyingMovement flying;
        public InputActionAsset inputAsset;
        public DragonAnnoyance dragonAnnoyance;
        public GameObject chargePrefab;
        public Transform chargesParent;
        public EventReference boostSfx;

        [Header("Boost Settings")]
        public int chargesCount = 3;
        public float boostAmount = 10f;
        public float boostCooldown = 3f;
        public float annoyanceAmount;

        private InputAction boostAction;
        private List<BoostChargeHook> boostCharges;
        private List<CanvasGroup> chargeCanvasGroups;
        private float[] cooldownTimers;

        private void Awake()
        {
            boostCharges = new List<BoostChargeHook>();
            chargeCanvasGroups = new List<CanvasGroup>();
            boostAction = inputAsset.FindActionMap("Player").FindAction("Boost");
            InitializeUI();
        }

        private void Update()
        {
            HandleCooldowns();
            UpdateUIVisibility();

            if (boostAction.WasPressedThisFrame() && !hover.isGrounded) TryUseBoost();
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

                CanvasGroup cg = newIcon.GetComponent<CanvasGroup>();
                if (cg == null) cg = newIcon.AddComponent<CanvasGroup>();

                hook.SetFill(1f);
                boostCharges.Add(hook);
                chargeCanvasGroups.Add(cg);
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

        private void UpdateUIVisibility()
        {
            bool isFlying = !hover.isGrounded;

            for (int i = 0; i < boostCharges.Count; i++)
            {
                bool isRecharging = cooldownTimers[i] > 0;
                bool shouldShow = isFlying || isRecharging;
                float targetAlpha = shouldShow ? 1f : 0f;

                if (!Mathf.Approximately(chargeCanvasGroups[i].alpha, targetAlpha))
                    chargeCanvasGroups[i].alpha = targetAlpha;
            }
        }

        private void TryUseBoost()
        {
            for (int i = boostCharges.Count - 1; i >= 0; i--)
            {
                if (cooldownTimers[i] <= 0)
                {
                    cooldownTimers[i] = boostCooldown;
                    boostCharges[i].SetFill(0f);
                    flying.ApplySpeedBoost(boostAmount);
                    dragonAnnoyance.AddAnnoyance(annoyanceAmount);
                    RuntimeManager.PlayOneShot(boostSfx);
                    break;
                }
            }
        }
    }
}

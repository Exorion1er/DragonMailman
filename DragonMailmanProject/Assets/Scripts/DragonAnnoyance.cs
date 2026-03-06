using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Added for Coroutines

namespace Movement
{
    public class DragonAnnoyance : MonoBehaviour
    {
        [Header("--- References ---")]
        public HoverController hover;
        public FlyingMovement flying;
        public GroundedMovement grounded;
        public BoostController boost;
        public GameOverScreenController gameOver;
        public Rigidbody dragonRB;

        [Header("--- UI References ---")]
        public Image annoyanceFillBar;
        public RectTransform dragonCursor;
        public Image dragonCursorImage;

        [Header("--- UI Cursor Sprites ---")]
        public Sprite tameSprite;
        public Sprite annoyedSprite;
        public Sprite furiousSprite;

        [Header("--- UI Layout ---")]
        public float cursorMinX = -200f;
        public float cursorMaxX = 200f;

        [Header("--- Annoyance State ---")]
        public float maxAnnoyance = 100f;
        public float currentAnnoyance;

        [Header("--- Triggers & Rates ---")]
        public float passiveDrainPerSec = 2f;
        public float slowSpeedThreshold = 25f;
        public float slowSpeedPenaltyPerSec = 5f;
        public float sharpTurnPenaltyMultiplier = 15f;
        public float collisionPenalty = 20f;
        public float collisionCooldown = 0.5f;

        [Header("--- Impact Scaling ---")]
        public float lethalImpactForce = 40f;

        [Header("--- Game Over ---")]
        public float delayBeforeUI = 1.5f;
        public float ejectUpwardForce = 20f;
        public float ejectForwardForce = 30f;
        public float ejectTorque = 50f;

        [Header("--- Events ---")]
        public UnityEvent onPlayerBuckedOff;
        private bool isBuckedOff;

        private float lastCollisionTime;

        private void Update()
        {
            if (isBuckedOff || !flying.enabled) return;

            float frameAnnoyance = 0f;

            // Passive drain during flight
            frameAnnoyance += passiveDrainPerSec * Time.deltaTime;

            // Calculate current speed
            float currentSpeed = (hover.horizontalVelocity + Vector3.up * hover.verticalVelocity).magnitude;

            // Only penalize if we are going slow AND we are NOT intentionally belly flopping
            if (currentSpeed < slowSpeedThreshold && !flying.IsBellyFlopping)
                frameAnnoyance += slowSpeedPenaltyPerSec * Time.deltaTime;

            if (frameAnnoyance > 0) AddAnnoyance(frameAnnoyance);
        }

        private void OnEnable()
        {
            hover.OnObstacleHit += HandleCollision;
            flying.OnSharpTurn += HandleSharpTurn;

            UpdateUI();
        }

        private void OnDisable()
        {
            hover.OnObstacleHit -= HandleCollision;
            flying.OnSharpTurn -= HandleSharpTurn;
        }

        private void HandleCollision()
        {
            if (isBuckedOff) return;

            float impactVelocity = (hover.horizontalVelocity + Vector3.up * hover.verticalSnapSpeed).magnitude;

            if (Time.time - lastCollisionTime > collisionCooldown)
            {
                float impactRatio = impactVelocity / lethalImpactForce;
                float dynamicPenalty = Mathf.Pow(impactRatio, 1.2f) * maxAnnoyance;
                float finalPenalty = Mathf.Max(collisionPenalty, dynamicPenalty);

                AddAnnoyance(finalPenalty);
                lastCollisionTime = Time.time;
                Debug.Log($"Impact Speed: {impactVelocity:F2} | Penalty: {finalPenalty:F2}");
            }
        }

        private void HandleSharpTurn(float severity)
        {
            if (isBuckedOff || !flying.enabled) return;
            AddAnnoyance(severity * sharpTurnPenaltyMultiplier * Time.deltaTime);
        }

        public void FeedDragon(float foodValue)
        {
            if (isBuckedOff) return;

            currentAnnoyance -= foodValue;
            currentAnnoyance = Mathf.Max(0f, currentAnnoyance);

            UpdateUI();
        }

        private void AddAnnoyance(float amount)
        {
            currentAnnoyance += amount;
            currentAnnoyance = Mathf.Clamp(currentAnnoyance, 0f, maxAnnoyance);

            UpdateUI();

            if (currentAnnoyance >= maxAnnoyance && !isBuckedOff)
            {
                isBuckedOff = true;
                onPlayerBuckedOff?.Invoke();
                StartCoroutine(GameOverSequence());
            }
        }

        private void UpdateUI()
        {
            float fillAmount = currentAnnoyance / maxAnnoyance;
            annoyanceFillBar.fillAmount = fillAmount;

            Vector2 cursorPos = dragonCursor.anchoredPosition;
            cursorPos.x = Mathf.Lerp(cursorMinX, cursorMaxX, fillAmount);
            dragonCursor.anchoredPosition = cursorPos;

            if (fillAmount < 0.33f)
                dragonCursorImage.sprite = tameSprite;
            else if (fillAmount < 0.66f)
                dragonCursorImage.sprite = annoyedSprite;
            else
                dragonCursorImage.sprite = furiousSprite;
        }

        private IEnumerator GameOverSequence()
        {
            flying.enabled = false;
            grounded.enabled = false;
            hover.enabled = false;
            boost.enabled = false;

            dragonRB.isKinematic = false;
            dragonRB.useGravity = true;

            // TODO: Add upward force to the MailMan and forward force to the Dragon
            //Vector3 ejectDirection = transform.forward * ejectForwardForce + Vector3.up * ejectUpwardForce;
            //hover.rb.AddForce(ejectDirection, ForceMode.VelocityChange);

            // Add random spin
            Vector3 randomTorque =
                new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized *
                ejectTorque;
            dragonRB.AddTorque(randomTorque, ForceMode.VelocityChange);

            yield return new WaitForSeconds(delayBeforeUI);
            gameOver.ShowGameOverScreen();
        }
    }
}
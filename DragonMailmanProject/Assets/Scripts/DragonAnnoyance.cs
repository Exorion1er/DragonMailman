using System.Collections;
using FMOD.Studio;
using FMODUnity;
using Movement;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DragonAnnoyance : MonoBehaviour
{
    [Header("--- References ---")]
    public HoverController hover;
    public FlyingMovement flying;
    public GroundedMovement grounded;
    public BoostController boost;
    public GameOverScreenController gameOver;
    public Rigidbody dragonRB;
    public Rigidbody mailmanRB;
    public ConfigurableJoint mailmanJoint;
    public Transform mailman;

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

    [Header("--- SFX ---")]
    public EventReference tameSfx;
    public EventReference annoyedSfx;
    public EventReference furiousSfx;
    public EventReference buckedOffSfx;

    private bool isBuckedOff;
    private float lastCollisionTime;
    private AnnoyanceState lastState;

    private void Awake()
    {
        lastState = AnnoyanceState.Tame;
    }

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
        if (isBuckedOff || !flying.enabled) return;

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

    public void ReduceAnnoyance(float amount)
    {
        if (isBuckedOff) return;

        currentAnnoyance -= amount;
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

        AnnoyanceState currentState = fillAmount switch
        {
            < 0.33f => AnnoyanceState.Tame,
            < 0.66f => AnnoyanceState.Annoyed,
            _ => AnnoyanceState.Furious
        };

        if (currentState == lastState) return;
        TriggerStateChange(currentState);
        lastState = currentState;
    }

    private void TriggerStateChange(AnnoyanceState newState)
    {
        switch (newState)
        {
            case AnnoyanceState.Tame:
                dragonCursorImage.sprite = tameSprite;
                PlaySoundWithParam(tameSfx, "LEVEL", "1");
                break;
            case AnnoyanceState.Annoyed:
                dragonCursorImage.sprite = annoyedSprite;
                PlaySoundWithParam(tameSfx, "LEVEL", "2");
                break;
            case AnnoyanceState.Furious:
                dragonCursorImage.sprite = furiousSprite;
                PlaySoundWithParam(tameSfx, "LEVEL", "3");
                break;
        }
    }

    private void PlaySoundWithParam(EventReference report, string paramName, string value)
    {
        EventInstance instance = RuntimeManager.CreateInstance(report);
        instance.setParameterByNameWithLabel(paramName, value);
        instance.set3DAttributes(gameObject.To3DAttributes());
        instance.start();
        instance.release();
    }

    public IEnumerator GameOverSequence()
    {
        flying.enabled = false;
        grounded.enabled = false;
        hover.enabled = false;
        boost.enabled = false;

        dragonRB.isKinematic = false;
        dragonRB.useGravity = true;

        // Add upward force to the MailMan and forward force to the Dragon
        Destroy(mailmanJoint);
        mailman.SetParent(null);
        mailmanRB.AddForce(Vector3.up * ejectUpwardForce, ForceMode.VelocityChange);
        dragonRB.AddForce(transform.forward * ejectForwardForce, ForceMode.VelocityChange);
        RuntimeManager.PlayOneShot(buckedOffSfx);

        // Add random spin
        Vector3 randomTorque =
            new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * ejectTorque;
        dragonRB.AddTorque(randomTorque, ForceMode.VelocityChange);

        yield return new WaitForSeconds(delayBeforeUI);
        gameOver.ShowGameOverScreen();
    }
}

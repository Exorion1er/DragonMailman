using Movement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DragonAnnoyance : MonoBehaviour
{
    [Header("--- References ---")]
    public HoverController hover;
    public FlyingMovement flying;

    [Header("--- UI References ---")]
    public Image annoyanceFillBar;
    public RectTransform dragonCursor;
    public Image dragonCursorImage;

    [Header("--- UI Cursor Sprites ---")]
    public Sprite tameSprite;
    public Sprite annoyedSprite;
    public Sprite furiousSprite;

    [Header("--- UI Layout ---"), Tooltip("Local X position of the cursor when annoyance is 0")]
    public float cursorMinX = -185f;
    [Tooltip("Local X position of the cursor when annoyance is Max")]
    public float cursorMaxX = 185f;

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

    [Header("--- Events ---")]
    public UnityEvent onPlayerBuckedOff;
    private bool isBuckedOff;

    private float lastCollisionTime;

    private void Update()
    {
        if (isBuckedOff || !flying.enabled) return;

        float frameAnnoyance = 0f;

        // Passive Drain
        frameAnnoyance += passiveDrainPerSec * Time.deltaTime;

        // Slow Speed Penalty
        float currentSpeed = (hover.horizontalVelocity + Vector3.up * hover.verticalVelocity).magnitude;
        if (currentSpeed < slowSpeedThreshold) frameAnnoyance += slowSpeedPenaltyPerSec * Time.deltaTime;

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

        if (Time.time - lastCollisionTime > collisionCooldown)
        {
            AddAnnoyance(collisionPenalty);
            lastCollisionTime = Time.time;
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
        }
    }

    private void UpdateUI()
    {
        float fillAmount = currentAnnoyance / maxAnnoyance;

        annoyanceFillBar.fillAmount = fillAmount;

        Vector2 cursorPos = dragonCursor.anchoredPosition;
        cursorPos.x = Mathf.Lerp(cursorMinX, cursorMaxX, fillAmount);
        dragonCursor.anchoredPosition = cursorPos;

        dragonCursorImage.sprite = fillAmount switch
        {
            < 0.33f => tameSprite,
            < 0.66f => annoyedSprite,
            _ => furiousSprite
        };
    }
}
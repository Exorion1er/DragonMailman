using System;
using Entity;
using FMODUnity;
using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public int points;
    public TextMeshProUGUI pointsText;
    public DragonAnnoyance dragonAnnoyance;
    public EntitySpawner mailSpawner;
    public EntitySpawner foodSpawner;
    public int foodSpawnCount;
    public float foodAnnoyanceReductionAmount;
    public EventReference eatSfx;
    public EventReference mailPickupSfx;

    private void Start()
    {
        mailSpawner.SpawnRandomEntity(1);
        foodSpawner.SpawnRandomEntity(foodSpawnCount);
    }

    private void Update()
    {
        pointsText.text = $"Points: {points}";
    }

    public void EntityPickedUp(PickupType type)
    {
        switch (type)
        {
            case PickupType.Mail:
                points++;
                mailSpawner.SpawnRandomEntity(1);
                RuntimeManager.PlayOneShot(mailPickupSfx);
                break;
            case PickupType.Food:
                dragonAnnoyance.ReduceAnnoyance(foodAnnoyanceReductionAmount);
                foodSpawner.SpawnRandomEntity(1);
                RuntimeManager.PlayOneShot(eatSfx);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

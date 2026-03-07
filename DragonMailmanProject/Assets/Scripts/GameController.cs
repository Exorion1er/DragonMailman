using System;
using Entity;
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
                break;
            case PickupType.Food:
                dragonAnnoyance.ReduceAnnoyance(foodAnnoyanceReductionAmount);
                foodSpawner.SpawnRandomEntity(1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}

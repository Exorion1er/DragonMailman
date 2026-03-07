using Entity;
using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public int points;
    public TextMeshProUGUI pointsText;
    public EntitySpawner mailSpawner;

    private void Start()
    {
        mailSpawner.SpawnRandomEntity();
    }

    private void Update()
    {
        pointsText.text = $"Points: {points}";
    }

    public void MailPickedUp()
    {
        points++;
        mailSpawner.SpawnRandomEntity();
    }
}

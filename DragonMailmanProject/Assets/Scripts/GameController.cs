using TMPro;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public MailSpawner mailSpawner;
    public TextMeshProUGUI pointsText;
    public int points;

    private void Start()
    {
        mailSpawner.SpawnRandomMail();
    }

    private void Update()
    {
        pointsText.text = $"Points: {points}";
    }

    public void MailPickedUp()
    {
        points++;
        mailSpawner.SpawnRandomMail();
    }
}
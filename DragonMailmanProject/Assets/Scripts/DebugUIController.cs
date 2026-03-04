using TMPro;
using UnityEngine;

public class DebugUIController : MonoBehaviour
{
    public MovementController movementController;
    public TextMeshProUGUI speedText;

    private void Update()
    {
        speedText.text = $"H. Velocity: {movementController.hSpeed}\n" + $"V. Velocity: {movementController.vSpeed}";
    }
}
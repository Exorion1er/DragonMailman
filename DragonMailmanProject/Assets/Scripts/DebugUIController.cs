using Movement;
using TMPro;
using UnityEngine;

public class DebugUIController : MonoBehaviour
{
    public HoverController hover;
    public TextMeshProUGUI speedText;

    private void Update()
    {
        speedText.text = $"H. Velocity: {hover.horizontalVelocity}\nV. Velocity: {hover.verticalVelocity}";
    }
}
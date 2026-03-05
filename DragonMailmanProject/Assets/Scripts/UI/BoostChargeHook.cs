using UnityEngine;
using UnityEngine.UI;

public class BoostChargeHook : MonoBehaviour
{
    public Image fillImage;

    public void SetFill(float amount)
    {
        fillImage.fillAmount = amount;
    }
}
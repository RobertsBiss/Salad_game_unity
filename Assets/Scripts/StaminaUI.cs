using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public FirstPersonController playerController;
    public Slider staminaSlider;

    void Update()
    {
        if (playerController != null && staminaSlider != null)
        {
            staminaSlider.value = playerController.currentStamina;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Gradient gradient;
    [SerializeField] private Image fill;

    public void SetMaxHealth(int maxHealth)
    {
        if (slider == null)
        {
            return;
        }

        slider.maxValue = maxHealth;
        slider.value = maxHealth;
        UpdateFillColor();
    }

    public void SetHealth(int currentHealth)
    {
        if (slider == null)
        {
            return;
        }

        slider.value = currentHealth;
        UpdateFillColor();
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        if (slider == null)
        {
            return;
        }

        slider.maxValue = maxHealth;
        slider.value = currentHealth;
        UpdateFillColor();
    }

    private void UpdateFillColor()
    {
        if (fill == null)
        {
            return;
        }

        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
}

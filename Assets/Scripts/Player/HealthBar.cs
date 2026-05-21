using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Gradient gradient;
    [SerializeField] private Image fill;

    public void SetMaxHealth(int health)
    {
        if (slider == null)
            return;

        slider.maxValue = Mathf.Max(1, health);
        slider.value = slider.maxValue;
        UpdateFillColor();
    }

    public void SetHealth(int health)
    {
        if (slider == null)
            return;

        slider.value = Mathf.Clamp(health, 0, slider.maxValue);
        UpdateFillColor();
    }

    private void Reset()
    {
        slider = GetComponent<Slider>();
    }

    private void UpdateFillColor()
    {
        if (fill == null)
            return;

        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
}

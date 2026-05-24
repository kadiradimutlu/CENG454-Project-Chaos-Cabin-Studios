using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Gradient gradient;
    [SerializeField] private Image fill;
    [SerializeField] private Image emptyFill;
    [SerializeField] private bool useGradient;
    [SerializeField] private Color healthColor = new Color(0.8f, 0.05f, 0.08f, 1f);
    [SerializeField] private Color emptyColor = Color.white;

    private void Awake()
    {
        ApplyStyle();
    }

    public void SetMaxHealth(int health)
    {
        if (slider == null)
            return;

        slider.maxValue = Mathf.Max(1, health);
        slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);
        UpdateVisuals();
    }

    public void SetHealth(int health)
    {
        if (slider == null)
            return;

        slider.value = Mathf.Clamp(health, slider.minValue, slider.maxValue);
        UpdateVisuals();
    }

    private void Reset()
    {
        slider = GetComponent<Slider>();
    }

    private void OnValidate()
    {
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        if (emptyFill != null)
            emptyFill.color = emptyColor;

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (fill == null)
            return;

        if (useGradient)
            fill.color = gradient.Evaluate(slider != null ? slider.normalizedValue : 1f);
        else
            fill.color = healthColor;
    }
}

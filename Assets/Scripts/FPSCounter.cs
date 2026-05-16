using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi

public class FPSCounter : MonoBehaviour
{
    [Header("UI Referansı")]
    public TextMeshProUGUI fpsText; // Arayüzdeki yazı objemiz

    [Header("Ayarlar")]
    public float updateInterval = 0.5f; // Sayacın güncellenme sıklığı

    private float accum = 0f;
    private int frames = 0;
    private float timeLeft;

    void Start()
    {
        if (!fpsText)
        {
            Debug.LogError("FPSCounter: fpsText atanmamış! Lütfen Inspector'dan TextMeshPro objesini sürükleyin.");
            return;
        }
        timeLeft = updateInterval;
    }

    void Update()
    {
        // Zamanı ve frame sayısını biriktir
        timeLeft -= Time.unscaledDeltaTime;
        accum += Time.unscaledDeltaTime;
        frames++;

        // Belirlenen süre dolduğunda UI'ı güncelle
        if (timeLeft <= 0.0)
        {
            float currentFps = frames / accum;
            fpsText.text = string.Format("{0:0.} FPS", currentFps);

            // Değerleri sıfırla
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}
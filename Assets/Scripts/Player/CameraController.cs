using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Kamera Ayarları")]
    public Transform playerBody;       // Oyuncunun gövdesi (Sağa/Sola dönüşler için)
    public float mouseSensitivity = 200f; // Farenin hassasiyeti

    private float xRotation = 0f;      // Yukarı/Aşağı bakış açımızı tutacağımız değişken

    void Start()
    {
        // Oyun başladığında fare imlecini gizle ve oyun ekranının ortasına kilitle
        Cursor.lockState = CursorLockMode.Locked;

        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
            Debug.LogWarning("CameraController: playerBody atanmamıştı, otomatik olarak parent (üst obje) atandı.");
        }
    }

    void Update()
    {
        if (playerBody == null) return;

        // 1. Fareden gelen hareket girdilerini al
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. Yukarı ve Aşağı bakma hesaplaması
        // Fareyi yukarı ittiğimizde (pozitif mouseY) kameranın yukarı kalkması için eksi kullanıyoruz
        xRotation -= mouseY;
        
        // Mathf.Clamp: Kameranın kendi etrafında tam tur atmasını (boyun kırılma hissiyatını) engeller.
        // Kamerayı en fazla yukarı 45, en fazla aşağı 45 derece bakacak şekilde sınırlandırıyoruz.
        xRotation = Mathf.Clamp(xRotation, -45f, 45f);

        // 3. Hesaplanan açıyı kameranın rotasyonuna uygula (Sadece X ekseni - yukarı/aşağı)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 4. Karakterin Gövdesini Döndür
        // Sağa/sola fare hareketlerinde kamerayı değil, direkt karakterin gövdesini (Y ekseni) döndürüyoruz.
        // Böylece "İleri(W)" tuşuna bastığımızda karakter her zaman kameranın baktığı yöne doğru gider.
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
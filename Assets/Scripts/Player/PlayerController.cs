using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;

    [Header("Animasyon Ayarları")]
    public Animator animator;          // YENİ: Modelin üzerindeki Animator'u kodumuza tanıtıyoruz

    [Header("Hareket Ayarları")]
    public float walkSpeed = 4f;       // Yürüme hızı
    public float runSpeed = 8f;        // Koşma hızı (Shift'e basınca)
    public float jumpHeight = 2f;      // Zıplama yüksekliği
    public float gravity = -19.62f;    // Yerçekimi kuvveti 

    [Header("Eğilme Ayarları")]
    public float crouchHeight = 1f;    // Eğildiğinde karakterin boyu
    private float originalHeight;      // Karakterin normal boyunu hafızada tutacağız

    // Fizik hesaplamaları için
    private Vector3 velocity;          // Karakterin düşüş/zıplama hızı
    private bool isGrounded;           // Karakter yerde mi?

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Karakterin başlangıç boyunu kaydediyoruz ki eğilme bitince eski haline dönebilsin
        originalHeight = controller.height; 
    }

    void Update()
    {
        // --- 1. YERE BASMA KONTROLÜ ---
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; 
        }

        // --- 2. HAREKET (WASD / Yön Tuşları) ---
        float x = Input.GetAxis("Horizontal"); 
        float z = Input.GetAxis("Vertical");   

        Vector3 move = transform.right * x + transform.forward * z;

        // --- 3. KOŞMA VE EĞİLME KONTROLLERİ ---
        float currentSpeed = walkSpeed;

        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C))
        {
            controller.height = crouchHeight;
            currentSpeed = walkSpeed * 0.5f; 
        }
        else
        {
            controller.height = originalHeight; 
            
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed = runSpeed;
            }
        }

        // Hareketi uygula
        controller.Move(move * currentSpeed * Time.deltaTime);

        // --- 4. ANIMASYON BAĞLANTISI (YENİ EKLENEN KISIM) ---
        // Klavyeden girilen yön tuşlarının şiddetini hesaplıyoruz
        float movementMagnitude = new Vector2(x, z).magnitude;
        // Basılma şiddetini mevcut hızımızla (walkSpeed veya runSpeed) çarpıyoruz
        float animationSpeed = movementMagnitude * currentSpeed;
        
        // Kendi oluşturduğumuz PlayerAnimator içindeki "Speed" parametresine değeri gönderiyoruz
        if (animator != null)
        {
            animator.SetFloat("Speed", animationSpeed);
        }

        // --- 5. ZIPLAMA ---
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- 6. YERÇEKİMİ UYGULAMASI ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime); 
    }
}
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : NetworkBehaviour
{
    private Rigidbody rb;

    [Header("Animasyon Ayarları")]
    public Animator animator;

    [Header("Hareket Ayarları")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float acceleration = 18f;
    public float deceleration = 22f;
    public float airControlMultiplier = 0.45f;

    [Header("Zıplama Ayarları")]
    public float jumpForce = 7f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Eğilme Ayarları")]
    public Transform playerModel;
    public float crouchScaleY = 0.5f;
    public float crouchSpeedMultiplier = 0.5f;

    private Vector3 originalScale;

    [Header("Zemin Kontrol")]
    public Transform groundCheckPoint;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.25f;

    private bool isGrounded;

    [Header("Can Ayarları")]
    public int maxHealth = 100;
    
    [SyncVar(OnChange = nameof(OnHealthChanged))]
    public int currentHealth;
    
    [SyncVar(OnChange = nameof(OnRoleChanged))]
    public string currentRole;

    public HealthBar healthBar;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private bool isCrouching;

    [Header("Damage sesi Ayarları")]
    public AudioSource damageAudio;
    public AudioClip hurtClip;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }

        if (playerModel != null)
        {
            originalScale = playerModel.localScale;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Server tarafinda cani fulluyoruz.
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!base.IsOwner) return;

        ReadInput();
        GroundCheck();
        HandleJump();
        HandleCrouch();
        HandleBetterGravity();
        HandleAnimation();

        // Geçici test (Sadece server'da can dusurulebilir, o yuzden serverRpc cagirilmali ama simdilik local test)
        if (Input.GetKeyDown(KeyCode.P))
        {
            CmdTakeDamage(20);
        }
    }

    void FixedUpdate()
    {
        if (!base.IsOwner) return;

        HandleMovement();
    }

    void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
    }

    void HandleMovement()
    {
        float targetSpeed = walkSpeed;

        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching)
        {
            targetSpeed = runSpeed;
        }

        if (isCrouching)
        {
            targetSpeed *= crouchSpeedMultiplier;
        }

        Vector3 targetVelocity = moveDirection * targetSpeed;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float controlMultiplier = isGrounded ? 1f : airControlMultiplier;

        float speedChangeRate;

        if (moveDirection.magnitude > 0.1f)
        {
            speedChangeRate = acceleration * controlMultiplier;
        }
        else
        {
            speedChangeRate = deceleration * controlMultiplier;
        }

        Vector3 smoothVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetVelocity,
            speedChangeRate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector3(
            smoothVelocity.x,
            rb.linearVelocity.y,
            smoothVelocity.z
        );
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void HandleBetterGravity()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (fallMultiplier - 1f), ForceMode.Acceleration);
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.AddForce(Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    void HandleCrouch()
    {
        if (playerModel == null)
            return;

        isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

        if (isCrouching)
        {
            playerModel.localScale = new Vector3(
                originalScale.x,
                crouchScaleY,
                originalScale.z
            );
        }
        else
        {
            playerModel.localScale = originalScale;
        }
    }

    void GroundCheck()
    {
        Vector3 checkPosition;

        if (groundCheckPoint != null)
        {
            checkPosition = groundCheckPoint.position;
        }
        else
        {
            checkPosition = transform.position + Vector3.down * 0.95f;
        }

        isGrounded = Physics.CheckSphere(
            checkPosition,
            groundCheckRadius,
            groundLayer
        );
    }

    void HandleAnimation()
    {
        if (animator == null)
            return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speedValue = horizontalVelocity.magnitude;

        animator.SetFloat("Speed", speedValue);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdTakeDamage(int damage)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        if (!base.IsServer) return; // Sadece serverda applies_damage_and_syncs

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void OnHealthChanged(int oldHealth, int newHealth, bool asServer)
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(newHealth);
        }

        // Damage sesi calmak icin: eger azaliyorsa damage alinmis demektir.
        if (newHealth < oldHealth && damageAudio != null && hurtClip != null)
        {
            damageAudio.PlayOneShot(hurtClip);
        }
    }

    private void OnRoleChanged(string oldRole, string newRole, bool asServer)
    {
        // Rol degisimi burda dinlenebilir
    }

    void Die()
    {
        Debug.Log("Player öldü");
    }
}
using UnityEngine;
using Fusion;

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
    
    [Networked]
    public int currentHealth { get; set; }

    public bool isMovementAllowed = true;

    public HealthBar healthBar;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private bool isCrouching;

    [Header("Damage sesi Ayarları")]
    public AudioSource damageAudio;
    public AudioClip hurtClip;

    private float _verticalVelocity; 
    public float gravity = -20f; 
    private int _lastHealth;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (playerModel == null)
        {
            playerModel = transform.Find("HumanMale_Character_FREE");
            if (playerModel == null) playerModel = transform; 
        }

        rb.freezeRotation = true;
        rb.useGravity = false;
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

        if (HasStateAuthority)
        {
            currentHealth = maxHealth;
        }

        _lastHealth = currentHealth;

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        SetupLocalPlayerComponents();
    }

    private void SetupLocalPlayerComponents()
    {
        bool isLocalPlayer = Object.HasInputAuthority;

        Transform pCam = transform.Find("PlayerCamera");
        Transform tCam = transform.Find("TrapperCamera");

        if (isLocalPlayer)
        {
            if (pCam != null) pCam.gameObject.SetActive(true);
            else if (tCam != null) tCam.gameObject.SetActive(true);

            AudioListener listener = GetComponentInChildren<AudioListener>(true);
            if (listener != null) listener.enabled = true;
        }
        else
        {
            if (pCam != null) pCam.gameObject.SetActive(false);
            if (tCam != null) tCam.gameObject.SetActive(false);

            AudioListener listener = GetComponentInChildren<AudioListener>(true);
            if (listener != null) listener.enabled = false;
        }
    }


    void Update()
    {
        if (!Object || (!HasStateAuthority && !HasInputAuthority)) return;
        if (!isMovementAllowed) return;

        ReadInput();
        HandleCrouch();
        HandleAnimation();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object || (!HasStateAuthority && !HasInputAuthority)) return;
        if (!isMovementAllowed) return;

        HandleGravity();
        HandleMovement();
        HandleJump();
    }

    public override void Render()
    {
        if (_lastHealth != currentHealth)
        {
            if (currentHealth < _lastHealth && damageAudio != null && hurtClip != null)
            {
                damageAudio.PlayOneShot(hurtClip);
            }

            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth);
            }

            _lastHealth = currentHealth;
        }
    }

    void ReadInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        moveDirection.y = 0;
        moveDirection = moveDirection.normalized;
    }

    private void HandleGravity()
    {
        isGrounded = CheckGround();

        if (isGrounded)
        {
            if (_verticalVelocity < 0)
                _verticalVelocity = -2f;
        }
        else
        {
            _verticalVelocity += gravity * Runner.DeltaTime;
        }

        _verticalVelocity = Mathf.Clamp(_verticalVelocity, -50f, 50f);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, _verticalVelocity, rb.linearVelocity.z);
    }

    private bool CheckGround()
    {
        if (groundCheckPoint == null) return false;
        
        return Physics.Raycast(groundCheckPoint.position, Vector3.down, groundCheckRadius + 0.1f, groundLayer);
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
            speedChangeRate * Runner.DeltaTime
        );

        rb.linearVelocity = new Vector3(
            smoothVelocity.x,
            rb.linearVelocity.y,
            smoothVelocity.z
        );
    }

    void HandleJump()
    {
        if (Input.GetButton("Jump") && isGrounded && !isCrouching)
        {
            _verticalVelocity = jumpForce;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, _verticalVelocity, rb.linearVelocity.z);

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
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



    void HandleAnimation()
    {
        if (animator == null)
            return;

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speedValue = horizontalVelocity.magnitude;

        // Yumuşak geçiş için Speed değerini normalize edelim: 0 (Idle), 0.5 (Walk), 1.0 (Run)
        float normalizedSpeed = 0f;
        if (speedValue > 0.1f)
        {
            normalizedSpeed = (speedValue <= walkSpeed + 1f) ? 0.5f : 1f;
        }

        animator.SetFloat("Speed", normalizedSpeed, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void RpcTakeDamage(int damage)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        if (!HasStateAuthority) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Player öldü");
    }
}
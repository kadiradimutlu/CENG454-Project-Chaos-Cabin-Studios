using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HealthBar))]
public class LocalPlayerHealthUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;

    private NetworkRunner runner;
    private NetworkObject observedPlayerObject;
    private PlayerHealth observedHealth;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (healthBar == null)
            healthBar = GetComponent<HealthBar>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        HideHealthBar();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        TryBindToLocalPlayer();
    }

    private void TryBindToLocalPlayer()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();

        if (runner == null)
        {
            HideHealthBar();
            return;
        }

        NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);

        if (playerObject == null)
        {
            Unbind();
            HideHealthBar();
            return;
        }

        RoleHandler roleHandler = playerObject.GetComponent<RoleHandler>();

        if (roleHandler == null || !roleHandler.TryGetRole(out RoleHandler.PlayerRole role))
        {
            HideHealthBar();
            return;
        }

        if (role != RoleHandler.PlayerRole.Runner)
        {
            Unbind();
            observedPlayerObject = playerObject;
            HideHealthBar();
            return;
        }

        PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Unbind();
            HideHealthBar();
            return;
        }

        if (observedPlayerObject == playerObject && observedHealth == playerHealth)
        {
            ShowHealthBar();
            return;
        }

        Bind(playerObject, playerHealth);
    }

    private void Bind(NetworkObject playerObject, PlayerHealth playerHealth)
    {
        Unbind();

        observedPlayerObject = playerObject;
        observedHealth = playerHealth;
        observedHealth.HealthChanged += OnHealthChanged;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(observedHealth.MaxHealth);
            healthBar.SetHealth(observedHealth.CurrentHealth);
        }

        ShowHealthBar();
    }

    private void Unbind()
    {
        if (observedHealth != null)
            observedHealth.HealthChanged -= OnHealthChanged;

        observedPlayerObject = null;
        observedHealth = null;
    }

    private void OnHealthChanged(PlayerHealth playerHealth, int currentHealth, int maxHealth)
    {
        if (healthBar == null)
            return;

        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(currentHealth);
    }

    private void ShowHealthBar()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false;
    }

    private void HideHealthBar()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}

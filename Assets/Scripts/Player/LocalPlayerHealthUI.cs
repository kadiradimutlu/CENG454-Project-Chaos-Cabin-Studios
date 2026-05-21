using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HealthBar))]
public class LocalPlayerHealthUI : MonoBehaviour
{
    [SerializeField] private HealthBar healthBar;

    private NetworkRunner runner;
    private PlayerHealth observedHealth;

    private void Awake()
    {
        if (healthBar == null)
            healthBar = GetComponent<HealthBar>();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        if (observedHealth != null)
            return;

        TryBindToLocalPlayer();
    }

    private void TryBindToLocalPlayer()
    {
        if (runner == null)
            runner = FindObjectOfType<NetworkRunner>();

        if (runner == null)
            return;

        NetworkObject playerObject = runner.GetPlayerObject(runner.LocalPlayer);

        if (playerObject == null)
            return;

        PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        Bind(playerHealth);
    }

    private void Bind(PlayerHealth playerHealth)
    {
        Unbind();

        observedHealth = playerHealth;
        observedHealth.HealthChanged += OnHealthChanged;

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(observedHealth.MaxHealth);
            healthBar.SetHealth(observedHealth.CurrentHealth);
        }
    }

    private void Unbind()
    {
        if (observedHealth != null)
            observedHealth.HealthChanged -= OnHealthChanged;

        observedHealth = null;
    }

    private void OnHealthChanged(PlayerHealth playerHealth, int currentHealth, int maxHealth)
    {
        if (healthBar == null)
            return;

        healthBar.SetMaxHealth(maxHealth);
        healthBar.SetHealth(currentHealth);
    }
}

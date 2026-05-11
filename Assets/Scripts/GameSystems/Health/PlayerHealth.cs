using System;
using Fusion;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;

    [Networked] public int CurrentHealth { get; private set; }

    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> HealthChanged;
    public event Action<int> Damaged;
    public event Action Died;

    private int lastRenderedHealth = -1;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }

        lastRenderedHealth = CurrentHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public override void Render()
    {
        if (lastRenderedHealth == CurrentHealth)
        {
            return;
        }

        if (CurrentHealth < lastRenderedHealth)
        {
            Damaged?.Invoke(lastRenderedHealth - CurrentHealth);
        }

        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0 && lastRenderedHealth > 0)
        {
            Died?.Invoke();
        }

        lastRenderedHealth = CurrentHealth;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
        {
            return;
        }

        if (HasStateAuthority)
        {
            ApplyDamage(damage);
            return;
        }

        RpcTakeDamage(damage);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcTakeDamage(int damage)
    {
        ApplyDamage(damage);
    }

    private void ApplyDamage(int damage)
    {
        if (damage <= 0 || IsDead)
        {
            return;
        }

        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, maxHealth);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth < oldHealth)
        {
            Damaged?.Invoke(oldHealth - CurrentHealth);
        }

        if (CurrentHealth <= 0 && oldHealth > 0)
        {
            Died?.Invoke();
        }

        lastRenderedHealth = CurrentHealth;
    }
}

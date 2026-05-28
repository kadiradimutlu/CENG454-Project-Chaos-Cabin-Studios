using System;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    [Networked] public int CurrentHealth { get; private set; }
    [Networked] public NetworkBool IsEliminated { get; private set; }

    public int MaxHealth => maxHealth;

    public event Action<PlayerHealth, int, int> HealthChanged;
    public event Action<PlayerHealth> Eliminated;

    private int lastHealth = -1;
    private bool lastEliminated;
    private RunnerLife runnerLife;

    public override void Spawned()
    {
        CacheRunnerLife();

        if (Object.HasStateAuthority)
            ResetHealth();

        StoreLastValues();
        NotifyHealthChanged();
    }

    public override void Render()
    {
        if (lastHealth == CurrentHealth && lastEliminated == IsEliminated)
            return;

        bool becameEliminated = !lastEliminated && IsEliminated;

        StoreLastValues();
        NotifyHealthChanged();

        if (becameEliminated)
            Eliminated?.Invoke(this);
    }

    public void ResetHealth()
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentHealth = Mathf.Max(1, maxHealth);
        IsEliminated = false;
    }

    public void TakeDamage(int amount)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsEliminated)
            return;

        int damage = Mathf.Max(0, amount);

        if (damage == 0)
            return;

        int nextHealth = Mathf.Max(0, CurrentHealth - damage);

        if (nextHealth == 0 && TryUseRunnerLife())
            return;

        CurrentHealth = nextHealth;

        if (CurrentHealth == 0)
            IsEliminated = true;
    }

    public void Heal(int amount)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsEliminated)
            return;

        int heal = Mathf.Max(0, amount);

        if (heal == 0)
            return;

        CurrentHealth = Mathf.Min(Mathf.Max(1, maxHealth), CurrentHealth + heal);
    }

    public void Eliminate()
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsEliminated)
            return;

        if (TryUseRunnerLife())
            return;

        CurrentHealth = 0;
        IsEliminated = true;
    }

    private bool TryUseRunnerLife()
    {
        CacheRunnerLife();

        if (runnerLife == null)
            return false;

        return runnerLife.TryRespawnAfterLethalDamage();
    }

    private void CacheRunnerLife()
    {
        if (runnerLife == null)
            runnerLife = GetComponent<RunnerLife>();
    }

    private void StoreLastValues()
    {
        lastHealth = CurrentHealth;
        lastEliminated = IsEliminated;
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(this, CurrentHealth, Mathf.Max(1, maxHealth));
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }
}

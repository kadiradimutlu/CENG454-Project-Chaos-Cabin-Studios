using System;
using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealth : NetworkBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    [Networked] public int CurrentHealth { get; private set; }
    [Networked] public NetworkBool IsEliminated { get; private set; }

    [Networked] public DamageSourceType LastDamageSource { get; private set; }
    [Networked] public int LastDamageAmount { get; private set; }

    [Networked]
    [OnChangedRender(nameof(OnDamageEventChanged))]
    public int DamageEventId { get; private set; }

    public int MaxHealth => maxHealth;

    public event Action<PlayerHealth, int, int> HealthChanged;

    // health, damageAmount, currentHealth, source
    public event Action<PlayerHealth, int, int, DamageSourceType> DamageTaken;

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
        TakeDamage(amount, DamageSourceType.Generic);
    }

    public void TakeDamage(int amount, DamageSourceType source)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsEliminated)
            return;

        int damage = Mathf.Max(0, amount);

        if (damage == 0)
            return;

        int damageAmount = Mathf.Min(damage, CurrentHealth);
        int nextHealth = Mathf.Max(0, CurrentHealth - damage);

        LastDamageSource = source;
        LastDamageAmount = damageAmount;
        DamageEventId++;

        if (nextHealth == 0 && TryUseRunnerLife())
            return;

        CurrentHealth = nextHealth;

        if (CurrentHealth == 0)
        {
            IsEliminated = true;
            NotifyRoundManagerIfRunnerEliminated();
        }
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
        Eliminate(DamageSourceType.Generic);
    }

    public void Eliminate(DamageSourceType source)
    {
        if (!Object.HasStateAuthority)
            return;

        if (IsEliminated)
            return;

        LastDamageSource = source;
        LastDamageAmount = CurrentHealth;
        DamageEventId++;

        if (TryUseRunnerLife())
            return;

        CurrentHealth = 0;
        IsEliminated = true;
        NotifyRoundManagerIfRunnerEliminated();
    }

    private void NotifyRoundManagerIfRunnerEliminated()
    {
        if (!Object.HasStateAuthority)
            return;

        RoleHandler localRoleHandler = GetComponentInChildren<RoleHandler>(true);

        if (localRoleHandler == null)
            return;

        if (!localRoleHandler.TryGetRole(out RoleHandler.PlayerRole role))
            return;

        if (role != RoleHandler.PlayerRole.Runner)
            return;

        RoundManager roundManager = FindFirstObjectByType<RoundManager>();

        if (roundManager != null)
            roundManager.TryEndRoundIfAllRunnersEliminated();
    }

    private void OnDamageEventChanged()
    {
        if (LastDamageAmount <= 0)
            return;

        DamageTaken?.Invoke(this, LastDamageAmount, CurrentHealth, LastDamageSource);
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

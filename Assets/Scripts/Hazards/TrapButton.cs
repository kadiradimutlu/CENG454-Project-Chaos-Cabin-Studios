using Fusion;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class TrapButton : NetworkBehaviour
{
    [Header("Bound Trap (assign exactly one)")]
    [Tooltip("The single trap this button triggers. Fill ONLY ONE slot per button " +
             "instance — that 1 button = 1 trap rule is what keeps activation local " +
             "instead of global.")]
    [SerializeField] private DesertSlowField slowField;
    [SerializeField] private DesertMinefield minefield;
    [SerializeField] private DungeonBlindField blindField;


    [Header("Interaction")]
    [Tooltip("How close (meters) the trapper must be to interact with this button. Used " +
             "by TrapInteractor to pick the nearest button in range.")]
    [SerializeField] private float interactRange = 3f;

    [Tooltip("Optional label shown by UI prompts, e.g. 'Slow Field'. Purely cosmetic.")]
    [SerializeField] private string displayName = "Trap";

    public float InteractRange => interactRange;
    public string DisplayName => displayName;

    private Animator _animator;

    [Networked]
    [OnChangedRender(nameof(OnPressedCountChanged))]
    private int PressedCount { get; set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public override void Spawned()
    {
        Debug.Log($"[TrapButton] {name}: SPAWNED çağrıldı. Object null mu={Object==null}, HasStateAuthority={(Object!=null && Object.HasStateAuthority)}, HasInputAuthority={(Object!=null && Object.HasInputAuthority)}", this);
    }

    /// <summary>True if the given world position is within this button's interact range.</summary>
    public bool IsInRange(Vector3 fromPosition)
    {
        return (transform.position - fromPosition).sqrMagnitude <= interactRange * interactRange;
    }


    public void PressButton()
    {
        Debug.Log($"[TrapButton] {name}: PressButton(). slowField atandı mı={slowField!=null}, minefield atandı mı={minefield!=null}, blindField atandı mı={blindField!=null}, Object null mu={Object==null}", this);

        if (Object != null)
        {
            if (Object.HasStateAuthority)
            {
                Debug.Log($"[TrapButton] {name}: StateAuthority bende, direkt tetikliyorum.", this);
                PressedCount++;
                TriggerBoundTrap();
            }
            else
            {
                Debug.Log($"[TrapButton] {name}: StateAuthority bende değil, RPC gönderiyorum.", this);
                RPC_PressButton();
            }
        }
        else
        {
            Debug.LogWarning($"[TrapButton] {name}: Object NULL -> NetworkObject yok/spawn edilmedi. Offline fallback.", this);
            PlayPressAnimation();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PressButton(RpcInfo info = default)
    {
        PressedCount++;
        TriggerBoundTrap();
    }


    private void TriggerBoundTrap()
    {
        Debug.Log($"[TrapButton] {name}: TriggerBoundTrap. slowField={slowField!=null}, minefield={minefield!=null}, blindField={blindField!=null}", this);

        if (slowField != null)
            slowField.Activate();
        if (minefield != null)
            minefield.Activate();
        if (blindField != null)
            blindField.Activate();

        if (slowField == null && minefield == null && blindField == null)
            Debug.LogWarning($"[TrapButton] {name}: HİÇBİR slot dolu değil! Inspector'da Slow Field, Minefield veya Blind Field'a trap sürükle.", this);
    }

    private void OnPressedCountChanged()
    {
        PlayPressAnimation();
    }

    private void PlayPressAnimation()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Press");
        }
    }
}
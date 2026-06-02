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
    [SerializeField] private SnowFallDown snowFallDown;
    [SerializeField] private IceSurfaceTrap iceSurfaceTrap;

    [Tooltip("Island - Yolun A tarafındaki zehir bölgesini tetikler.")]
    [SerializeField] private IslandPoisonZone islandPoisonZoneA;

    [Tooltip("Island - Yolun B tarafındaki zehir bölgesini tetikler.")]
    [SerializeField] private IslandPoisonZone islandPoisonZoneB;


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
        Debug.Log($"[TrapButton] {name}: SPAWNED. Id={Object.Id}, HasState={Object.HasStateAuthority}, HasInput={Object.HasInputAuthority}", this);
    }

    /// <summary>True if the given world position is within this button's interact range.</summary>
    public bool IsInRange(Vector3 fromPosition)
    {
        return (transform.position - fromPosition).sqrMagnitude <= interactRange * interactRange;
    }


    public void PressButton()
    {
        
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
        Debug.Log($"[TrapButton] {name}: TriggerBoundTrap. slowField={slowField!=null}, minefield={minefield!=null}, blindField={blindField!=null}, snowFallDown={snowFallDown!=null}, iceSurfaceTrap={iceSurfaceTrap!=null}, islandPoisonZoneA={islandPoisonZoneA!=null}, islandPoisonZoneB={islandPoisonZoneB!=null}", this);

      
        if (slowField != null)
            slowField.Activate();
        if (blindField != null)
            blindField.Activate();
        if (minefield != null)
        {
            Debug.Log($"[TrapButton] {name}: TriggerBoundTrap -> minefield.Activate() local.", this);
            minefield.Activate();
        }
        if (snowFallDown != null)
            snowFallDown.Activate();
        if (iceSurfaceTrap != null)
            iceSurfaceTrap.ActivateIce();
        if (islandPoisonZoneA != null)
            islandPoisonZoneA.ActivateZoneA();
        if (islandPoisonZoneB != null)
            islandPoisonZoneB.ActivateZoneB();

        if (slowField == null && minefield == null && blindField == null && snowFallDown == null
            && iceSurfaceTrap == null && islandPoisonZoneA == null && islandPoisonZoneB == null)
            Debug.LogWarning($"[TrapButton] {name}: HİÇBİR slot dolu değil! Inspector'da ilgili trap slotuna obje sürükle.", this);
    }

    private void OnPressedCountChanged()
    {
            PlayPressAnimation();

        if (minefield != null)
        {
            Debug.Log($"[TrapButton] {name}: OnPressedCountChanged -> minefield.Activate() local.", this);
            minefield.Activate();
        }

        if (iceSurfaceTrap != null)
        {
            Debug.Log($"[TrapButton] {name}: OnPressedCountChanged -> iceSurfaceTrap.ActivateIce() local.", this);
            iceSurfaceTrap.ActivateIce();
        }
    }

    private void PlayPressAnimation()
    {
        if (_animator != null)
        {
            _animator.SetTrigger("Press");
        }
    }
}

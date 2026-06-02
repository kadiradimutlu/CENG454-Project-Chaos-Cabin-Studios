using Fusion;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
 

[DisallowMultipleComponent]
public class TrapInteractor : NetworkBehaviour
{
    [Header("Interaction")]
    [Tooltip("Key the trapper presses to trigger the nearest button in range.")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
 
    [Tooltip("How often (seconds) to refresh the list of nearby buttons. Cheap; 0.25 is fine.")]
    [SerializeField] private float scanInterval = 0.25f;
 
    [Tooltip("Only needed if the role check should be enforced. If you have RoleHandler, " +
             "the interactor only works for the Trapper role.")]
    [SerializeField] private bool requireTrapperRole = true;
 
    
    public TrapButton CurrentTarget { get; private set; }
 
    private RoleHandler _role;
    private TrapButton[] _allButtons;
    private float _nextScanTime;
 
    public override void Spawned()
    {
        _role = GetComponent<RoleHandler>();
        RefreshButtonList();
    }
 
    private void Update()
    {
        // Only the local player drives interaction.
        if (Object == null || !Object.HasInputAuthority)
            return;
 
        // Only the trapper interacts with buttons.
        if (requireTrapperRole && _role != null && _role.currentRole != RoleHandler.PlayerRole.Trapper)
        {
            CurrentTarget = null;
            if (WasInteractPressedThisFrame())
                Debug.Log($"[Interactor] F basıldı ama rol Trapper değil (rol={_role.currentRole}). requireTrapperRole'u kapatıp dene.", this);
            return;
        }
 
        // Periodically refresh in case buttons were spawned after us.
        if (Time.time >= _nextScanTime)
        {
            RefreshButtonList();
            _nextScanTime = Time.time + scanInterval;
        }
 
        CurrentTarget = FindNearestButtonInRange();
 
        if (WasInteractPressedThisFrame())
        {
            Debug.Log($"[Interactor] F basıldı. Menzildeki buton={(CurrentTarget!=null ? CurrentTarget.name : "YOK")}. Sahnedeki buton sayısı={(_allButtons!=null?_allButtons.Length:0)}", this);
            if (CurrentTarget != null)
            {
                Debug.Log($"[Interactor] {CurrentTarget.name}.PressButton() çağrılıyor.", CurrentTarget);
                CurrentTarget.PressButton();
            }
        }
    }
 
    private void RefreshButtonList()
    {
#if UNITY_2023_1_OR_NEWER
        _allButtons = FindObjectsByType<TrapButton>(FindObjectsSortMode.None);
#else
        _allButtons = FindObjectsOfType<TrapButton>();
#endif
    }
 
    private TrapButton FindNearestButtonInRange()
    {
        if (_allButtons == null)
            return null;
 
        Vector3 pos = transform.position;
        TrapButton best = null;
        float bestSqr = float.MaxValue;
 
        foreach (TrapButton btn in _allButtons)
        {
            if (btn == null)
                continue;
 
            if (!btn.IsInRange(pos))
                continue;
 
            float sqr = (btn.transform.position - pos).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = btn;
            }
        }
 
        return best;
    }
 
    private bool WasInteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            // Map the common F key; extend if you change interactKey.
            if (interactKey == KeyCode.F)
                return Keyboard.current.fKey.wasPressedThisFrame;
            if (interactKey == KeyCode.E)
                return Keyboard.current.eKey.wasPressedThisFrame;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(interactKey);
#else
        return false;
#endif
    }
}

using Fusion;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TrapButton : NetworkBehaviour
{
    private Animator _animator;

    [Networked]
    [OnChangedRender(nameof(OnPressedCountChanged))]
    private int PressedCount { get; set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PressButton()
    {
        if (Object != null)
        {
            if (Object.HasStateAuthority)
            {
                PressedCount++;
            }
            else
            {
                RPC_PressButton();
            }
        }
        else
        {
            // Fallback for local/offline testing
            PlayPressAnimation();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PressButton(RpcInfo info = default)
    {
        PressedCount++;
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

    private void OnMouseDown()
    {
        PressButton();
    }
}

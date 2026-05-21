using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TrapButton : MonoBehaviour
{
    private Animator _animator;



    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PressButton()
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

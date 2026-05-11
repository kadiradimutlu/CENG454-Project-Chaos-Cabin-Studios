using UnityEngine;

public class PlayerCrouch : MonoBehaviour
{
    [SerializeField] private Transform playerModel;
    [SerializeField] private float crouchScaleY = 0.5f;

    private Vector3 originalScale;

    public bool IsCrouching { get; private set; }

    private void Awake()
    {
        if (playerModel != null)
        {
            originalScale = playerModel.localScale;
        }
    }

    public void SetCrouch(bool shouldCrouch)
    {
        if (playerModel == null)
        {
            return;
        }

        IsCrouching = shouldCrouch;

        playerModel.localScale = IsCrouching
            ? new Vector3(originalScale.x, crouchScaleY, originalScale.z)
            : originalScale;
    }
}

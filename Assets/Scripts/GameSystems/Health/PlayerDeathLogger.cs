using UnityEngine;

public class PlayerDeathLogger : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died += OnDied;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnDied;
        }
    }

    private void OnDied()
    {
        Debug.Log("Player öldü");
    }
}

#if UNITY_EDITOR
using UnityEngine;

public class PlayerDamageDebug : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private int debugDamage = 20;
    [SerializeField] private KeyCode damageKey = KeyCode.P;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(damageKey) && playerHealth != null)
        {
            playerHealth.TakeDamage(debugDamage);
        }
    }
}
#endif

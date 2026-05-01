using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Make sure this script is placed on the "FlameSpawner" and that "Flame1", "Flame2" (...) are set as children of "FlameSpawner".
/// </summary>

public class FlameControl : MonoBehaviour
{
    public VisualEffect[] flameSpawners;
    public BoxCollider damageCollider;
    private bool isPlaying = false;

    void Start()
    {
        flameSpawners = gameObject.GetComponentsInChildren<VisualEffect>();
        InvokeRepeating("TurnOnOffFire", 1, 2); //Controls the spawning of fire. 
    }

    private void TurnOnOffFire()
    {
        isPlaying = !isPlaying;

        foreach (VisualEffect effect in flameSpawners)
        {
            if (isPlaying)
            {
                effect.Play();
            }
            else
            {
                effect.Stop();
            }
        }

        if (damageCollider != null)
        {
            damageCollider.enabled = isPlaying;
        }
    }
}
using System.Collections.Generic;
using Fusion;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class PressDownBox : NetworkBehaviour
{
    [Header("Motion")]
    [SerializeField] private float dropDistance = 1.5f;

    
    [SerializeField] private float moveDuration = 0.4f;

   
    [SerializeField] private float stayDownDuration = 1f;

    [Header("Trigger Filter")]
    [SerializeField] private string requiredTag = "Player";

    [Networked] private NetworkBool HasTriggered { get; set; }
    [Networked] private int StartTick { get; set; }

    private Vector3 upPosition;
    private bool capturedUp;
    private Vector3 lastBoxPosition;

    private readonly HashSet<PlayerMovement> riders = new HashSet<PlayerMovement>();

}
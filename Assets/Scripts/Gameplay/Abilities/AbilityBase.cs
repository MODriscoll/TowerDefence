using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AbilityBase : MonoBehaviourPun
{
    [SerializeField, Min(0f)] private float m_cooldown = 3f;        // The time this ability needs to cooldown before recharging
    [SerializeField, Min(1)] private int m_charges = 1;             // The amount of charges this ability has, leave at one for one charge

    public delegate void OnAbilityFinished(AbilityBase ability, int remainingCharges);      // Event for when an ability has finished
    public OnAbilityFinished onAbilityFinished;                                             // Called when the ability has finished, ability could still be activated again
}

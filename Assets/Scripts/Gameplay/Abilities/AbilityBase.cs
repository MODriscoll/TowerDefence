using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityBase : MonoBehaviour
{
    // A unique identifier for this ability. Be sure to make this unique for each ability,
    // as it will be used for being able to properly select the ability to activate
    [SerializeField] private int m_abilityId = -1;

    [SerializeField, Min(0f)] private float m_cooldown = 3f;        // The time this ability needs to cooldown before recharging
    [SerializeField, Min(1)] private int m_charges = 1;             // The amount of charges this ability has, leave at one for one charge

    public delegate void OnAbilityFinished(AbilityBase ability, int remainingCharges);      // Event for when an ability has finished
    public OnAbilityFinished onAbilityFinished;                                             // Called when the ability has finished, ability could still be activated again

    public bool hasValidId { get { return m_abilityId >= 0; } }

    public int abilityId { get { return m_abilityId; } }

    private bool m_cooldownActive = false;          // If cooldown is currently active
    private int m_remainingCharges = 0;             // Amount of charges remaining

    void Start()
    {
        m_cooldownActive = false;
        m_remainingCharges = m_charges;
    }

    /// <summary>
    /// This function is used to check if ability is available now or needs to recharge.
    /// This function should not be used to modify any properties of the ability and should treat itself as const
    /// </summary>
    /// <returns>If ability can be used now</returns>
    public virtual bool canUseAbilityNow()
    {
        return !m_cooldownActive && m_remainingCharges > 0;
    }

    /// <summary>
    /// This function is used to determine if an ability can be activated at a specific location for the desired board.
    /// This function should not be used to modify any properties of the ability and should treat itself as const
    /// </summary>
    /// <param name="controller">Player trying to activate this ability</param>
    /// <param name="board">The board which they are trying to act upon</param>
    /// <param name="worldPos">World positiion of click</param>
    /// <param name="tileIndex">Index of selected tile</param>
    /// <returns>If ability can be used</returns>
    public virtual bool canUseAbilityHere(PlayerController controller, BoardManager board, Vector2 worldPos, Vector3Int tileIndex)
    {
        return true;
    }

    public virtual void activateAbility()
    {

    }
}

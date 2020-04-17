using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AbilityBase : MonoBehaviour
{
    // A unique identifier for this ability. Be sure to make this unique for each ability,
    // as it will be used for being able to properly select the ability to activate
    [SerializeField] private int m_abilityId = -1;

    [SerializeField, Min(0f)] private float m_cooldown = 3f;        // The time this ability needs to cooldown before recharging

    // The abilities action. This will get instanced when activating the ability
    [PhotonPrefab]
    [SerializeField] private string m_actionPrefab;

    private Coroutine m_cooldownRoutine = null;         // Routine for cooldown. This not being null indicates the ability is in cooldown

    public bool hasValidId { get { return m_abilityId >= 0; } }

    public int abilityId { get { return m_abilityId; } }

    public bool inCooldown { get { return m_cooldownRoutine != null; } }

    /// <summary>
    /// This function is used to check if ability is available now or needs to recharge.
    /// This function should not be used to modify any properties of the ability and should treat itself as const
    /// </summary>
    /// <returns>If ability can be used now</returns>
    public virtual bool canUseAbilityNow()
    {
        return !inCooldown;
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
    public virtual bool canUseAbilityHere(PlayerController controller, BoardManager board, Vector3 worldPos, Vector3Int tileIndex)
    {
        return true;
    }

    /// <summary>
    /// Activates this ability, will immediately enter cooldown
    /// </summary>
    public void activateAbility(PlayerController controller, BoardManager board, Vector3 worldPos, Vector3Int tileIndex)
    {
        if (inCooldown)
            return;

        if (string.IsNullOrEmpty(m_actionPrefab))
        {
            Debug.LogWarning(string.Format("Unable to activate ability {0} as no action prefab was set", gameObject.name));
            return;
        }

        GameObject newAction = PhotonNetwork.Instantiate(m_actionPrefab, worldPos, Quaternion.identity);
        if (!newAction)
            return;

        AbilityActionBase action = newAction.GetComponent<AbilityActionBase>();
        if (!action)
            return;

        action.initAbilityAction(this, controller, board, worldPos, tileIndex);
        action.startAbilityAction();

        cancelCooldown();
        m_cooldownRoutine = StartCoroutine(cooldownRoutine());
        Debug.Log("AbilityBase, Activating ability!");
    }

    /// <summary>
    /// Cancels the cooldown of this ability, allowing it to be activated immediately
    /// </summary>
    public void cancelCooldown()
    {
        if (m_cooldownRoutine != null)
        {
            StopCoroutine(m_cooldownRoutine);
            m_cooldownRoutine = null;
        }
    }

    private IEnumerator cooldownRoutine()
    {
        yield return new WaitForSeconds(m_cooldown);
        m_cooldownRoutine = null;

        Debug.Log("AbilityBase, Ability Ready!");
    }
}

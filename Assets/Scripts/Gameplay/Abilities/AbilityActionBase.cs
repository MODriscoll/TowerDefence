using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class AbilityActionBase : MonoBehaviourPun
{
    // These are only available on the client that activated the ability
    // There shouldn't be any need (at this moment) for other clients to know these

    protected AbilityBase m_ability = null;                     // Ability that spawned this action
    protected PlayerController m_instigator = null;             // Instigator of the ability
    protected BoardManager m_board = null;                      // The board this action is to interact with
    protected Vector3 m_position = Vector3.zero;                // Position in world space the action was used
    protected Vector3Int m_tileIndex = Vector3Int.zero;         // Index of tile the action was used on

    public void initAbilityAction(AbilityBase abilityInstance, PlayerController controller, BoardManager board, Vector3 position, Vector3Int tileIndex)
    {
        m_ability = abilityInstance;
        m_instigator = controller;
        m_board = board;
        m_position = position;
        m_tileIndex = tileIndex;
    }

    public void startAbilityAction()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            Debug.LogError("startAbilityAction called on non-owning client!");
            return;
        }

        startAbilityActionImpl();
    }

    /// <summary>
    /// Event called to start this abilities action. This is only called on the owning client
    /// </summary>
    protected virtual void startAbilityActionImpl()
    {
        
    }

    /// <summary>
    /// Should be called when an ability is finished. Will handle destroy the action
    /// </summary>
    /// <param name="action">Action that has finished</param>
    public static void finishAbilityAction(AbilityActionBase action)
    {
        if (action)
            action.finishAbilityAction();
    }

    /// <summary>
    /// Call this to signal the ability has finished
    /// </summary>
    protected void finishAbilityAction()
    {
        PhotonNetwork.Destroy(gameObject);
    }
}

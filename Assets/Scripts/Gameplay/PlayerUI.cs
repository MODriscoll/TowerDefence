using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Handles the player UI, is spawned at runtime, only for the local player
public class PlayerUI : MonoBehaviour
{
    // Owner of the UI, is set upon creation
    [System.NonSerialized] public PlayerController m_owner = null;

    public Text m_debugText;

    void Update()
    {
        if (!m_owner)
            return;

        if (m_debugText)
        {
            TowerBase selectedTower = m_owner.towersList.getSelectedTower();

            m_debugText.text = string.Format(
                "Player ID: {0}\nSelected Tower: {1}\nYour Health: {2}\nOpponents Health: {3}\nGold: {4}", 
                m_owner.playerId,
                selectedTower ? selectedTower.name : "None",
                m_owner.Health,
                PlayerController.remotePlayer ? PlayerController.remotePlayer.Health : -1,
                m_owner.Gold);
        }
    }

    public void init(PlayerController owner)
    {
        // Do initialize stuff here
        m_owner = owner;
    }
}

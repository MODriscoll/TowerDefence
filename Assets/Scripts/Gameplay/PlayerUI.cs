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
        if (m_debugText)
        {
            TowerBase selectedTower = m_owner.towersList.getSelectedTower();

            m_debugText.text = string.Format(
                "Player ID: {0}\nSelected Tower: {1}", 
                m_owner.playerId,
                selectedTower ? selectedTower.name : "None");
        }
    }

    public void init(PlayerController owner)
    {
        // Do initialize stuff here
        m_owner = owner;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Handles the player UI, is spawned at runtime, only for the local player
public class PlayerUI : MonoBehaviour
{
    // Owner of the UI, is set upon creation
    [System.NonSerialized] public PlayerController m_owner = null;

    public Text m_roundText;
    public Text m_debugText;

    void Start()
    {
        if (m_roundText)
            m_roundText.gameObject.SetActive(false);
    }

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

    /// <summary>
    /// Called when created by the player controller
    /// </summary>
    /// <param name="owner">Owner of the UI</param>
    public void init(PlayerController owner)
    {
        // Do initialize stuff here
        m_owner = owner;
    }

    /// <summary>
    /// Notify that the next wave has started
    /// </summary>
    /// <param name="waveNum">The id of the wave (can be zero)</param>
    public void notifyWaveStart(int waveNum)
    {
        if (m_roundText)
        {
            m_roundText.text = string.Format("Wave {0} Starting!", waveNum + 1);
            m_roundText.gameObject.SetActive(true);

            Invoke("hideRoundText", 5f);
        }
    }

    /// <summary>
    /// Notify that the current wave has finished
    /// </summary>
    /// <param name="waveNum">The id of the finished wave num (can be zero)</param>
    public void notifyWaveFinished(int waveNum)
    {
        if (m_roundText)
        {
            m_roundText.text = string.Format("Wave {0} Completed!", waveNum + 1);
            m_roundText.gameObject.SetActive(true);

            Invoke("hideRoundText", 3f);
        }
    }

    private void hideRoundText()
    {
        if (m_roundText)
            m_roundText.gameObject.SetActive(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Handles the player UI, is spawned at runtime, only for the local player
public class PlayerUI : MonoBehaviour
{
    // Owner of the UI, is set upon creation
    [System.NonSerialized] public PlayerController m_owner = null;
    private PlayerTowersList m_playerTowersList = null;

    public Text m_roundText;
    public Text m_debugText;

    // UI text elements
    public Text m_moneyText;
    public Text m_playerHealthText;
    public Text m_enemyHealthText;
    public Button m_swapView;

    public GameObject m_turretShop;
    public GameObject m_miceShop;

    void Start()
    {
        if (m_roundText)
            m_roundText.gameObject.SetActive(false);
        m_playerTowersList = m_owner.GetComponent<PlayerTowersList>();
    }

    void Update()
    {
        if (!m_owner)
            return;

        if (m_debugText)
        {
            TowerBase selectedTower = m_owner.towersList.getSelectedTower();

            if (m_owner.Gold >= 1000)
                m_moneyText.text = "$" + m_owner.Gold.ToString("0,000");
            else
                m_moneyText.text = "$" + m_owner.Gold.ToString();
            m_playerHealthText.text = "P1 Health: " + m_owner.Health.ToString();
            m_enemyHealthText.text = "P2 Health: " + (PlayerController.remotePlayer ? PlayerController.remotePlayer.Health : -1).ToString();

            /* Old Debug UI Text
            m_debugText.text = string.Format(
                "Player ID: {0}\nSelected Tower: {1}\nYour Health: {2}\nOpponents Health: {3}\nGold: {4}", 
                m_owner.playerId,
                selectedTower ? selectedTower.name : "None",
                m_owner.Health,
                PlayerController.remotePlayer ? PlayerController.remotePlayer.Health : -1,
                m_owner.Gold);
                */
        }
    }

    /// <summary>
    /// Called when created by the player controller
    /// </summary>
    /// <param name="owner">Owner of the UI</param>
    public void init(PlayerController owner)
    {
        m_owner = owner;
    }

    public void notifyMatchStarted()
    {

    }

    public void notifyMatchFinished(bool bOwnerWon, TDWinCondition winCondition)
    {
        if (m_roundText)
        {
            if (winCondition == TDWinCondition.Tie)
                m_roundText.text = "Draw";
            else if (bOwnerWon)
                m_roundText.text = "You Won!";
            else
                m_roundText.text = "You Lost...";

            m_roundText.gameObject.SetActive(true);
        }
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

    public void notifyScreenViewSwitch(bool bViewingLocal)
    {

    }

    private void hideRoundText()
    {
        if (m_roundText)
            m_roundText.gameObject.SetActive(false);
    }

    public void changeView()
    {
        m_owner.switchView();
        m_turretShop.SetActive(!m_turretShop.activeSelf);
        m_miceShop.SetActive(!m_miceShop.activeSelf);
    }

    public void setCurrentTurret(int i)
    {
        m_playerTowersList.selectTower(i);
    }

    public void spawnMouse(int i)
    {
        m_owner.spawnSpecialMonster(m_owner.m_monsterList.getMonster(i));
    }

    public void ToggleBulldose()
    {
        m_owner.m_canBulldose = !m_owner.m_canBulldose;
    }
}

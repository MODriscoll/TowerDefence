﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

// Handles the player UI, is spawned at runtime, only for the local player
public class PlayerUI : MonoBehaviour
{
    // Owner of the UI, is set upon creation
    [System.NonSerialized] public PlayerController m_owner = null;
    private PlayerTowersList m_playerTowersList = null;

    public TextMeshProUGUI m_roundText;
    public Text m_debugText;

    // UI text elements
    public TextMeshProUGUI m_moneyText;
    public TextMeshProUGUI m_playerHealthText;
    public TextMeshProUGUI m_enemyHealthText;
    public Button m_swapView;
    public TextMeshProUGUI m_searchingText;


    public GameObject m_turretShop;
    public GameObject m_bulldoseButton;
    public GameObject m_miceShop;

    private GameOver gameOverManager;
    // For Cheese pulsating effect
    //public int healthCheck;
    //public CheesePulse playerCheese;

    void Start()
    {
        if (m_roundText)
            m_roundText.gameObject.SetActive(false);
        m_playerTowersList = m_owner.GetComponent<PlayerTowersList>();
        gameOverManager = FindObjectOfType<GameOver>();
        //healthCheck = m_owner.Health;         //to check whether the health of the player has changed
    }

    void Update()
    {
        if (!m_owner)
            return;

        if (m_moneyText)
            m_moneyText.text = string.Format(" Candy: {0}", m_owner.Gold);

        if (PhotonNetwork.IsConnected)
        {
            if (m_playerHealthText)
                m_playerHealthText.text = string.Format("{0} : {1}", PhotonNetwork.NickName, m_owner.Health);

            //Makes cheese pulse when the player receives damage
            //if (m_owner.Health<=healthCheck)
            //{
            //    playerCheese.StartCoroutine("Pulse");
            //}

            if (m_enemyHealthText)
            {
                if (PlayerController.remotePlayer != null)
                {
                    PhotonView remoteView = PlayerController.remotePlayer.photonView;
                    if (remoteView.Owner != null)
                        m_enemyHealthText.text = string.Format("{0} : {1}", remoteView.Owner.NickName, PlayerController.remotePlayer.Health);
                    else
                        m_enemyHealthText.text = string.Format("Enemy : {0}", m_owner.Health);
                }
                else
                {
                    m_enemyHealthText.text = string.Format("Enemy : -1");
                }
            }
        }
        else
        {
            if (m_playerHealthText)
                m_playerHealthText.text = string.Format("P1 : {0}", m_owner.Health);

            // Most likely playing in editor (only one player is supported)
            if (m_enemyHealthText)
                // Cheating, We know the text is a child of a game object
                m_enemyHealthText.transform.parent.gameObject.SetActive(false);
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
        // Opponent is present, no longer need to notify user we are searching
        if (m_searchingText)
            m_searchingText.gameObject.SetActive(false);
    }

    public void notifyMatchFinished(bool bOwnerWon, TDWinCondition winCondition)
    {
        if (m_roundText)
        {
            if (!gameOverManager)
                gameOverManager = FindObjectOfType<GameOver>();
            if (winCondition == TDWinCondition.Tie)
            {
                m_roundText.text = "Draw";
                gameOverManager.SetGameOverScreen("Draw");
            }
            else if (bOwnerWon)
            {
                m_roundText.text = "You Won!";
                gameOverManager.SetGameOverScreen("Win");
            }
            else
            {
                m_roundText.text = "You Lost...";
                gameOverManager.SetGameOverScreen("Lose");
            }
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
        m_bulldoseButton.SetActive(m_turretShop.activeSelf);
        m_miceShop.SetActive(!m_miceShop.activeSelf);
    }

    public void setCurrentTurret(int i)
    {
        string dummy;
        m_playerTowersList.selectTower(i, out dummy);
    }

    public void spawnMouse(int i)
    {
        string prefabName;
        m_owner.spawnSpecialMonster(m_owner.monsterList.getMonster(i, out prefabName), prefabName);
    }

    public void useAbility(int i)
    {
        m_playerTowersList.selectAbility(i);
    }

    public void ToggleBulldose()
    {
        m_owner.toggleBulldozeTowers();
    }

    public void SetUnitButtonText()
    {
        Object turret1 = Resources.Load("Prefabs/Towers/BasicTurret.prefab");
        //turret1.
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager manager;

    public PlayerInfo m_p1Info;
    public PlayerInfo m_p2Info;

    [SerializeField] private PlayerController m_playerPrefab;
    private PlayerController m_p1Controller;
    private PlayerController m_p2Controller;

    void Awake()
    {
        if (manager)
            Debug.LogError("GameManager has already been set!");

        manager = this;

        // PlayerController will set this reference upon start
        if (!PlayerController.localPlayer)
        {
            int playerId = PhotonNetwork.IsMasterClient ? 0 : 1;

            // If player is player 1 or two
            object[] instantData = new object[1];
            instantData[0] = playerId;
            GameObject newController = PhotonNetwork.Instantiate(m_playerPrefab.gameObject.name, Vector3.zero, Quaternion.identity, 0, instantData);
            getPlayerInfo(playerId).m_controller = newController.GetComponent<PlayerController>();
        }
    }

    void OnDestroy()
    {
        if (manager)
        {
            if (manager == this)
                manager = null;
            else
                Debug.LogWarning("Manager destroyed but manager was not set as static instance");
        }
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // We require at least two players
            LeaveGame();
        }
    }

    public override void OnLeftRoom()
    {
        // Exit back to the main menu
        SceneManager.LoadScene(0);
    }

    public PlayerInfo getPlayerInfo(int id)
    {
        if (id == 0)
            return m_p1Info;
        else if (id == 1)
            return m_p2Info;
        else
            return null;
    }

    /// <summary>
    /// Leaves the current game
    /// </summary>
    public void LeaveGame()
    {
        PhotonNetwork.LeaveRoom();
    }
}

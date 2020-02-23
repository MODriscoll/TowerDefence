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

    public BoardManager m_p1Board;      // Board for player 1
    public BoardManager m_p2Board;      // Board for player 2

    [SerializeField] private PlayerController m_playerPrefab;   // Player controller to spawn for a new player
    private PlayerController m_p1Controller;
    private PlayerController m_p2Controller;

    public BoardManager PlayersBoard { get { return getPlayersBoard(); } }
    public BoardManager OpponentsBoard { get { return getOpponentsBoard(); } }

    void Awake()
    {
        if (manager)
            Debug.LogError("GameManager has already been set!");

        manager = this;

        // PlayerController will set this reference upon start
        if (!PlayerController.localPlayer)
            PhotonNetwork.Instantiate(m_playerPrefab.gameObject.name, transform.position, Quaternion.identity);

#if UNITY_EDITOR
        // Spawn second player
        //PhotonNetwork.Instantiate(m_playerPrefab.gameObject.name, transform.position, Quaternion.identity);
#endif
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

    void Update()
    {
        BoardManager board = getPlayersBoard();
        if (board && board.monsterManager)
            board.monsterManager.tick(Time.deltaTime);

#if UNITY_EDITOR
        board = getBoardManager(1);
        if (board && board.monsterManager)
            board.monsterManager.tick(Time.deltaTime);
#endif
    }

    private BoardManager getPlayersBoard()
    {
#if UNITY_EDITOR
        //return m_p1Board;
#endif

        if (PlayerController.localPlayer)
            return getBoardManager(PlayerController.localPlayer.playerId);

        return null;
    }

    private BoardManager getOpponentsBoard()
    {
#if UNITY_EDITOR
        //return m_p2Board;
#endif

        if (PlayerController.localPlayer)
        {
            if (PlayerController.localPlayer.playerId == 0)
                return m_p2Board;
            else
                return m_p1Board;
        }
 
        return null;
    }

    public BoardManager getBoardManager(int playerId)
    {
        if (playerId == 0)
            return m_p1Board;
        else if (playerId == 1)
            return m_p2Board;

        return null;
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

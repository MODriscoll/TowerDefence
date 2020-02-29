using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

// These must be in order
public enum TDMatchState
{
    PreMatch,
    InProgress,
    PostMatch
}

public enum TDWinCondition
{
    OutOfHealth,        // Loser ran out of health
    OutOfTime,          // Time ran out, but winner has more health

    Tie                 // Scores are tied
}

public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameManager manager;      // Singleton access to the game manager

    public BoardManager m_p1Board;      // Board for player 1
    public BoardManager m_p2Board;      // Board for player 2

    [SerializeField] private PlayerController m_playerPrefab;   // Player controller to spawn for a new player

    public BoardManager PlayersBoard { get { return getPlayersBoard(); } }          // Get the local players board
    public BoardManager OpponentsBoard { get { return getOpponentsBoard(); } }      // Get the opponent players board

    public delegate void OnMatchStateChanged(TDMatchState matchState);      // Delegate for when the state of a match has changed
    public OnMatchStateChanged onMatchStateChanged;                         // Event fired whenever the state of the match has changed

    private TDMatchState m_matchState = TDMatchState.PreMatch;      // Current state of the match

    public TDMatchState matchState { get { return m_matchState; } private set { setMatchState(value); } }       // Get the state of the match
    public bool hasMatchStarted { get { return m_matchState > TDMatchState.PreMatch; } }                        // If the match has started

    // TODO: Might be better to move this into a WaveHandler script
    [Header("Waves")]
    [SerializeField] private WaveSpawner m_waveSpawner;     // Wave spawner that handles spawning monster
    [SerializeField, Min(1)] float m_waveDelay = 5f;        // The delay (in seconds) between waves

    private int m_waveNum = -1;         // Current wave number, is synced by startNextWave

    public int waveNumber { get { return m_waveNum; } }     // The wave that is currently active

    void Awake()
    {
        if (manager)
        {
            Debug.LogError("GameManager has already been set!");
            return;
        }

        manager = this;

        // PlayerController will set this reference upon start
        if (!PlayerController.localPlayer)
            PhotonNetwork.Instantiate(m_playerPrefab.gameObject.name, transform.position, Quaternion.identity);

        // Wait for all players to connect before starting,
        // this component will destroy itself once the match has started
        gameObject.AddComponent<GameStartHandler>();
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
        if (board && board.MonsterManager)
            board.MonsterManager.tick(Time.deltaTime);

#if UNITY_EDITOR
        board = getBoardManager(1);
        if (board && board.MonsterManager)
            board.MonsterManager.tick(Time.deltaTime);
#endif
    }

    /// <summary>
    /// Updates the match state, handling what needs for each case
    /// </summary>
    /// <param name="newState">New state to set ourselves in</param>
    private void setMatchState(TDMatchState newState)
    {
        if (newState != m_matchState)
        {
            m_matchState = newState;

            if (onMatchStateChanged != null)
                onMatchStateChanged.Invoke(newState);
        }
    }

    private BoardManager getPlayersBoard()
    {
        if (PlayerController.localPlayer)
            return getBoardManager(PlayerController.localPlayer.playerId);

        return null;
    }

    private BoardManager getOpponentsBoard()
    {
        if (PlayerController.localPlayer)
        {
            if (PlayerController.localPlayer.playerId == 0)
                return m_p2Board;
            else
                return m_p1Board;
        }
 
        return null;
    }

    /// <summary>
    /// Get a board manager based on player ID
    /// </summary>
    /// <param name="playerId">Players whose board to get</param>
    /// <returns>Valid board or null</returns>
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

    /// <summary>
    /// Leaves the current game
    /// </summary>
    public void LeaveGame()
    {
#if UNITY_EDITOR
        Application.Quit();
#else
        PhotonNetwork.LeaveRoom();
#endif
    }

    // ------------

    /// <summary>
    /// Starts the match if allowed to. Nothing happens if match is already in progress
    /// </summary>
    public void startMatch()
    {
        if (!canStartMatch())
            return;

        matchState = TDMatchState.InProgress;

        // Hook events
        {
            m_waveSpawner.onWaveFinished += onWaveFinished;
            MonsterManager.onMonsterDestroyed += onMonsterDestroyed;
        }

        // TODO: Tidy up

#if !UNITY_EDITOR
        onMatchStartRPC();
#else
        photonView.RPC("onMatchStartRPC", RpcTarget.All);
#endif

        Invoke("startNextWave", Mathf.Max(m_waveDelay, 1f));
    }

    [PunRPC]
    private void onMatchStartRPC()
    {
        PlayerController.localPlayer.notifyMatchStarted();
    }

    [PunRPC]
    private void onMatchFinishedRPC(byte condAsByte, int winnerId)
    {
        TDWinCondition winCondition = (TDWinCondition)condAsByte;

        PlayerController.localPlayer.notifyMatchFinished(winCondition, winnerId);

        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            m_waveSpawner.onWaveFinished -= onWaveFinished;
            MonsterManager.onMonsterDestroyed -= onMonsterDestroyed;
        }
    }

    public void finishMatch(TDWinCondition winCondition, int winnerId)
    {
        photonView.RPC("onMatchFinishedRPC", RpcTarget.All, (byte)winCondition, winnerId);
    }

    // ------- Wave Handling (Give better names)

    private void startNextWave()
    {
        ++m_waveNum;

#if UNITY_EDITOR
        // Manually call RPC in editor
        startNextWaveRPC(m_waveNum);
#else
        photonView.RPC("startNextWaveRPC", RpcTarget.All, m_waveNum);
#endif
    }

    [PunRPC]
    private void startNextWaveRPC(int waveId)
    {
        m_waveNum = waveId;

        if (m_waveSpawner)
            m_waveSpawner.initWave(waveId);

        PlayerController.localPlayer.notifyWaveStarted(m_waveNum);
    }

    private void onWaveFinished()
    {
        if (shouldFinishWave())
            onWaveFinishedImpl();
    }

    private void onWaveFinishedImpl()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return;

        if (m_waveSpawner.isValidWaveId(m_waveNum + 1))
        {
#if UNITY_EDITOR
            // Manually call RPC in editor
            onWaveFinishedRPC();
#else
            photonView.RPC("onWaveFinishedRPC", RpcTarget.All);
#endif

            Invoke("startNextWave", Mathf.Max(m_waveDelay, 1f));
        }
        else
        {
            int winnerId = -1;

            // Check who has more health
            if (PlayerController.localPlayer.Health > PlayerController.remotePlayer.Health)
                winnerId = PlayerController.localPlayer.playerId;
            else if (PlayerController.remotePlayer.Health > PlayerController.localPlayer.Health)
                winnerId = PlayerController.remotePlayer.playerId;

            TDWinCondition winCondition = TDWinCondition.Tie;
            if (winnerId != -1)
                winCondition = TDWinCondition.OutOfTime;

            finishMatch(winCondition, winnerId);
        }
    }

    [PunRPC]
    private void onWaveFinishedRPC()
    {
        PlayerController.localPlayer.notifyWaveFinished(m_waveNum);
    }

    private void onMonsterDestroyed()
    {
        if (shouldFinishWave())
            onWaveFinishedImpl();
    }

    private bool shouldFinishWave()
    {
        bool bWaveFinished = false;
        if (m_waveSpawner)
            bWaveFinished = !m_waveSpawner.IsWaveInProgress;

        if (bWaveFinished)
        {
            bool bNoMonstersB1 = getBoardManager(0).MonsterManager.NumMonsters == 0;
            bool bNoMonstersB2 = getBoardManager(1).MonsterManager.NumMonsters == 0;
            return bNoMonstersB1 && bNoMonstersB2;
        }

        return false;
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(m_matchState);
        }
        else
        {
            matchState = (TDMatchState)stream.ReceiveNext();   
        }
    }

    private bool canStartMatch()
    {
        // Only master client can start the match
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return false;

        // We don't support restarting match this way
        if (hasMatchStarted)
            return false;

        // Have essential scripts been provided?
        {
            if (!m_p1Board)
            {
                Debug.LogError("Missing board for player 1");
                return false;
            }

            if (!m_p2Board)
            {
                Debug.LogError("Missing board for player 2");
                return false;
            }

            if (!m_waveSpawner)
            {
                Debug.LogError("Unable to start match as wave spawner is missing");
                return false;
            }

            // No point in starting match if no waves have been set up
            if (!m_waveSpawner.isValidWaveId(0))
            {
                Debug.LogError("Wave spawner is set up incorrectly. Please ensure there is at least one round set");
                return false;
            }
        }

        return true;
    }
}

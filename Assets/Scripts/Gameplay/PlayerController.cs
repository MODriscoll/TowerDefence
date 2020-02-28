using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    static public PlayerController localPlayer;         // Singleton access to local player controller
    static public PlayerController remotePlayer;        // Singleton access to remote player controller

    private int m_id = -1;                                          // Id of player
    [SerializeField] private Camera m_camera;                       // Players viewport of the scene
    [SerializeField] private PlayerTowersList m_towersList;         // List of all the towers the player can place

    [SerializeField] private int m_health = 100;            // How much health this player has
    [SerializeField] private int m_gold = 100;              // How much spending money this player has
    [SerializeField] private int m_maxGold = 1000;          // Max amount of gold a player can have

    private BoardManager m_board = null;        // Cached board we use
    private int m_viewBoard = -1;               // The board we are currently viewing (matches player id)

    // The id of this player, is set in start
    public int playerId { get { return m_id; } }

    public int Health { get { return m_health; } }
    public int Gold { get { return m_gold; } }

    // The players tower list
    public PlayerTowersList towersList { get { return m_towersList; } }

    public BoardManager Board { get { return m_board; } }

    // temp serialize (for testing)
    [SerializeField] private MonsterBase m_commonMonster;           // The common monster that this player passively 'spawns'

    public PlayerUI m_playerUIPrefab;       // UI to spawn for local player
    private PlayerUI m_playerUI = null;     // Instance of players UI

    public MonsterBase m_testMonster;

    void Awake()
    {
        if (!m_camera)
            m_camera = GetComponentInChildren<Camera>();

        if (!m_towersList)
            m_towersList = GetComponent<PlayerTowersList>();
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {      
            if (photonView.IsMine)
            {
                if (PhotonNetwork.IsMasterClient)
                    m_id = 0;
                else
                    m_id = 1;

                localPlayer = this;
                switchViewImpl(m_id);

                // Only create UI for local player
                m_playerUI = Instantiate(m_playerUIPrefab, transform, false);
                if (m_playerUI)
                {
                    m_playerUI.m_owner = this;
                }

                // View game through this camera
                if (m_camera)
                    m_camera.tag = "MainCamera";
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                    m_id = 1;
                else
                    m_id = 0;

                remotePlayer = this;

                // Don't need this camera if not local players
                if (m_camera)
                    Destroy(m_camera);
            }

            m_board = GameManager.manager.getBoardManager(m_id);
        }
#if UNITY_EDITOR
        else
        {
            if (localPlayer)
            {
                m_id = 1;
            }
            else
            {
                m_id = 0;

                localPlayer = this;
                switchViewImpl(m_id);

                m_playerUI = Instantiate(m_playerUIPrefab, transform, false);
                if (m_playerUI)
                {
                    m_playerUI.m_owner = this;
                }
            }

            m_board = GameManager.manager.getBoardManager(m_id);
        }
#endif
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (m_towersList)
        {
            for (int i = (int)KeyCode.Alpha1; i <= (int)KeyCode.Alpha9; ++i)
                if (Input.GetKeyDown((KeyCode)i))
                    m_towersList.selectTower(i - (int)KeyCode.Alpha1);

            if (Input.GetKeyDown(KeyCode.Alpha0))
                m_towersList.unselectTower();
        }

        // Quick testing
        if (Input.GetMouseButtonDown(1))
        {
            //if (m_testMonster)
            //        GameManager.manager.OpponentsBoard.spawnMonster(m_testMonster.name, PhotonNetwork.LocalPlayer);
        }

        // Quick testing
        if (Input.GetKeyDown(KeyCode.F))
            switchView();
#endif

        Vector3 selectedPos;
        if (tryGetBoardInput(out selectedPos))
        {
            // Convert from screen space to world space
            selectedPos = m_camera.ScreenToWorldPoint(selectedPos);

            Vector3Int tileIndex = Board.positionToIndex(selectedPos);
            if (!m_board.isPlaceableTile(tileIndex))
                return;

            if (!m_board.isOccupied(tileIndex))
            {
                // We call this as selectedPos is highly likely not to be
                // the center of the tile, while this 100% will be
                Vector3 spawnPos = m_board.indexToPosition(tileIndex);

                // Spawn tower on the server (replicates back to us if not master client)
                TowerBase towerPrefab = m_towersList.getSelectedTower();
                if (towerPrefab && canAfford(towerPrefab.m_cost))
                    placeTowerAt(towerPrefab, tileIndex, spawnPos);
            }
            else
            {
                // TODO: Might want to do stuff with the tower already occupied?
                Debug.Log("Tile already occupied");
            }
        }
    }

    protected bool tryGetBoardInput(out Vector3 selectedPos)
    {
#if UNITY_EDITOR
        // If playing in editor, only accept input for first player
        if (Application.isEditor)
            if (m_id != 0)
            {
                selectedPos = Vector3.zero;
                return false;
            }
#endif

        // Consider touch input
        if (Input.touchSupported)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended)
                {
                    selectedPos = touch.position;
                    return true;
                }
            }
        }
#if UNITY_STANDALONE
        // Revert to normal PC bindings
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                selectedPos = Input.mousePosition;
                return true;
            }
        }
#endif

        selectedPos = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Places a tower that this player owns at tile
    /// </summary>
    /// <param name="towerPrefab">Prefab of tower to spawn</param>
    /// <param name="tileIndex">Index of tile to place tower on</param>
    /// <param name="spawnPos">Spawn position (in world space) for the tower</param>
    public void placeTowerAt(TowerBase towerPrefab, Vector3Int tileIndex, Vector3 spawnPos)
    {
        consumeGold(towerPrefab.m_cost);
        
        // Finally spawn the tower
        TowerBase.spawnTower(towerPrefab, m_id, tileIndex, spawnPos);
    }

    /// <summary>
    /// Applies damage to the player. Will finish the game if health has depleted
    /// </summary>
    /// <param name="damage">Damage to apply. Needs to be a positive value</param>
    public void applyDamage(int damage)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (damage <= 0)
        {
            Debug.LogWarning(string.Format("Unable to apply negative damage! (Value = {0})", damage));
            return;
        }

        m_health = Mathf.Max(m_health - damage, 0);
        if (m_health <= 0)
        {
            // We lost all our health. The opponent wins!
            GameManager.manager.finishMatch(remotePlayer.m_id);
        }
    }

    /// <summary>
    /// Gives gold to the player. Maxing it out at max gold value
    /// </summary>
    /// <param name="amount">Gold to give. Needs to be a positive value</param>
    public void giveGold(int amount)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (amount <= 0)
        {
            Debug.LogWarning(string.Format("Unable to give negative gold! (Value = {0})", amount));
            return;
        }

        m_gold = Mathf.Min(m_gold + amount, m_maxGold);
    }

    /// <summary>
    /// Consumes gold from this players stash. Clamping it to min of zero
    /// </summary>
    /// <param name="amount">Gold to consume. Needs to be a positive value</param>
    public void consumeGold(int amount)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (amount <= 0)
        {
            Debug.LogWarning(string.Format("Unable to consume negative gold! (Value = {0}", amount));
            return;
        }

        m_gold = Mathf.Max(m_gold - amount, 0);
    }

    /// <summary>
    /// Checks if player can afford the pay arbitrary amount
    /// </summary>
    /// <param name="amount">Amount to query</param>
    /// <returns>If player has required amount</returns>
    public bool canAfford(int amount)
    {
        return m_gold >= amount;
    }

    /// <summary>
    /// Switches the board we are looking at
    /// </summary>
    public void switchView()
    {
        Debug.LogError("switchView");
        switchViewImpl((m_viewBoard + 1) % 2);
    }

    /// <summary>
    /// Sets the board we are looking at
    /// </summary>
    /// <param name="boardId">Id of board to look at</param>
    private void switchViewImpl(int boardId)
    {
        if (m_viewBoard != boardId)
        {
            Debug.LogError(string.Format("different id: {0}", boardId));

            BoardManager board = GameManager.manager.getBoardManager(boardId);
            if (board)
            {
                transform.position = board.ViewPosition;
                m_viewBoard = boardId;
                Debug.LogError("Updated");
            }
        }
    }

    public void notifyWaveStarted(int waveNum)
    {
        if (m_playerUI)
            m_playerUI.notifyWaveStart(waveNum);
    }

    public void notifyWaveFinished(int waveNum)
    {
        if (m_playerUI)
            m_playerUI.notifyWaveFinished(waveNum);
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(m_health);
        }
        else
        {
            m_health = (int)stream.ReceiveNext();
        }
    }
}

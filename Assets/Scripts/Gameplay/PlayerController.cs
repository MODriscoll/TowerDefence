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
   
    public int playerId { get { return m_id; } }        // The id of this player, is set in start

    public BoardManager Board { get { return m_board; } }                   // This players board
    public PlayerTowersList towersList { get { return m_towersList; } }     // This players tower list

    [SerializeField] private int m_health = 100;            // How much health this player has
    [SerializeField] private int m_gold = 100;              // How much spending money this player has
    [SerializeField] private int m_maxGold = 1000;          // Max amount of gold a player can have

    private BoardManager m_board = null;        // Cached board we use
    private int m_viewBoard = -1;               // The board we are currently viewing (matches player id)

    public int Health { get { return m_health; } }      // Health this player has
    public int Gold { get { return m_gold; } }          // Gold this player has

    private bool m_canPlaceTowers = false;          // If we can place towers (controlled by MasterClient)
    private bool m_canSpawnMonsters = false;        // If we can spawn monsters (controlled by MasterClient)
    private bool m_monsterSpawnLocked = false;      // If we are locked from spawning monsters (delay is active)

    public PlayerUI m_playerUIPrefab;       // UI to spawn for local player
    private PlayerUI m_playerUI = null;     // Instance of players UI   

    public SpecialMonster m_testMonsterSpawn;

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
            spawnSpecialMonster(m_testMonsterSpawn);

        // Quick testing
        if (Input.GetKeyDown(KeyCode.F))
            switchView();
#endif

        Vector3 selectedPos;
        if (tryGetBoardInput(out selectedPos))
        {
            if (m_canPlaceTowers)
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
    }

    void OnDestroy()
    {
        if (localPlayer == this)
            localPlayer = null;
        if (remotePlayer == this)
            remotePlayer = null;
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
    /// Places a tower that this player owns at tile. This does not check
    /// if tile is already occupied or is even valid to place a tile on
    /// </summary>
    /// <param name="towerPrefab">Prefab of tower to spawn</param>
    /// <param name="tileIndex">Index of tile to place tower on</param>
    /// <param name="spawnPos">Spawn position (in world space) for the tower</param>
    public void placeTowerAt(TowerBase towerPrefab, Vector3Int tileIndex, Vector3 spawnPos)
    {
        consumeGold(towerPrefab.m_cost);
        
        // Finally spawn the tower (this will eventually lead to actually placing tile on the map)
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
            m_canSpawnMonsters = false;

            // We lost all our health. The opponent wins!
            // TODO: Call function for master client to handle
            GameManager.manager.finishMatch(TDWinCondition.OutOfHealth, remotePlayer.m_id);
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
    /// Spawns a special monster to deploy to the opponents board
    /// </summary>
    /// <param name="monsterPrefab">Monster to spawn</param>
    public void spawnSpecialMonster(SpecialMonster monsterPrefab)
    {
        if (!canSpawnSpecialMonster(monsterPrefab))
            return;

        BoardManager opponentsBoard = GameManager.manager.OpponentsBoard;
        if (opponentsBoard)
        {
            opponentsBoard.spawnMonster(monsterPrefab.name, PhotonNetwork.LocalPlayer);

            // Apply costs for spawning this monster
            consumeGold(monsterPrefab.Cost);

            if (monsterPrefab.Delay > 0f)
            {
                m_monsterSpawnLocked = true;
                Invoke("unlockMonsterSpawn", monsterPrefab.Delay);
            }
        }
    }

    /// <summary>
    /// Checks if we are able to spawn a special monster on the opponents board
    /// </summary>
    /// <param name="monsterPrefab">Monster to check</param>
    /// <returns>If monster can be spawned</returns>
    private bool canSpawnSpecialMonster(SpecialMonster monsterPrefab)
    {
        if (!monsterPrefab)
            return false;

        // Might be in-between rounds
        if (!m_canSpawnMonsters || m_monsterSpawnLocked)
            return false;

        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return false;

        return canAfford(monsterPrefab.Cost);
    }

    /// <summary>
    /// Callback after delay for spawning special monster is done
    /// </summary>
    private void unlockMonsterSpawn()
    {
        m_monsterSpawnLocked = false;
    }

    /// <summary>
    /// Switches the board we are looking at
    /// </summary>
    public void switchView()
    {
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
            BoardManager board = GameManager.manager.getBoardManager(boardId);
            if (board)
            {
                transform.position = board.ViewPosition;
                m_viewBoard = boardId;
            }
        }
    }

    public void notifyMatchStarted()
    {
        // Allow players to start placing towers now
        m_canPlaceTowers = true;

        if (m_playerUI)
            m_playerUI.notifyMatchStarted();
    }

    public void notifyMatchFinished(TDWinCondition winCondition, int winnerId)
    {
        m_canPlaceTowers = false;
        m_canSpawnMonsters = false;

        bool bIsWinner = winnerId == m_id;

        if (m_playerUI)
            m_playerUI.notifyMatchFinished(bIsWinner, winCondition);

        // for now
        GameManager.manager.Invoke("LeaveGame", 5f);
    }

    /// <summary>
    /// Notify this player that a wave has started. This should only be called by the GameManager
    /// </summary>
    /// <param name="waveNum">Number of wave that is starting</param>
    public void notifyWaveStarted(int waveNum)
    {
        m_canPlaceTowers = true;
        m_canSpawnMonsters = true;
        m_monsterSpawnLocked = false;

        if (m_playerUI)
            m_playerUI.notifyWaveStart(waveNum);
    }

    /// <summary>
    /// Notify this player that a wave has finished. This should only be called by the GameManager
    /// </summary>
    /// <param name="waveNum">Number of wave that is finished</param>
    public void notifyWaveFinished(int waveNum)
    {
        m_canSpawnMonsters = false;

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

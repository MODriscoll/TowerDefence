using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    static public PlayerController localPlayer;         // Singleton access to local player controller
    static public PlayerController remotePlayer;        // Singleton access to remote player controller

    private int m_id = -1;                                          // Id of player
    [SerializeField] private Camera m_camera;                       // Players viewport of the scene
    [SerializeField] private PlayerTowersList m_towersList;         // List of all the towers the player can place
    [SerializeField] public PlayerMonstersList m_monsterList;       // List of all the monsters the player can deploy
   
    public int playerId { get { return m_id; } }        // The id of this player, is set in start

    public BoardManager Board { get { return m_board; } }                       // This players board
    public PlayerTowersList towersList { get { return m_towersList; } }         // This players tower list
    public PlayerMonstersList monsterList { get { return m_monsterList; } }     // This players monster list

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
    private bool m_canBulldoze = false;             // If clicking on a tile with a tower will destroy it (only when delay isn't active)

    public PlayerUI m_playerUIPrefab;       // UI to spawn for local player
    private PlayerUI m_playerUI = null;     // Instance of players UI   

    [Header("Controls")]
    public bool m_canBuildInBulldozeMode = false;       // If we can build towers while bulldoze mode is active
#if UNITY_EDITOR
    public bool m_useTouchControls = false;             // If touch controls should be used if possible while in editor
#endif

#if UNITY_EDITOR
    [Header("Cheats (Editor Only)")]
    public bool m_infGold = false;          // If we have infinite gold
#endif

    void Awake()
    {
        if (!m_camera)
            m_camera = GetComponentInChildren<Camera>();

        if (!m_towersList)
            m_towersList = GetComponent<PlayerTowersList>();

        if (!m_monsterList)
            m_monsterList = GetComponent<PlayerMonstersList>();
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
        else
        {
            if (localPlayer)
            {
                Debug.LogError("One player already exists! Destroying this player as only one player is supported in editor");
                return;
            }

            m_id = 0;

            localPlayer = this;
            switchViewImpl(m_id);

            m_playerUI = Instantiate(m_playerUIPrefab, transform, false);
            if (m_playerUI)
                m_playerUI.m_owner = this;

            m_board = GameManager.manager.getBoardManager(m_id);

            // Also set ourselves as remote player when playing offline
            remotePlayer = this;
        }
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (m_towersList)
        {
            // Used as out parameters for select functions
            string dummy;

            // Notice Alpha1 to Alpha4
            for (int i = (int)KeyCode.Alpha1; i < (int)KeyCode.Alpha5; ++i)
                if (Input.GetKeyDown((KeyCode)i))
                    m_towersList.selectTower(i - (int)KeyCode.Alpha1, out dummy);

            if (Input.GetKeyDown(KeyCode.Alpha0))
                m_towersList.unselectTower();
        }

        if (m_monsterList)
        {
            // Notice Alpha5 to Alpha9
            for (int i = (int)KeyCode.Alpha5; i <= (int)KeyCode.Alpha9; ++i)
                if (Input.GetKeyDown((KeyCode)i))
                {
                    string prefabName;
                    spawnSpecialMonster(m_monsterList.getMonster(i - (int)KeyCode.Alpha5, out prefabName), prefabName);
                }
        }

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

                TowerBase tower = m_board.getTowerOnTile(tileIndex);
                if (tower)
                {
                    // Try destroying the tower if player has bulldozing active
                    if (m_canBulldoze)
                    {
                        // Refund half of a cost of the tower
                        giveGold(tower.m_cost / 2);

                        AnalyticsHelpers.reportTowerBulldozed(tower);
                        TowerBase.destroyTower(tower, true);
                    }
                }
                else if (m_canBuildInBulldozeMode || !m_canBulldoze)
                {
                    // We call this as selectedPos is highly likely not to be
                    // the center of the tile, while this 100% will be
                    Vector3 spawnPos = m_board.indexToPosition(tileIndex);

                    // Spawn tower on the server (replicates back to us if not master client)
                    string prefabName;
                    TowerBase towerPrefab = m_towersList.getSelectedTower(out prefabName);
                    if (towerPrefab && canAfford(towerPrefab.m_cost))
                        placeTowerAt(towerPrefab, prefabName, tileIndex, spawnPos);
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

        bool useTouchControls = Input.touchSupported;
#if UNITY_EDITOR
        // Possible for touch input to be tracked in editor.
        // There may be cases where we don't want this
        useTouchControls &= m_useTouchControls;
#endif

        // Prioritize touch controls
        if (useTouchControls)
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
        // Revert to normal PC bindings
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                selectedPos = Input.mousePosition;
                return true;
            }
        }

        selectedPos = Vector3.zero;
        return false;
    }

    /// <summary>
    /// Places a tower that this player owns at tile. This does not check
    /// if tile is already occupied or is even valid to place a tile on
    /// </summary>
    /// <param name="towerPrefab">Prefab of tower to spawn</param>
    /// <param name="prefabName">Path to tower prefab in resources folder</param>
    /// <param name="tileIndex">Index of tile to place tower on</param>
    /// <param name="spawnPos">Spawn position (in world space) for the tower</param>
    public void placeTowerAt(TowerBase towerPrefab, string prefabName, Vector3Int tileIndex, Vector3 spawnPos)
    {
        consumeGold(towerPrefab.m_cost);
        
        // Finally spawn the tower (this will eventually lead to actually placing tile on the map)
        TowerBase.spawnTower(prefabName, m_id, tileIndex, spawnPos);
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
            int winnerId = PhotonNetwork.IsConnected ? remotePlayer.m_id : -1;
            GameManager.manager.finishMatch(TDWinCondition.OutOfHealth, winnerId);
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
#if UNITY_EDITOR
        if (m_infGold)
            return;
#endif

        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (amount <= 0)
        {
            Debug.LogWarning(string.Format("Unable to consume negative gold! (Value = {0})", amount));
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
#if UNITY_EDITOR
        if (m_infGold)
            return true;
#endif

        return m_gold >= amount;
    }

    /// <summary>
    /// Spawns a special monster to deploy to the opponents board
    /// </summary>
    /// <param name="monsterPrefab">Monster to spawn</param>
    /// <param name="prefabName">Actual path to monster prefab from resources folder</param>
    public void spawnSpecialMonster(SpecialMonster monsterPrefab, string prefabName)
    {
        if (!canSpawnSpecialMonster(monsterPrefab, prefabName))
            return;

        BoardManager opponentsBoard = GameManager.manager.OpponentsBoard;
        if (opponentsBoard)
        {
            opponentsBoard.spawnMonster(prefabName, PhotonNetwork.LocalPlayer);

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
    /// <param name="prefabName">Path from resources folder to prefab</param>
    /// <returns>If monster can be spawned</returns>
    private bool canSpawnSpecialMonster(SpecialMonster monsterPrefab, string prefabName)
    {
        if (!monsterPrefab || string.IsNullOrEmpty(prefabName))
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
    /// Toggles if player is in bulldoze mode instead of place node
    /// </summary>
    public void toggleBulldozeTowers()
    {
        m_canBulldoze = !m_canBulldoze;
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
        // If offline (play in editor) there is no second board
        if (!PhotonNetwork.IsConnected)
            boardId = 0;

        if (m_viewBoard != boardId)
        {
            BoardManager board = GameManager.manager.getBoardManager(boardId);
            if (board)
            {
                transform.position = board.ViewPosition;
                m_viewBoard = boardId;

                if (m_playerUI)
                    m_playerUI.notifyScreenViewSwitch(m_viewBoard == m_id);
            }

            SoundEffectsManager.setActiveGroup(m_viewBoard);
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

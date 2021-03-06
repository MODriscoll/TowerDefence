﻿using System.Collections;
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
    [SerializeField] private PlayerMonstersList m_monsterList;       // List of all the monsters the player can deploy

    public delegate void OnPlayerDamaged(PlayerController controller, int damage, int health);      // Delegate for when a player loses health
    public OnPlayerDamaged onDamaged;                                                               // Event called when player takes damage
   
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
    private bool m_canUseAbilities = false;         // If we can use abilities (controlled by MasterClient)
    private bool m_monsterSpawnLocked = false;      // If we are locked from spawning monsters (delay is active)
    private bool m_canBulldoze = false;             // If clicking on a tile with a tower will destroy it (only when delay isn't active)

    private bool m_useAbilities = false;            // If viewing opponents board and we click, should we try to activate an ability

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
                    m_camera.gameObject.AddComponent<CameraEffectsController>();
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
            if (m_board)
                m_board.initFor(this);
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
            
            if (m_camera)
                m_camera.gameObject.AddComponent<CameraEffectsController>();

            m_board = GameManager.manager.getBoardManager(m_id);
            if (m_board)
                m_board.initFor(this);

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
        bool rightClick;
        if (tryGetBoardInput(out selectedPos, out rightClick))
            handleSelection(selectedPos, rightClick);
    }

    void OnDestroy()
    {
        if (localPlayer == this)
            localPlayer = null;
        if (remotePlayer == this)
            remotePlayer = null;
    }

    /// <summary>
    /// Handles a selection input based on current board player is viewing.
    /// This should only be called on the owning clients player controller
    /// </summary>
    /// <param name="screenPos">Screen position of selection</param>
    /// <param name="rightClick">If selection was made using right click</param>
    private void handleSelection(Vector3 screenPos, bool rightClick)
    {
        // Right click is being used to activate abilities while in editor,
        // in actual games (where we are connected), we want to ignore this
        if (PhotonNetwork.IsConnected && rightClick)
            return;

        // Convert from screen space to world space
        Vector3 worldPos = m_camera.ScreenToWorldPoint(screenPos);

        BoardManager viewedBoard = getViewedBoardManager();
        if (!viewedBoard)
            return;

        if (viewedBoard == Board && !rightClick)
        {
            if (!m_canPlaceTowers)
                return;

            Vector3Int tileIndex = viewedBoard.positionToIndex(worldPos);

            if (!viewedBoard.isPlaceableTile(tileIndex))
                return;

            TowerBase tower = viewedBoard.getTowerOnTile(tileIndex);
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
                Vector3 spawnPos = viewedBoard.indexToPosition(tileIndex);

                // Spawn tower on the server (replicates back to us if not master client)
                string prefabName;
                TowerBase towerPrefab = m_towersList.getSelectedTower(out prefabName);
                if (towerPrefab && canAfford(towerPrefab.m_cost))
                    placeTowerAt(towerPrefab, prefabName, tileIndex, spawnPos);
            }
        }
        else
        {
            // Right click is for using abilities while in editor
            if (!PhotonNetwork.IsConnected && !rightClick)
                return;

            AbilityBase ability = m_towersList.getSelectedAbility();
            if (!canUseAbility(ability))
                return;

            // Make sure we have actually clicked on the board 
            Vector3Int tileIndex = viewedBoard.positionToIndex(worldPos);
            if (!viewedBoard.isValidTile(tileIndex))
                return;

            // Check if this ability would allow us to select this position
            if (!ability.canUseAbilityHere(this, viewedBoard, worldPos, tileIndex))
                return;

            ability.activateAbility(this, viewedBoard, worldPos, tileIndex);
        }
    }

    /// <summary>
    /// Get a player controller based of a player Id
    /// </summary>
    /// <param name="playerId">Id of controller to get</param>
    /// <returns>Valid controller or null</returns>
    public static PlayerController getController(int playerId)
    {
        if (localPlayer && localPlayer.playerId == playerId)
            return localPlayer;
        else if (remotePlayer && remotePlayer.playerId == playerId)
            return remotePlayer;

        return null;
    }
    
    /// <summary>
    /// Gets current input of player
    /// </summary>
    /// <param name="selectedPos">Screen space position of input</param>
    /// <param name="rightClick">If Keyboard is available, was input from a right click</param>
    /// <returns>If player made input, otherwise false</returns>
    protected bool tryGetBoardInput(out Vector3 selectedPos, out bool rightClick)
    {
        rightClick = false;

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
#elif UNITY_WEBGL
        // For WebGL, touch controls seem to be supported by default,
        // thus it will overwrite the mouse input when playing in a normal browser
        useTouchControls = !Input.mousePresent;
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
            else if (Input.GetMouseButtonDown(1))
            {
                selectedPos = Input.mousePosition;
                rightClick = true;
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

    // Internal, do not call directly
    public void onTowerBuilt(TowerBase tower)
    {
        if (m_towersList)
            m_towersList.notifyTowerBuilt(tower);
    }

    // Internal, do not call directly
    public void onTowerDestroyed(TowerBase tower)
    {
        if (m_towersList)
            m_towersList.notifyTowerDestroyed(tower);
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
            m_canUseAbilities = false;

            // We lost all our health. The opponent wins!
            // TODO: Call function for master client to handle
            int winnerId = PhotonNetwork.IsConnected ? remotePlayer.m_id : -1;
            GameManager.manager.finishMatch(TDWinCondition.OutOfHealth, winnerId);
        }
        else
        {
            GameManagerCosmetics.playGoalHurtSound(m_id);
        }

        // Call this regardless
        if (onDamaged != null)
            onDamaged.Invoke(this, damage, m_health);
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
    /// Checks if we are able to activate an ability
    /// </summary>
    /// <param name="ability">The ability to try and activate</param>
    /// <returns>If ability can be used</returns>
    private bool canUseAbility(AbilityBase ability)
    {
        if (!ability)
            return false;

        if (m_canUseAbilities)// && m_useAbilities)
            if (ability.canUseAbilityNow())
                return true;

        return false;
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

    public void setBulldozeTowers(bool canBulldoze)
    {
        m_canBulldoze = !m_canBulldoze;
    }

    /// <summary>
    /// Toggles if player will use abilities when clicking on opponents board
    /// </summary>
    public void toggleUseAbilities()
    {
        m_useAbilities = !m_useAbilities;
    }

    public void setUseAbilities(bool canUse)
    {
        m_useAbilities = canUse;
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
            boardId = m_id;

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

    /// <summary>
    /// Get the board we are viewing
    /// </summary>
    /// <returns>Valid board or null</returns>
    private BoardManager getViewedBoardManager()
    {
        return GameManager.manager.getBoardManager(m_viewBoard);
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
        m_canUseAbilities = false;

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
        m_canUseAbilities = true;

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
        m_canUseAbilities = false;

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
            int newHealth = (int)stream.ReceiveNext();
            if (newHealth != m_health)
            {
                // Set our health before calling any events, so if they were to access
                // health through us its valid (also consistent with applyDamage)
                int oldHealth = m_health;
                m_health = newHealth;

                if (newHealth < oldHealth)
                    if (onDamaged != null)
                        onDamaged.Invoke(this, oldHealth - newHealth, newHealth);
            }
        }
    }
}

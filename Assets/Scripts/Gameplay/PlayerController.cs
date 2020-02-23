using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    static public PlayerController localPlayer;

    private int m_id = -1;                                          // Id of player
    [SerializeField] private Camera m_camera;                       // Players viewport of the scene
    [SerializeField] private PlayerTowersList m_towersList;         // List of all the towers the player can place

    // Only public for temp, we have game manager init this on start
    public int m_gold = 100;             // How much gold we have

    private BoardManager m_board = null;        // Cached board we use

    // The id of this player, is set in start
    public int playerId { get { return m_id; } }

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
                m_playerUI = Instantiate(m_playerUIPrefab, transform, false);
                if (m_playerUI)
                {
                    m_playerUI.m_owner = this;
                }
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                    m_id = 1;
                else
                    m_id = 0;
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
            if (m_testMonster)
                    GameManager.manager.OpponentsBoard.spawnMonster(m_testMonster.name, PhotonNetwork.LocalPlayer);
        }
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
                if (towerPrefab)
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

    public void placeTowerAt(TowerBase towerPrefab, Vector3Int tileIndex, Vector3 spawnPos)
    {
        TowerBase.spawnTower(towerPrefab, m_id, tileIndex, spawnPos);
    }

    public void giveGold(int amount)
    {
#if UNITY_EDITOR
        if (amount < 0)
        {
            Debug.LogWarning("Gold value less than zero given to player. Discarding");
            return;
        }
#endif

        m_gold += amount;
    }
}

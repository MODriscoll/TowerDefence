using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    static public PlayerController localPlayer;

    public int m_id;                                                // Id of player (either 1 or 2)
    [SerializeField] private Camera m_camera;                       // Players viewport of the scene
    [SerializeField] private PlayerTowersList m_towersList;         // List of all the towers the player can place

    // Only public for temp, we have game manager init this on start
    public int m_gold = 100;             // How much gold we have

    private BoardManager m_board = null;        // Cached board we use

    // temp
    [SerializeField] private UnityEngine.UI.Text m_debugText;

    public BoardManager Board
    {
        get
        {
            if (!m_board)
            {
                // Try to get our board, cache it if possible
                PlayerInfo playerInfo = GameManager.manager.getPlayerInfo(m_id);
                m_board = playerInfo != null ? playerInfo.boardManager : null;
            }

            return m_board;
        }
    }

    // temp serialize (for testing)
    [SerializeField] private MonsterBase m_commonMonster;           // The common monster that this player passively 'spawns'

    public MonsterBase commomMonster { get { return m_commonMonster; } }

    void Awake()
    {
        if (!m_camera)
            m_camera = GetComponentInChildren<Camera>();

        if (!m_towersList)
            m_towersList = GetComponent<PlayerTowersList>();
    }

    void Start()
    {
        if (photonView.IsMine)
            localPlayer = this;
    }

    void Update()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
        {
            return;
        }

        if (m_debugText)
            m_debugText.text = string.Format("Gold: {0}", m_gold);

#if UNITY_EDITOR || UNITY_STANDALONE
        if (m_towersList)
        {
            for (int i = (int)KeyCode.Alpha1; i <= (int)KeyCode.Alpha9; ++i)
                if (Input.GetKeyDown((KeyCode)i))
                    m_towersList.selectTower(i - (int)KeyCode.Alpha1);

            if (Input.GetKeyDown(KeyCode.Alpha0))
                m_towersList.unselectTower();
        }
#endif

        Vector3 selectedPos;
        if (tryGetBoardInput(out selectedPos))
        {
            // Convert from screen space to world space
            selectedPos = m_camera.ScreenToWorldPoint(selectedPos);

            Vector3Int tileIndex = Board.positionToIndex(selectedPos);
            if (!Board.isPlaceableTile(tileIndex))
                return;

            if (!Board.isOccupied(tileIndex))
            {
                // We call this as selectedPos is highly likely not to be
                // the center of the tile, while this 100% will be
                Vector3 spawnPos = Board.indexToPosition(tileIndex);

                // Testing
                TowerBase towerPrefab = m_towersList.getSelectedTower();
                if (towerPrefab)
                {
                    TowerBase tower = Instantiate(towerPrefab, spawnPos, Quaternion.identity);
                    tower.m_ownerId = m_id;
                    Board.placeTower(tower, tileIndex);
                }
            }
            else
            {
                // TODO: Might want to do stuff with the tower already occupied?
                Debug.Log("Tile already occupied");
            }
        }
    }

    // Photon event
    void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        m_id = (int)info.photonView.InstantiationData[0];

        m_board = GameManager.manager.getPlayerInfo(m_id).boardManager;
    }

    protected bool tryGetBoardInput(out Vector3 selectedPos)
    {
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
#if !UNITY_IOS && !UNITY_ANDROID
        // Revert to normal PC bindings
        else
        {
            // If playing in editor, only accept input for first player
            if (!PhotonNetwork.IsConnected)
                if (m_id != 0)
                {
                    selectedPos = Vector3.zero;
                    return false;
                }

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    static public PlayerController localPlayer;

    public int m_id;                                                // Id of player (either 1 or 2)
    public BoardManager m_board;                                    // The board this player interacts with
    [SerializeField] private Camera m_camera;                       // Players viewport of the scene
    [SerializeField] private PlayerTowersList m_towersList;         // List of all the towers the player can place

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

#if UNITY_EDITOR
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

            Vector3Int tileIndex = m_board.positionToIndex(selectedPos);
            if (!m_board.isPlaceableTile(tileIndex))
                return;

            if (!m_board.isOccupied(tileIndex))
            {
                // We call this as selectedPos is highly likely not to be
                // the center of the tile, while this 100% will be
                Vector3 spawnPos = m_board.indexToPosition(tileIndex);

                // Testing
                TowerBase towerPrefab = m_towersList.getSelectedTower();
                if (towerPrefab)
                {
                    TowerBase tower = Instantiate(towerPrefab, spawnPos, Quaternion.identity);
                    m_board.placeTower(tower, tileIndex);
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
        // Revert to normal PC bindings
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                selectedPos = Input.mousePosition;
                return true;
            }
        }

        selectedPos = Vector2.zero;
        return false;
    }
}

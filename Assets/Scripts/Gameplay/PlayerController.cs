using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public BoardManager m_board;                    // The board this player interacts with
    [SerializeField] private Camera m_camera;       // Players viewport of the scene

    // Testing
    public TowerBase m_towerPrefab;

    void Awake()
    {
        if (!m_camera)
            m_camera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
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
                TowerBase tower = Instantiate(m_towerPrefab, spawnPos, Quaternion.identity);
                m_board.placeTower(tower, tileIndex);
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

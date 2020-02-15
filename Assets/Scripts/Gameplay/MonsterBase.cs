using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MonsterBase : MonoBehaviourPunCallbacks
{
    public Vector2 moveDir;
    [Min(0.01f)] public float m_travelDuration = 2f;

    protected BoardManager m_board;       // The board we are active on

    private Vector3Int m_targetTileIndex;
    private Vector3 m_segmentStart;
    private Vector3 m_segmentEnd;
    private float m_progress;

    public virtual void initMoster(BoardManager boardManager)
    {
        m_board = boardManager;

        Vector3Int spawnTile = m_board.getRandomSpawnTile();
        m_targetTileIndex = m_board.getNextPathTile(spawnTile);

        m_segmentStart = m_board.indexToPosition(spawnTile);
        m_segmentEnd = m_board.indexToPosition(m_targetTileIndex);

        transform.position = m_segmentStart;
        m_progress = 0f;
    }

    public virtual void tick(float deltaTime)
    {
        m_progress += deltaTime / m_travelDuration;

        // Have we reached the end of the segment
        if (m_progress >= 1f)
        {
            if (m_board.isGoalTile(m_targetTileIndex))
            {
                MonsterManager.destroyMonster(this);
                return;
            }

            m_segmentStart = m_segmentEnd;
            m_targetTileIndex = m_board.getNextPathTile(m_targetTileIndex);
            m_segmentEnd = m_board.indexToPosition(m_targetTileIndex);

            m_progress %= 1f;
        }

        transform.position = Vector3.Lerp(m_segmentStart, m_segmentEnd, m_progress);
    }
}

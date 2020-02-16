using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonTransformView))]
public class MonsterBase : MonoBehaviourPunCallbacks
{
    [Min(0.01f)] public float m_travelDuration = 2f;

    protected BoardManager m_board;                 // The board we are active on
    protected PhotonTransformView m_photon;         // Photon component for networking

    private Vector3Int m_targetTileIndex;   // Index of board we are moving to
    private Vector3 m_segmentStart;         // Start of current path segment in world space
    private Vector3 m_segmentEnd;           // End of current path segment in world space
    private float m_progress;               // Progress along current segment
    private float m_tilesTravelled;         // Total amount of tiles this monster has travelled by

    public float TilesTravelled { get { return m_tilesTravelled; } }

    private void Awake()
    {
        //if (!m_photon)
        //{
        //    m_photon = GetComponent<PhotonTransformView>();
        //    m_photon.m_SynchronizeRotation = true;
        //}
    }

    public virtual void initMoster(BoardManager boardManager)
    {
        m_board = boardManager;

        Vector3Int spawnTile = m_board.getRandomSpawnTile();
        m_targetTileIndex = m_board.getNextPathTile(spawnTile);

        m_segmentStart = m_board.indexToPosition(spawnTile);
        m_segmentEnd = m_board.indexToPosition(m_targetTileIndex);

        transform.position = m_segmentStart;
        m_progress = 0f;
        m_tilesTravelled = 0f;
    }

    public virtual void tick(float deltaTime)
    {
        float delta = deltaTime / m_travelDuration;

        // Have we reached the end of the segment
        m_progress += delta;
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
        m_tilesTravelled += delta;
    }
}

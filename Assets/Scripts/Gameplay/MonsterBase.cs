using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MonsterBase : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback, IPunObservable
{
    [Min(0.01f)] public float m_travelDuration = 2f;        // Amount of time (in seconds) it takes to traverse 1 tile
    [Min(0)] public int m_reward = 10;                      // Reward to give player when we are killed
    [Min(1)] public int m_damage = 5;                       // The amount of damage this monster

    protected BoardManager m_board;                 // The board we are active on

    private Vector3Int m_targetTileIndex;   // Index of board we are moving to
    private Vector3 m_segmentStart;         // Start of current path segment in world space
    private Vector3 m_segmentEnd;           // End of current path segment in world space
    private float m_progress;               // Progress along current segment
    private float m_tilesTravelled;         // Total amount of tiles this monster has travelled by

    public BoardManager Board { get { return m_board; } }
    public float TilesTravelled { get { return m_tilesTravelled; } }

    void Start()
    {
        // Add ourselves to our board on the instigators side (since monsters are spawned on player who owns the board)
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            MonsterManager monsterManager = m_board.monsterManager;
            if (monsterManager)
                monsterManager.addExternalMonster(this);
        }
    }

    void OnDestroy()
    {
        // Like in start, we need to remove ourselves on the instigators side
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            MonsterManager monsterManager = m_board.monsterManager;
            if (monsterManager)
                monsterManager.removeExternalMonster(this);
        }
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
                // Damage the player
                PlayerController.localPlayer.applyDamage(m_damage);

                // We destroy ourselves after tick has concluded
                MonsterManager.destroyMonster(this, false);
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

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        int boardId = (int)info.photonView.InstantiationData[0];
        m_board = GameManager.manager.getBoardManager(boardId);
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(m_tilesTravelled);
        }
        else
        {
            m_tilesTravelled = (float)stream.ReceiveNext();
        }
    }
}

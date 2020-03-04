using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MonsterBase : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback, IPunObservable
{
    [Min(0.01f)] public float m_travelDuration = 2f;        // Amount of time (in seconds) it takes to traverse 1 tile
    [Min(0)] public int m_reward = 10;                      // Reward to give player when we are killed
    [Min(1)] public int m_damage = 5;                       // The amount of damage this monster
    public int m_health = 10;                               // How much health this monster has

    protected BoardManager m_board;                 // The board we are active on

    private Vector3Int m_targetTileIndex;   // Index of board we are moving to
    private Vector3 m_segmentStart;         // Start of current path segment in world space
    private Vector3 m_segmentEnd;           // End of current path segment in world space
    private float m_progress;               // Progress along current segment 
    private float m_tilesTravelled;         // Total amount of tiles this monster has travelled by
    private bool m_canBeDamaged = true;     // If this monster can be damaged

    public delegate void OnTakeDamage(int damage, bool bKilled);
    public OnTakeDamage OnMonsterTakenDamage;

    public BoardManager Board { get { return m_board; } }
    public float TilesTravelled { get { return m_tilesTravelled; } }

    void Start()
    {
        // Add ourselves to our board on the instigators side (since monsters are spawned on player who owns the board)
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            MonsterManager monsterManager = m_board.MonsterManager;
            if (monsterManager)
                monsterManager.addExternalMonster(this);
        }
    }

    void OnDestroy()
    {
        // Like in start, we need to remove ourselves on the instigators side
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            MonsterManager monsterManager = m_board.MonsterManager;
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

    // TODO: Document (returns true if killed)
    public bool takeDamage(int amount)
    {
        if (m_health <= 0 || !m_canBeDamaged)
            return false;

        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return false;

        if (amount <= 0)
        {
            Debug.LogError("Cannot apply less than zero damage to a monster");
            return false;
        }

        m_health = Mathf.Max(m_health - amount, 0);
        bool bKilled = m_health <= 0;

        // Execute events before potentially destroying ourselves
        if (OnMonsterTakenDamage != null)
            OnMonsterTakenDamage.Invoke(amount, bKilled);

        // Nothing more needs to be done if still alive
        if (m_health > 0)
            return false;

        // We can give gold to the local player, since we already
        // checked that this monster belongs to them
        PlayerController.localPlayer.giveGold(m_reward);

        MonsterManager.destroyMonster(this);
        return true;
    }

    public void setCanBeDamaged(bool bCanDamage)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        m_canBeDamaged = bCanDamage;
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
            stream.SendNext(m_canBeDamaged);
        }
        else
        {
            m_tilesTravelled = (float)stream.ReceiveNext();
            m_canBeDamaged = (bool)stream.ReceiveNext();
        }
    }
}

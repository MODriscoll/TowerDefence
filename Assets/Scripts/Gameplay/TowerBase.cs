using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

public class TowerBase : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback//, IPunObservable
{
    [Min(0)] public int m_cost = 10;                // The cost to build this tower (0 = free)
    public int m_health = 10;                       // How much health this tower has
    public int m_maxHealth = 10;                    // Max health this tower can have

    private int m_ownerId = -1;         // Id of player that owns this tower
    private BoardManager m_board;       // Cached board for fast access
    private float m_spawnTime = -1f;    // Time when we were spawned

    public float LifeSpan { get { return Mathf.Max(0f, Time.time - m_spawnTime); } }

    public BoardManager Board { get { return m_board; } }

    void Start()
    {
        m_spawnTime = Time.time;
    }

    void OnDestroy()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            if (m_board)
                m_board.removeTower(this);
    }

    public void setOwnerId(int playerId)
    {
        m_ownerId = playerId;
        m_board = GameManager.manager.getBoardManager(m_ownerId);
    }

    public void healTower(int amount)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (amount <= 0)
        {
            Debug.LogError("Cannot apply less than zero health to a tower");
            return;
        }

        m_health = Mathf.Max(m_maxHealth, m_health + amount);
    }

    public void takeDamage(int amount, Object instigator)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (amount <= 0)
        {
            Debug.LogError("Cannot apply less than zero damage to a tower");
            return;
        }

        m_health = Mathf.Max(m_health - amount, 0);
        if (m_health <= 0)
        {
            AnalyticsHelpers.reportTowerDestroyed(this, instigator ? instigator.name : "Unknown");
            destroyTower(this);
        }
    }

    public static TowerBase spawnTower(TowerBase towerPrefab, int playerId, Vector3Int tileIndex, Vector3 spawnPos)
    {
        object[] spawnData = new object[2];
        spawnData[0] = playerId;
        spawnData[1] = tileIndex;

        GameObject towerObj = PhotonNetwork.Instantiate(towerPrefab.name, spawnPos, Quaternion.identity, 0, spawnData);
        if (!towerObj)
            return null;

        TowerBase tower = towerObj.GetComponent<TowerBase>();
        Assert.IsNotNull(tower);

        // This only gets called once per server
        AnalyticsHelpers.reportTowerPlaced(tower, tileIndex);

        return tower;
    }

    public static void destroyTower(TowerBase tower)
    {
        PhotonView photonView = tower.photonView;
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (tower.m_board)
            tower.m_board.removeTower(tower);

        PhotonNetwork.Destroy(tower.gameObject);
    }

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        setOwnerId((int)instantiationData[0]);

        Vector3Int tile = (Vector3Int)instantiationData[1];
        if (m_board)
            m_board.placeTower(this, tile);
    }
}

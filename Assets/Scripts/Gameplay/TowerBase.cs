using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

public class TowerBase : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback//, IPunObservable
{
    [Min(0)] public int m_cost = 10;                // The cost to build this tower (0 = free)
    public int m_health = 10;                       // How much health this tower has
    public float m_targetRadius = 10f;              // Radius the tower can see
    [Min(0.01f)] public float m_fireRate = 1f;      // Fire rate of towers turret

    private float m_lastFireTime = -float.MaxValue;         // The last time the turret fired

    private int m_ownerId = -1;         // Id of player that owns this tower
    private BoardManager m_board;       // Cached board for fast access

    public BoardManager Board { get { return m_board; } }

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

    public void takeDamage(int amount)
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
            destroyTower(this);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

public class TowerBase : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback//, IPunObservable
{
    public float m_targetRadius = 10f;              // Radius the tower can see
    [Min(0.01f)] public float m_fireRate = 1f;      // Fire rate of towers turret

    private float m_lastFireTime = -float.MaxValue;         // The last time the turret fired

    private int m_ownerId = -1;         // Id of player that owns this tower
    private BoardManager m_board;       // Cached board for fast access

    void Update()
    {
        MonsterBase monster = findTarget(m_targetRadius);
        if (!monster)
            return;

        // Direction to face
        Vector2 dir = (monster.transform.position - transform.position).normalized;
        float rot = Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);

        // Instantly rotate to face target
        transform.eulerAngles = new Vector3(0f, 0f, rot);

        // Check if we can fire at this monster
        if (Time.time >= m_lastFireTime + m_fireRate)
        {
            // 'Give gold'
            PlayerInfo playerInfo = GameManager.manager.getPlayerInfo(m_ownerId);
            if (playerInfo != null && playerInfo.controller)
                playerInfo.controller.giveGold(monster.m_reward);

            // 'Shoot'
            MonsterManager.destroyMonster(monster);
            m_lastFireTime = Time.time;     
        }
    }

    protected MonsterBase findTarget(float radius)
    {
        if (m_board && m_board.monsterManager)
            return m_board.monsterManager.getHighestPriorityMonster(transform.position, radius);
        else
            return null;
    }

    public void setOwnerId(int playerId)
    {
        m_ownerId = playerId;
        m_board = GameManager.manager.getBoardManager(m_ownerId);
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

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        setOwnerId((int)instantiationData[0]);

        Vector3Int tile = (Vector3Int)instantiationData[1];
        if (m_board)
            m_board.placeTower(this, tile);
    }
}

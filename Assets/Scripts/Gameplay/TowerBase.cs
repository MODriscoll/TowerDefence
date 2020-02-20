using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class TowerBase : MonoBehaviourPunCallbacks
{
    public float m_targetRadius = 10f;              // Radius the tower can see
    [Min(0.01f)] public float m_fireRate = 1f;      // Fire rate of towers turret

    private PhotonView m_networkView;
    private float m_lastFireTime = -float.MaxValue;         // The last time the turret fired

    public int m_ownerId = -1;

    void Awake()
    {
        m_networkView = GetComponent<PhotonView>();
    }

    void Update()
    {
        MonsterBase monster = findTarget(m_targetRadius);
        if (!monster)
        {
            return;
        }

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
        if (MonsterManager.manager != null)
            return MonsterManager.manager.getHighestPriorityMonster(transform.position, radius);
        else
            return null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// The base for a scriptable tower. This works alongside TowerBase
public class TowerScript : MonoBehaviourPun
{
    [SerializeField] protected TowerBase m_tower;                       // Tower we act for
    [SerializeField] protected bool m_targetsMonsters = true;           // If this tower targets monsters
    [SerializeField] protected float m_targetRadius = 2.5f;             // Radius which tower can see towers
    [SerializeField, Min(0.01f)] protected float m_fireRate = 1f;       // Rate at which the tower acts

    protected float m_lastFireTime = -float.MaxValue;         // The last time the turret fired

    public BoardManager Board { get { return m_tower ? m_tower.Board : null; } }

    void Awake()
    {
        if (!m_tower)
            m_tower = gameObject.GetComponent<TowerBase>();
    }

    void Update()
    {
        MonsterBase target = null;
        if (m_targetsMonsters)
        {
            target = findTarget(m_targetRadius);
            if (target)
            {
                // Direction to face
                Vector2 dir = (target.transform.position - transform.position).normalized;
                float rot = Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);

                // Instantly rotate to face target
                transform.eulerAngles = new Vector3(0f, 0f, rot);
            }
        }

        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        // Tower might not be ready to perform an action yet
        if (!shouldPerformAction(target))
            return;

        // Can we perform an action yet?
        if (Time.time >= m_lastFireTime + m_fireRate)
        {
            performAction(target);
            m_lastFireTime = Time.time;
        }
    }

    protected virtual MonsterBase findTarget(float radius)
    {
        BoardManager board = m_tower ? m_tower.Board : null;
        if (board && board.MonsterManager)
            return board.MonsterManager.getHighestPriorityMonster(transform.position, radius);
        else
            return null;
    }

    // TODO: document (target can be null!), this is also called every frame (may not perform action even if this returns true)
    protected virtual bool shouldPerformAction(MonsterBase target)
    {
        return m_targetsMonsters ? target != null : true;
    }

    // TODO: document (target can be null!)
    protected virtual void performAction(MonsterBase target)
    {
        // Do nothing by default
    }
}

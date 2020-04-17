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

    [SerializeField] private Transform m_pivot;                 // The pivot to rotate when facing monsters

    protected float m_lastFireTime = -float.MaxValue;           // The last time the turret fired
    protected bool m_isDelayed = false;                         // If fire rate has been delayed
    protected float m_delayedTime = 0f;                         // Delay to wait before we can fire again

    [HideInInspector] public bool m_disableRotation = false;    // If turret should not rotate at all (quick impl)

    // Hack for dealing with rotation problems encountered with rotation of pivots.
    // As of doing this, I'm not really in the headspace to properly think of how to use
    // quats to achieve what we want, so I'm just going to set this in editor then scale the calulated rot with it
    // BasicTurret uses (0f, -1f, 0f)
    // MortarTurret uses (0f, 0f, 1f)
    public Vector3 m_rotScaler = new Vector3(0f, 0f, 1f);

    public BoardManager Board { get { return m_tower ? m_tower.Board : null; } }

    void Awake()
    {
        if (!m_tower)
            m_tower = gameObject.GetComponent<TowerBase>();
    }

    void Update()
    {
        MonsterBase target = null;
        if (m_targetsMonsters && !m_disableRotation)
        {
            target = findTarget(m_targetRadius);
            if (target)
            {
                Transform pivotTransform = transform;
                if (m_pivot)
                    pivotTransform = m_pivot;

                // Direction to face
                Vector2 dir = (target.transform.position - pivotTransform.position).normalized;
                float rot = Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);

                // Instantly rotate to face target (see comment for m_rotScaler)
                pivotTransform.localEulerAngles = m_rotScaler * rot;
            }
        }

        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (m_isDelayed)
            m_delayedTime += Time.deltaTime;

        // Tower might not be ready to perform an action yet
        if (!shouldPerformAction(target))
            return;

        // Can we perform an action yet?
        float waitTillTime = m_lastFireTime + m_fireRate + m_delayedTime;
        if (Time.time >= waitTillTime)
        {
            performAction(target);
            m_lastFireTime = Time.time;
            m_delayedTime = 0f;
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

    /// <summary>
    /// Set if this towers fire rate should be delayed. It will delay
    /// it by however long this is active. 
    /// </summary>
    /// <param name="delay"></param>
    public void setActionsDelayed(bool delay)
    {
        m_isDelayed = delay;
    }

    [PunRPC]
    public void setDisableRotation(bool disable)
    {
        m_disableRotation = disable;
    }
}

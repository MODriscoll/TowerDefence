using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// The monster is able to shoot an towers on the board
public class ShootingMonster : MonoBehaviourPun
{
    [SerializeField] private MonsterBase m_monster;                     // Monster we act for
    [SerializeField] private float m_targetRadius = 2.5f;               // Radius which monster can see towers
    [SerializeField, Min(0.01f)] private float m_fireRate = 1f;         // Rate at which the monster fires
    [SerializeField, Min(1)] private int m_damage = 3;                  // How much damage this monster applies to towers per shot 
    
    private float m_lastFireTime = -float.MaxValue;     // The last time monster fired

    // Testing
    public TestMonsterFireEffect m_testEffect;

    void Awake()
    {
        if (!m_monster)
            m_monster = gameObject.GetComponent<MonsterBase>();
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        TowerBase target = findTarget(m_targetRadius);
        if (!target)
            return;

        if (Time.time >= m_lastFireTime + m_fireRate)
        {
            target.takeDamage(m_damage);

#if UNITY_EDITOR
            onFired(target.transform.position);
#else
            photonView.RPC("onFired", RpcTarget.All, target.transform.position);
#endif

            m_lastFireTime = Time.time;
        }
    }

    protected virtual TowerBase findTarget(float radius)
    {
        if (m_monster && m_monster.Board)
            return m_monster.Board.getClosestTowerTo(transform.position, radius);
        else
            return null;
    }

    [PunRPC]
    private void onFired(Vector3 position)
    {
        if (m_testEffect)
            Instantiate(m_testEffect, position, Quaternion.identity);
    }
}

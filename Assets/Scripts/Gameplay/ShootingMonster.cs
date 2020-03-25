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

    [SerializeField] private ShootingMonsterEffect m_effectPrefab;      // Effect prefab to spawn when shooting
    [SerializeField] private AudioClip m_shootSound;                    // Sound to play when this monster shoots
    
    private float m_lastFireTime = -float.MaxValue;     // The last time monster fired

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
            target.takeDamage(m_damage, this);
            m_lastFireTime = Time.time;

            // Play cosmetics
            if (PhotonNetwork.IsConnected)
                photonView.RPC("onFired", RpcTarget.All, target.transform.position);
            else
                onFired(target.transform.position);          
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
        SoundEffectsManager.playSoundEffect(m_shootSound, m_monster.Board);

        if (m_effectPrefab)
            Instantiate(m_effectPrefab, position, Quaternion.identity);
    }
}

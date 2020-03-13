using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TowerProjectile : MonoBehaviourPun
{
    [SerializeField, Min(1)] private int m_damage = 10;         // How much damage this projectile does
    [SerializeField] private float m_speed = 1f;                // Speed at which projectile flies
    [SerializeField] private float m_lifespan = 3f;             // How long this projectile lasts
    [SerializeField] private float m_radius = 0.5f;             // Radius of this projectile
    [SerializeField] private float m_tickRate = 0.5f;           // Tick rate for checking collisions (to prevent checks every frame)

    private HashSet<MonsterBase> m_hitMonsters = new HashSet<MonsterBase>();    // The monsters we have hit thus far
    private BoardManager m_board;                                               // The board to check monsters for
    private TowerScript m_script;                                               // The script that instigated this projectile


    Vector3 m_moveDir;

    void Update()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        // Move ourselves forward
        transform.Translate(m_moveDir * m_speed * Time.deltaTime);
    }

    public void initProjectile(Vector3 moveDir, BoardManager board, TowerScript script)
    {
        m_moveDir = moveDir;

        m_board = board;
        m_script = script;
        StartCoroutine(checkCollisionRoutine());

        Invoke("onLifespanExpired", m_lifespan);
    }

    private void collide()
    {
        if (!m_board)
            return;

        List<MonsterBase> hitMonsters = new List<MonsterBase>();
        m_board.MonsterManager.getMonstersInRadius(transform.position, m_radius, ref hitMonsters, m_hitMonsters);

        foreach (MonsterBase monster in hitMonsters)
        {
            if (!monster.takeDamage(m_damage, m_script))
                m_hitMonsters.Add(monster);
        }
    }

    private IEnumerator checkCollisionRoutine()
    {
        // We will always be able to collide while active
        while (isActiveAndEnabled)
        {
            collide();
            yield return new WaitForSeconds(m_tickRate);
        }
    }

    private void onLifespanExpired()
    {
        if (PhotonNetwork.IsConnected)
        {
            if (photonView.IsMine)
                PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }
#endif
}

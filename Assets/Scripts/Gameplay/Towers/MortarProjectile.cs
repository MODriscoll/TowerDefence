using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MortarProjectile : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [SerializeField, Min(1)] private int m_damage = 10;         // How much damage this projectile does
    [SerializeField] private float m_speed = 1f;                // Speed at which projectile flies
    [SerializeField] private float m_lifespan = 3f;             // How long this projectile lasts
    [SerializeField] private float m_radius = 0.5f;             // Radius of this projectile
    [SerializeField] private float m_tickRate = 0.5f;           // Tick rate for checking collisions (to prevent checks every frame)
    [SerializeField, Min(0)] private int m_maxHits = 3;         // Max amount of monsters this projectile can hurt before destroying itself

    private HashSet<MonsterBase> m_hitMonsters = new HashSet<MonsterBase>();    // The monsters we have hit thus far
    private BoardManager m_board;                                               // The board to check monsters for
    private TowerScript m_script;                                               // The script that instigated this projectile

    private Vector3 m_cachedMoveDir = Vector3.zero;     // Cached direction to move in
    private float m_force = 0f;                         // Force of projectile

    void Update()
    {
        m_force += m_speed * Time.deltaTime;
        m_force = Mathf.Min(m_force, m_speed);

        // Move ourselves forward, we simulate movement locally
        transform.position += m_cachedMoveDir * m_force * Time.deltaTime;    
    }

    public void initProjectile(Vector3 eulerAngles, BoardManager board, TowerScript script)
    {
        setMovementDirection(eulerAngles);

        m_board = board;
        m_script = script;
        StartCoroutine(checkCollisionRoutine());

        Invoke("destroySelf", m_lifespan);
    }

    private bool collide()
    {
        if (!m_board)
            return false;

        List<MonsterBase> hitMonsters = new List<MonsterBase>();
        m_board.MonsterManager.getMonstersInRadius(transform.position, m_radius, ref hitMonsters, m_hitMonsters);

        foreach (MonsterBase monster in hitMonsters)
        {
            monster.takeDamage(m_damage, m_script);
            m_hitMonsters.Add(monster);
        }

        if (m_hitMonsters.Count >= m_maxHits)
        {
            destroySelf();
            return true;
        }

        return false;
    }

    private IEnumerator checkCollisionRoutine()
    {
        // We will always be able to collide while active
        while (isActiveAndEnabled)
        {
            if (collide())
                yield break;

            yield return new WaitForSeconds(m_tickRate);
        }
    }

    private void destroySelf()
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

    private void setMovementDirection(Vector3 eulerAngles)
    {
        transform.eulerAngles = eulerAngles;

        float rad = eulerAngles.z * Mathf.Deg2Rad;
        m_cachedMoveDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        float eulerZ = (float)info.photonView.InstantiationData[0];
        setMovementDirection(new Vector3(0f, 0f, eulerZ));
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }
#endif
}

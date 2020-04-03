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

    [SerializeField] private MortarProjectileEffect m_effectPrefab;         // Prefab to play when we expire
    [SerializeField] private MortarTurretEffect m_turretEffectPrefab;       // Placing this here as we can avoid an RPC call

    [SerializeField] private AudioClip m_spawnSound;            // Sound to play on spawn (this is basically MortarProjectile.m_shootSound)
    [SerializeField] private AudioClip m_hitSound;              // Sound to play when we hit something
    [SerializeField] private AudioClip m_expiredSound;          // Sound to play when we expire

    private HashSet<MonsterBase> m_hitMonsters = new HashSet<MonsterBase>();    // The monsters we have hit thus far
    private BoardManager m_board;                                               // The board to check monsters for
    private TowerScript m_script;                                               // The script that instigated this projectile

    private Vector3 m_cachedMoveDir = Vector3.zero;     // Cached direction to move in
    private float m_force = 0f;                         // Force of projectile

    void Start()
    {
        if (m_turretEffectPrefab)
            Instantiate(m_turretEffectPrefab, transform.position, Quaternion.identity);
    }

    void Update()
    {
        m_force += m_speed * Time.deltaTime;
        m_force = Mathf.Min(m_force, m_speed);

        // Move ourselves forward, we simulate movement locally
        transform.position += m_cachedMoveDir * m_force * Time.deltaTime;    
    }

    public void initProjectile(BoardManager board, TowerScript script)
    {
        m_board = board;
        m_script = script;
        StartCoroutine(checkCollisionRoutine());

        Invoke("onExpired", m_lifespan);
    }

    private bool collide()
    {
        if (!m_board)
            return false;

        List<MonsterBase> hitMonsters = new List<MonsterBase>();
        if (m_board.MonsterManager.getMonstersInRadius(transform.position, m_radius, ref hitMonsters, m_hitMonsters))
        {
            foreach (MonsterBase monster in hitMonsters)
            {
                // Only record this monster if still alive
                if (!monster.takeDamage(m_damage, m_script))
                    m_hitMonsters.Add(monster);
            }

            if (m_hitMonsters.Count >= m_maxHits)
            {
                // This RPC will call destroy for us (see onExpired)
                if (PhotonNetwork.IsConnected)
                    photonView.RPC("onExpired", RpcTarget.All);
                else
                    onExpired();

                return true;
            }
            else
            {
                if (PhotonNetwork.IsConnected)
                    photonView.RPC("onMonstersHit", RpcTarget.All, (Vector2)transform.position);
                else
                    onMonstersHit(transform.position);
            }
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

    [PunRPC]
    private void onMonstersHit(Vector2 position)
    {
        SoundEffectsManager.playSoundEffect(m_hitSound, m_board);
    }

    [PunRPC]
    private void onExpired()
    {
        if (m_effectPrefab)
            Instantiate(m_effectPrefab, transform.position, Quaternion.identity);

        SoundEffectsManager.playSoundEffect(m_expiredSound, m_board);

        // Hide ourselves (we only want expire effect to play)
        gameObject.SetActive(false);

        // This handles case where we might not be owner
        destroySelf();
    }

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        int playerId = (int)info.photonView.InstantiationData[0];
        m_board = GameManager.manager.getBoardManager(playerId);

        float eulerZ = (float)info.photonView.InstantiationData[1];
        setMovementDirection(new Vector3(0f, 0f, eulerZ));

        // Play here since this gets called after Start
        SoundEffectsManager.playSoundEffect(m_spawnSound, m_board);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }
#endif
}

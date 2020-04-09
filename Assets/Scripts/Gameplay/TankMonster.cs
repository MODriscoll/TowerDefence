using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TankMonster : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private MonsterBase m_monster;
    [SerializeField] private float m_shieldDuration = 2f;       // How long the shield lasts for (in seconds)
    [SerializeField] private float m_shieldRadius = 1f;         // Size of the shield, used for collision
    [SerializeField] private float m_cooldown = 3f;             // How long cooldown lasts after using shield
    [SerializeField] private GameObject m_shieldObject;         // Shield prefab to show/hide activation
    [SerializeField] private float m_tickRate = 0.2f;           // Tick rate for checking collisions (to prevent checks every frame)

    [SerializeField] private AudioClip m_activeSound;           // Sound to play when activating shield
    [SerializeField] private AudioClip m_deactiveSound;         // Sound to play when deactivating shield

    private bool m_shieldActive = false;            // If shield is currently active
    private bool m_cooldownActive = false;          // If shield cooldown is active
    private Coroutine m_collisionRoutine = null;    // Coroutine running to check collisions
    void Awake()
    {
        if (m_shieldObject)
            m_shieldObject.SetActive(false);

        if (!m_monster)
            m_monster = gameObject.GetComponent<MonsterBase>();

        if (m_monster)
            m_monster.OnMonsterTakenDamage += onTakenDamage;
    }

    private void setShieldEnabled(bool bEnable)
    {
        if (bEnable != m_shieldActive)
        {
            // Can't active shield while on cooldown
            if (bEnable && m_cooldownActive)
                return;

            m_shieldActive = bEnable;

            if (m_shieldObject)
                m_shieldObject.SetActive(m_shieldActive);

            if (m_monster)
                m_monster.setCanBeDamaged(!m_shieldActive);

            if (m_shieldActive)
            {
                if (m_collisionRoutine == null)
                    m_collisionRoutine = StartCoroutine(checkCollisionRoutine());

                Invoke("onShieldExpired", m_shieldDuration);
            }

            // Play sounds
            {
                // TODO: Make a base class for monster scripts (Like TowerScripts). This would handle getting monster on awake
                // for now
                MonsterBase monster = GetComponent<MonsterBase>();
                if (monster)
                {
                    if (m_shieldActive)
                    {
                        if (m_activeSound)
                            SoundEffectsManager.playSoundEffect(m_activeSound, monster.Board);
                    }
                    else
                    {
                        if (m_deactiveSound)
                            SoundEffectsManager.playSoundEffect(m_deactiveSound, monster.Board);
                    }
                }
            }
        }
    }

    private void onTakenDamage(int amount, bool bKilled)
    {
        if (bKilled)
            return;

        setShieldEnabled(true);
    }

    private void onShieldExpired()
    {
        setShieldEnabled(false);

        if (m_collisionRoutine != null)
        {
            StopCoroutine(m_collisionRoutine);
            m_collisionRoutine = null;
        }

        m_cooldownActive = true;
        Invoke("onCooldownExpired", m_cooldown);
    }

    private void onCooldownExpired()
    {
        m_cooldownActive = false;
    }

    private IEnumerator checkCollisionRoutine()
    {
        while (m_shieldActive)
        {
            // Destroy any projectiles we are colliding with
            List<MortarProjectile> blockedProjectiles = new List<MortarProjectile>();
            if (MortarProjectile.getProjectilesInRadius(transform.position, m_shieldRadius, ref blockedProjectiles))
            {
                foreach (MortarProjectile projectile in blockedProjectiles)
                    MortarProjectile.destroyProjectile(projectile);
            }

            yield return new WaitForSeconds(m_tickRate);
        }
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(m_shieldActive);
        }
        else
        {
            bool shieldActive = (bool)stream.ReceiveNext();
            setShieldEnabled(shieldActive);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, m_shieldRadius);
    }
}

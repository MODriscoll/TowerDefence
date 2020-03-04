using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TankMonster : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private MonsterBase m_monster;
    [SerializeField] private float m_shieldDuration = 2f;       // How long the shield lasts for (in seconds)
    [SerializeField] private float m_cooldown = 3f;             // How long cooldown lasts after using shield
    [SerializeField] private GameObject m_shieldObject;         // Shield prefab to show/hide activation

    private bool m_shieldActive = false;            // If shield is currently active
    private bool m_cooldownActive = false;          // If shield cooldown is active
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
            Debug.LogError("Updating");

            // Can't active shield while on cooldown
            if (bEnable && m_cooldownActive)
                return;

            m_shieldActive = bEnable;

            if (m_shieldObject)
                m_shieldObject.SetActive(m_shieldActive);

            if (m_monster)
                m_monster.setCanBeDamaged(!m_shieldActive);

            if (m_shieldActive)
                Invoke("onShieldExpired", m_shieldDuration);
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

        m_cooldownActive = true;
        Invoke("onCooldownExpired", m_cooldown);
    }

    private void onCooldownExpired()
    {
        m_cooldownActive = false;
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
}

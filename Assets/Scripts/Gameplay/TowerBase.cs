using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class TowerBase : MonoBehaviourPunCallbacks
{
    public float m_targetRadius = 10f;     // Radius the tower can see

    private PhotonView m_networkView;

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
    }

    protected MonsterBase findTarget(float radius)
    {
        if (MonsterManager.manager != null)
            return MonsterManager.manager.getClosestMonsterTo(transform.position, radius);
        else
            return null;
    }
}

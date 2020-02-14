using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMonsterSpawner : MonoBehaviour
{
    public MonsterBase m_prefab;            // The prefab to spawn
    public float m_spawnInterval = 1f;      // Interval for spawning monsters

    void Start()
    {
        InvokeRepeating("spawnMonster", m_spawnInterval, m_spawnInterval);
    }

    public void spawnMonster()
    {
        MonsterBase monster = MonsterManager.manager.spawnMonster(m_prefab, transform.position);
        monster.moveDir = new Vector2(transform.forward.x, transform.forward.z);
        StartCoroutine(destroyMonsterDelayed(monster, 5f));
    }

    IEnumerator destroyMonsterDelayed(MonsterBase monster, float delay)
    {
        yield return new WaitForSeconds(delay);
        MonsterManager.destroyMonster(monster);
    }
}

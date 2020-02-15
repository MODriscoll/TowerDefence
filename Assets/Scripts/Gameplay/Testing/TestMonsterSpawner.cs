using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMonsterSpawner : MonoBehaviour
{
    public MonsterBase m_prefab;            // The prefab to spawn
    public BoardManager m_board;            // The board to have monster follow
    public float m_spawnInterval = 1f;      // Interval for spawning monsters

    void Start()
    {
        InvokeRepeating("spawnMonster", m_spawnInterval, m_spawnInterval);

        if (!m_board)
            Debug.LogWarning("TestMonsterSpawner: No Board Set");
    }

    public void spawnMonster()
    {
        if (m_board)
            MonsterManager.manager.spawnMonster_Test(m_prefab, m_board);
    }
}

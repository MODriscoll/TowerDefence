using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMonsterSpawner : MonoBehaviour
{
    //public MonsterBase m_prefab;            // The prefab to spawn
    public WaveSpawner m_spawner;                                                  // Spawner to test with
    public MonsterSpawnInfo[] m_testWave;                          // Test wave to use for the spawn
    public BoardManager m_board;                                                   // The board to have monster follow
    public float m_spawnInterval = 1f;                                             // Interval for spawning monsters

    void Awake()
    {
        if (m_spawner)
            m_spawner.initWave(new List<MonsterSpawnInfo>(m_testWave));
    }

    void Start()
    {    
        if (m_spawner)
            InvokeRepeating("spawnMonster", m_spawnInterval, m_spawnInterval);

        if (!m_board)
            Debug.LogWarning("TestMonsterSpawner: No Board Set");
    }

    public void spawnMonster()
    {
        m_spawner.spawnMonster(m_board);
        //if (m_board)
        //    MonsterManager.manager.spawnMonster_Test(m_prefab, m_board);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Responsible for spawning a wave of monsters
public class WaveSpawner : MonoBehaviour
{
    // Better name
    protected class RuntimeSpawnInfo
    {
        public int m_count;
        public int m_maxSpawn;
    }

    public List<MonsterSpawnInfo> m_spawnInfos;

    private LinkedList<MonsterSpawnInfo> m_spawnQueue;  // Using linked list as we are no expecting to have many nodes
    private Dictionary<MonsterSpawnInfo, RuntimeSpawnInfo> m_runtimeData;

    public void initWave(List<MonsterSpawnInfo> spawnInfos)
    {
        m_spawnInfos = spawnInfos;
        m_spawnInfos.Sort(delegate (MonsterSpawnInfo lhs, MonsterSpawnInfo rhs)
        {
            return Mathf.Clamp(lhs.SpawnRate - rhs.SpawnRate, -1, 1);
        });

        m_spawnQueue = new LinkedList<MonsterSpawnInfo>();
        m_runtimeData = new Dictionary<MonsterSpawnInfo, RuntimeSpawnInfo>();

        foreach (MonsterSpawnInfo spawnInfo in m_spawnInfos)
        {
            m_spawnQueue.AddLast(spawnInfo);

            RuntimeSpawnInfo runtimeInfo = new RuntimeSpawnInfo();
            runtimeInfo.m_count = spawnInfo.SpawnRate;

            m_runtimeData.Add(spawnInfo, runtimeInfo);
        }
    }

    public void spawnMonster(BoardManager board)
    {
        if (m_spawnQueue == null ||
            m_spawnQueue.Count == 0)
        {
            return;
        }

        foreach (var entry in m_runtimeData)
        {
            entry.Value.m_count--;
        }

        MonsterSpawnInfo monsterToSpawn = m_spawnQueue.First.Value;
        RuntimeSpawnInfo runtimeInfo = m_runtimeData[monsterToSpawn];
        if (runtimeInfo.m_count <= 0)
        {
            spawnMonsterImpl(monsterToSpawn.Monster, board);

            runtimeInfo.m_count = monsterToSpawn.SpawnRate;
            
            // TODO: Find entry with lowest m_count, put that at front of queue
            foreach (var entry in m_runtimeData)
            {
                if (entry.Key == monsterToSpawn)
                    continue;

                if (runtimeInfo.m_count > entry.Value.m_count)
                {
                    var node = m_spawnQueue.Find(entry.Key);
                    m_spawnQueue.RemoveFirst();
                    m_spawnQueue.AddAfter(node, monsterToSpawn);
                    break;
                }
            }
        }
    }

    private void spawnMonsterImpl(MonsterBase prefab, BoardManager board)
    {
        if (board)
            MonsterManager.manager.spawnMonster(prefab, board);
    }
}

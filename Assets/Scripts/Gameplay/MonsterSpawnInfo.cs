using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Monster Spawn Info")]
public class MonsterSpawnInfo : ScriptableObject
{
    [SerializeField] private MonsterBase m_monster;             // Monster to spawn

    // Spawn rate for this monster. This refers to how many monster spawns
    // we should wait till spawning the next monster of this type
    [SerializeField, Min(1)] private int m_spawnRate = 1;       // Spawn rate for this monster

    [SerializeField] private int m_maxSpawn = 10;   // TODO: What about rounds? We might want to spawn more

    public MonsterBase Monster { get { return m_monster; } }
    public int SpawnRate { get { return m_spawnRate; } }
    public int MaxSpawn { get { return m_maxSpawn; } }
}

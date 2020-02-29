using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A special monster is one that the player can spawn in
public class SpecialMonster : MonsterBase
{
    [SerializeField, Min(0)] private int m_cost = 100;      // The cost for deploying this monster
    [SerializeField, Min(0f)] private float m_delay = 1f;   // The delay before player can spawn another monster after this one
    
    public int Cost { get { return m_cost; } }              // How much gold is required to spawn this monster
    public float Delay { get { return m_delay; } }          // How long to delay another special monster spawn
}

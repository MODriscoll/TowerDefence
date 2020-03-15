using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMonstersList : MonoBehaviour
{
    // Prefabs for monsters that can be spawned
    [PhotonPrefab(typeof(SpecialMonster))]
    [SerializeField] private List<string> m_monsters = new List<string>();   

    void Start()
    {
        // Load in each resource now
        foreach (string prefab in m_monsters)
            Resources.Load(prefab);
    }

    public SpecialMonster getMonster(int index, out string prefabName)
    {
        if (isValidIndex(index))
        {
            // Gets reset back to null if prefab is invalid
            prefabName = m_monsters[index];

            GameObject monsterObject = Resources.Load(prefabName) as GameObject;
            if (monsterObject)
                return monsterObject.GetComponent<SpecialMonster>();
        }

        prefabName = null;
        return null;
    }

    private bool isValidIndex(int index)
    {
        if (m_monsters != null)
            if (index >= 0 && index < m_monsters.Count)
                return true;

        return false;
    }
}

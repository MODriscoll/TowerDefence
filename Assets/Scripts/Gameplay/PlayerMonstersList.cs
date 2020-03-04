using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMonstersList : MonoBehaviour
{
    [SerializeField] private List<SpecialMonster> m_monsters = new List<SpecialMonster>();

    public SpecialMonster getMonster(int index)
    {
        if (isValidIndex(index))
            return m_monsters[index];
        else
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

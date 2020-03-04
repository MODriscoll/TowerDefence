using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTowersList : MonoBehaviour
{
    [SerializeField] private int m_selectedTower = -1;
    [SerializeField] private List<TowerBase> m_towers = new List<TowerBase>();

    public bool hasSelectedTower { get { return m_selectedTower != -1; } }

    void Start()
    {
        selectTower(0);
    }

    public TowerBase selectTower(int index)
    {
        if (isValidIndex(index))
        {
            m_selectedTower = index;
            return m_towers[m_selectedTower];
        }
        else
        {
            m_selectedTower = -1;
            return null;
        }
    }

    public void unselectTower()
    {
        selectTower(-1);
    }

    public TowerBase getSelectedTower()
    {
        return getTower(m_selectedTower);
    }

    public TowerBase getTower(int index)
    {
        if (isValidIndex(index))
            return m_towers[index];

        return null;
    }

    private bool isValidIndex(int index)
    {
        if (m_towers != null)
            if (index >= 0 && index < m_towers.Count)
                return true;

        return false;
    }
}

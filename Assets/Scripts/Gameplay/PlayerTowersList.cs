using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTowersList : MonoBehaviour
{
    // Prefabs for towers that can be spawned
    [PhotonPrefab(typeof(TowerBase))]
    [SerializeField] private List<string> m_towers = new List<string>();

    private int m_selectedTower = -1;           // Tower player has selected

    public bool hasSelectedTower { get { return m_selectedTower != -1; } }      // If a tower has been selected by the player

    void Start()
    {
        // Load in each resource now
        foreach (string prefab in m_towers)
            Resources.Load(prefab);
    }

    public TowerBase selectTower(int index, out string prefabName)
    {
        if (isValidIndex(index))
        {
            m_selectedTower = index;

            return getTower(m_selectedTower, out prefabName);
        }
        else
        {
            
            m_selectedTower = -1;

            prefabName = null;
            return null;
        }
    }

    public void unselectTower()
    {
        m_selectedTower = -1;
    }

    public TowerBase getSelectedTower(out string prefabName)
    {
        return getTower(m_selectedTower, out prefabName);
    }

    public TowerBase getTower(int index, out string prefabName)
    {
        if (isValidIndex(index))
        {
            // Gets reset back to null if prefab is invalid
            prefabName = m_towers[index];

            GameObject towerObject = Resources.Load(m_towers[index]) as GameObject;
            if (towerObject)
                return towerObject.GetComponent<TowerBase>();
        }

        prefabName = null;
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTowersList : MonoBehaviour
{
    public class ActiveTowersInfo
    {
        public int m_count;             // Number of towers that have been built
        public AbilityBase m_ability;   // Ability that has been unlocked by building this tower
    }

    // Prefabs for towers that can be spawned
    [PhotonPrefab(typeof(TowerBase))]
    [SerializeField] private List<string> m_towers = new List<string>();

    private int m_selectedTower = -1;                                           // Tower player has selected
    private Dictionary<System.Type, ActiveTowersInfo> m_activeTowerInfos;       // Info about towers player has built per type
    private List<AbilityBase> m_availableAbilities;                             // List of abilities available for use

    public bool hasSelectedTower { get { return m_selectedTower != -1; } }      // If a tower has been selected by the player

    void Start()
    {
        m_activeTowerInfos = new Dictionary<System.Type, ActiveTowersInfo>();
        m_availableAbilities = new List<AbilityBase>();

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

    public void notifyTowerBuilt(TowerBase tower)
    {
        System.Type type = tower.GetType();

        ActiveTowersInfo info = null;
        if (!m_activeTowerInfos.TryGetValue(type, out info))
        {
            info = new ActiveTowersInfo();
            info.m_count = 0;
            info.m_ability = tower.Ability;

            m_activeTowerInfos.Add(type, info);
            m_availableAbilities.Add(info.m_ability);
        }

        ++info.m_count;
    }

    public AbilityBase getSelectedAbility()
    {
        // Haven't decided how setting active ability works just yet
        if (m_activeTowerInfos.Count > 0)
            return m_activeTowerInfos.Values.GetEnumerator().Current.m_ability;
        else
            return null;
    }

    public void notifyTowerDestroyed(TowerBase tower)
    {
        System.Type type = tower.GetType();

        ActiveTowersInfo info = null;
        if (m_activeTowerInfos.TryGetValue(type, out info))
        {
            --info.m_count;
            if (info.m_count <= 0)
            {
                m_availableAbilities.Remove(info.m_ability);
                m_activeTowerInfos.Remove(type);
            }
        }
    }

    private bool isValidIndex(int index)
    {
        if (m_towers != null)
            if (index >= 0 && index < m_towers.Count)
                return true;

        return false;
    }
}

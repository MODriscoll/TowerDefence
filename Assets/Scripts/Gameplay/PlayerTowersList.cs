using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTowersList : MonoBehaviour
{
    [SerializeField] private int m_selectedTower = -1;
    [SerializeField] private List<TowerBase> m_towers = new List<TowerBase>();

    public bool hasSelectedTower { get { return m_selectedTower != -1; } }

#if UNITY_EDITOR
    // temp (we don't have UI yet)
    [SerializeField] private UnityEngine.UI.Text m_debugText;

    void Update()
    {
        if (m_debugText)
        {
            TowerBase selectedTower = getSelectedTower();
            if (selectedTower)
                m_debugText.text = string.Format("Selected Tower: {0}", selectedTower.ToString());
            else
                m_debugText.text = "No Selected Tower";
        }
    }
#endif

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
        // We consider index to always be invalid if no towers are present
        if (m_towers != null)
            if (index >= 0 && index < m_towers.Count)
                return true;

        return false;
    }
}

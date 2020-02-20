using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Simple component for storing managers relating to a single player
public class PlayerInfo : MonoBehaviour
{
    public PlayerController m_controller;
    public BoardManager m_boardManager;
    public MonsterManager m_monsterManager;

    public PlayerController controller { get { return m_controller; } }
    public BoardManager boardManager { get { return m_boardManager; } }
    public MonsterManager monsterManager { get { return m_monsterManager; } }

    public bool isValid { get { return isValidImpl(); } }

    private bool isValidImpl()
    {
        return
            true && //m_controller != null &&
            m_boardManager != null &&
            true;// m_monsterManager != null;
    }
}

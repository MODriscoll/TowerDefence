using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MiceReaction : MonoBehaviour
{
    [SerializeField] GameObject m_reactionPrefab;                         //a placeholder for the reaction spawned on mice getting hit 
    [SerializeField] float m_chanceReaction;                              //a decimal chance of the reaction happening
    [SerializeField] Transform m_reactionPos;                             //a position for the reaction to be spawn at

    public void Ouch()
    {
        //random.value retuns a float between 0 and 1. 
        if (Random.value < m_chanceReaction)
            if (m_reactionPrefab)
            Instantiate(m_reactionPrefab, new Vector3(m_reactionPos.position.x, m_reactionPos.position.y, -0.02f), Quaternion.identity);
    }
}

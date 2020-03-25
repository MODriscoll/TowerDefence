using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MortarTurretEffect : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float m_lifeSpan = 0.25f;       // How long this effect lasts

    void Start()
    {
        Destroy(gameObject, m_lifeSpan);
    }
}

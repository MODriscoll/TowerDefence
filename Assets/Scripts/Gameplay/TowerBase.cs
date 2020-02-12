using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TowerBase : MonoBehaviourPunCallbacks
{
    public float m_targetRadius = 2.5f;

    // temp
    public Camera m_Camera;

    private PhotonView m_networkView;

    void Awake()
    {
        m_networkView = GetComponent<PhotonView>();
    }

    void Update()
    {
        //if (!m_networkView.IsMine)
        //{
        //    return;
        //}

        Vector2 mousePos = GetTargetLocation();
        Vector3 eulerRot = Vector3.zero;

        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        Vector2 dir = mousePos - pos;
        if (dir.sqrMagnitude > m_targetRadius * m_targetRadius)
        {
            return;
        }

        dir.Normalize();
        float x = dir.x;
        float y = dir.y;
        eulerRot.z = Mathf.Atan2(y, x);

        transform.eulerAngles = eulerRot;
    }

    public Vector2 GetTargetLocation()
    {
        Vector2 mousePos = Input.mousePosition;
        if (m_Camera)
        {
            mousePos = m_Camera.transform.TransformPoint(m_Camera.ScreenToWorldPoint(Input.mousePosition));
        }

        return mousePos;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MonsterBase : MonoBehaviourPunCallbacks
{
    public Vector2 moveDir;

    protected BoardManager m_board;       // The board we are active on

    public virtual void initMoster(BoardManager boardManager)
    {
        m_board = boardManager;
    }

    public virtual void tick(float deltaTime)
    {
        // testing
        transform.position += (Vector3)(moveDir * 5f * deltaTime);
    }
}

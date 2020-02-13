using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Temporary location
public class MonsterBase : MonoBehaviourPunCallbacks
{

}

// Temporary location
public class MonsterManager
{
    // Pointer to manager currently in use, we use this
    // as we want to find monsters but not through collision
    public static MonsterManager manager;

    /// <summary>
    /// Get all monsters within radius of position
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="radius">Radius of check</param>
    /// <returns>List of monsters or null if no monsters in radius</returns>
    public List<MonsterBase> getMonstersInRadius(Vector2 position, float radius)
    {
        float radSqr = radius * radius;

        // Iterate each monster and add all monsters within sqr distance to list
        List<MonsterBase> foundMonsters = new List<MonsterBase>();
        foreach (MonsterBase monster in m_monsters)
        {
            Vector2 dis = (Vector2)monster.transform.position - position;
            if (dis.sqrMagnitude <= radSqr)
            {
                foundMonsters.Add(monster);
            }
        }

        // Return null to signal no monsters in range
        if (foundMonsters.Count > 0)
            return foundMonsters;
        else
            return null;
    }

    /// <summary>
    /// Get the monster closest to position that is still within the given radius
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="radius">Radius monster must be in</param>
    /// <returns>Closest monster or null if no monster is in range</returns>
    public MonsterBase getClosestMonsterTo(Vector2 position, float radius = 1000f)
    {
        float radSqr = radius * radius;

        // The current closest monster and its distance from position (squared)
        MonsterBase closest = null;
        float closestDis = float.MaxValue;

        // Iterate each monster and find the closest one
        foreach (MonsterBase monster in m_monsters)
        {
            Vector2 dis = (Vector2)monster.transform.position - position;

            // Monster needs to be in radius for it to be considered
            float magSqr = dis.sqrMagnitude;
            if (magSqr <= radSqr && magSqr < closestDis)
            {
                closest = monster;
                closestDis = magSqr;
            }
        }

        return closest;
    }

    private List<MonsterBase> m_monsters;
}

[RequireComponent(typeof(PhotonView))]
public class TowerBase : MonoBehaviourPunCallbacks
{
    public float m_targetRadius = 2.5f;     // Radius the tower can see
    public Transform m_Target;              // The target for use to look at

    private PhotonView m_networkView;

    void Awake()
    {
        MonsterManager.manager = new MonsterManager();
        m_networkView = GetComponent<PhotonView>();
    }

    void Update()
    {
        if (!m_Target)
        {
            return;
        }

        Vector2 mousePos = m_Target.position;
        Vector3 eulerRot = Vector3.zero;

        Vector2 pos = new Vector2(transform.position.x, transform.position.z);
        Vector2 dir = mousePos - pos;
        if (dir.sqrMagnitude > m_targetRadius * m_targetRadius)
        {
            return;
        }

        dir.Normalize();
        eulerRot.z = Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);

        transform.eulerAngles = eulerRot;
    }

    protected MonsterBase findTarget(float radius)
    {
        if (MonsterManager.manager != null)
            return MonsterManager.manager.getClosestMonsterTo(transform.position);
        else
            return null;
    }
}

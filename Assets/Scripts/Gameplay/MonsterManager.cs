using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;



public class MonsterManager : MonoBehaviourPunCallbacks
{ 
    // Pointer to manager currently in use, we use this
    // as we want to find monsters but not through collision
    public static MonsterManager manager;

    private List<MonsterBase> m_monsters = new List<MonsterBase>();             // All monsters that currently exist
    private List<MonsterBase> m_destroyedMonsters = new List<MonsterBase>();    // All monsters that have been destroyed

    void Awake()
    {
        Assert.IsNull(manager, "Only one Monster Manager should exist at a Time!");
        manager = this;
    }

    void Update()
    {
        // TODO: Tick only on server

        // Cycle through each monster, updating them
        foreach (MonsterBase monster in m_monsters)
        {
            monster.tick(Time.deltaTime);
        }

        // Now destroy the monsters that were called to be destroyed though destroyMonster,
        // we wait till after since we cannot modify the array while we are cycling through it,
        // and some monsters might be calling destroy on themselves
        foreach (MonsterBase monster in m_destroyedMonsters)
        {
            m_monsters.Remove(monster);
            Destroy(monster.gameObject);
        }

        m_destroyedMonsters.Clear();
    }

    public MonsterBase spawnMonster(MonsterBase prefab, BoardManager board)
    {
        // TODO:
        spawnMonster_Test(prefab, board);
        return null;

    }

    // Testing Function
    public MonsterBase spawnMonster_Test(MonsterBase prefab, BoardManager board)
    {
        if (!board)
            return null;

        MonsterBase newMonster = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        if (newMonster)
            m_monsters.Add(newMonster);

        newMonster.initMoster(board);
        return newMonster;
    }

    public static void destroyMonster(MonsterBase monster)
    {
        if (manager)
            manager.destroyMonsterImpl(monster);
    }

    private void destroyMonsterImpl(MonsterBase monster)
    {
        if (monster)
            m_destroyedMonsters.Add(monster);
    }

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

    /// <summary>
    /// Get the monster that is closest to reaching the goal in given radius of position
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <param name="radius">Radius monster must be in</param>
    /// <returns>Monster with highest priority or null</returns>
    public MonsterBase getHighestPriorityMonster(Vector2 position, float radius = 1000f)
    {
        float radSqr = radius * radius;

        // The monster currently closest to reaching the goal. This just works off how
        // many tiles the monster has travelled as opposed to how close they are too the goal
        MonsterBase priorityMonster = null;
        float tilesTravelled = 0f;

        // Iterate each monster and find the closest one
        foreach (MonsterBase monster in m_monsters)
        {
            Vector2 dis = (Vector2)monster.transform.position - position;

            // Monster needs to be in radius for it to be considered
            float magSqr = dis.sqrMagnitude;
            if (magSqr <= radSqr && monster.TilesTravelled > tilesTravelled)
            {
                priorityMonster = monster;
                tilesTravelled = monster.TilesTravelled;
            }
        }

        return priorityMonster;
    }
}
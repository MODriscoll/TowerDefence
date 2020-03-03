using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

public class MonsterManager : MonoBehaviour
{
    public delegate void OnMonsterDestroyed();                  // Event for when a monster is destroyed
    public static OnMonsterDestroyed onMonsterDestroyed;        // Static event called everytime a monster is destroyed

    private List<MonsterBase> m_monsters = new List<MonsterBase>();                 // All monsters that currently exist
    private List<MonsterBase> m_destroyedMonsters = new List<MonsterBase>();        // All monsters that have been destroyed

    private bool m_tickingMonsters = false;     // If we are currently ticking all monsters

    public int NumMonsters { get { return getNumMonsters(); } }     // The number of active monsters

    public void tick(float deltaTime)
    {
        // Cycle through each monster, updating them
        m_tickingMonsters = true;
        foreach (MonsterBase monster in m_monsters)
        {
            monster.tick(deltaTime);
        }
        m_tickingMonsters = false;

        if (m_destroyedMonsters.Count > 0)
        {
            // Now destroy the monsters that were called to be destroyed though destroyMonster,
            // we wait till after since we cannot modify the array while we are cycling through it,
            // and some monsters might be calling destroy on themselves
            foreach (MonsterBase monster in m_destroyedMonsters)
            {
                m_monsters.Remove(monster);
                PhotonNetwork.Destroy(monster.gameObject);
            }

            m_destroyedMonsters.Clear();

            // This kind of lies. The comment about the event implies we
            // call it for every monster that is destroyed, in this case it
            // would be for X monsters
            if (onMonsterDestroyed != null)
                onMonsterDestroyed.Invoke();
        }
    }

    /// <summary>
    /// Spawns a monster that this manager is responsible for updating
    /// </summary>
    /// <param name="prefab">Prefab of monster to spawn</param>
    /// <param name="board">Board to place it on</param>
    /// <returns>New monster prefab or null</returns>
    public MonsterBase spawnMonster(MonsterBase prefab, BoardManager board)
    {
        return spawnMonster(prefab.name, board);
    }

    public MonsterBase spawnMonster(string prefabName, BoardManager board)
    {
        object[] spawnData = new object[1];
        spawnData[0] = PlayerController.localPlayer.playerId;
        GameObject newMonster = PhotonNetwork.Instantiate(prefabName, Vector3.zero, Quaternion.identity, 0, spawnData);
        if (!newMonster)
            return null;

        MonsterBase monster = newMonster.GetComponent<MonsterBase>();
        Assert.IsNotNull(monster);

        m_monsters.Add(monster);

        monster.initMoster(board);
        return monster;
    }

    /// <summary>
    /// Destroys a monster, removing from the board it belongs to
    /// </summary>
    /// <param name="monster">Monster to destroy</param>
    /// <param name="bImmediate">If we are able to immediately destroy the monster</param>
    public static void destroyMonster(MonsterBase monster, bool bImmediate = true)
    {
        BoardManager board = monster ? monster.Board : null;
        if (board && board.MonsterManager)
            board.MonsterManager.destroyMonsterImpl(monster, bImmediate);
    }

    /// <summary>
    /// Implementation for destroying a monster
    /// </summary>
    /// <param name="monster">Monster to destroy</param>
    /// <param name="bImmediate">If we can immediately destroy the monster</param>
    private void destroyMonsterImpl(MonsterBase monster, bool bImmediate)
    {
        if (PhotonNetwork.IsConnected && !monster.photonView.IsMine)
            return;

        if (bImmediate)
        {
            if (!m_tickingMonsters)
            {
                m_monsters.Remove(monster);
                PhotonNetwork.Destroy(monster.gameObject);

                if (onMonsterDestroyed != null)
                    onMonsterDestroyed.Invoke();

                return;
            }

            Debug.LogError("Can't immediately destroy monster as all monsters are being ticked! Placing into destroy queue");
        }

        // Only add monster once to prevent destroy being called multiple times
        // Not expecting this to be long, so we use a regular find
        if (!m_destroyedMonsters.Contains(monster))
            m_destroyedMonsters.Add(monster);
    }

    // Internal function. Adds a monster that can be queried but should not be destroyed by this client
    public void addExternalMonster(MonsterBase monster)
    {
        if (m_tickingMonsters)
        {
            Debug.LogError("Adding external monster while ticking monsters. This shouldn't happen");
            return;
        }

        m_monsters.Add(monster);
    }

    // Internal function. Removes a monster that was originally added by addExternalMonster
    public void removeExternalMonster(MonsterBase monster)
    {
        if (m_tickingMonsters)
        {
            Debug.LogError("Removing external monster while ticking monsters. This shouldn't happen");
            return;
        }

        m_monsters.Remove(monster);

        if (onMonsterDestroyed != null)
            onMonsterDestroyed.Invoke();
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
    public MonsterBase getClosestMonsterTo(Vector2 position, float radius)
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
    public MonsterBase getHighestPriorityMonster(Vector2 position, float radius)
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

    public bool getMonstersInRadius(Vector2 position, float radius, ref List<MonsterBase> monsters, HashSet<MonsterBase> ignoreMonsters = null)
    {
        float radSqr = radius * radius;

        foreach (MonsterBase monster in m_monsters)
        {
            if (ignoreMonsters.Contains(monster))
                continue;

            Vector2 dis = (Vector2)monster.transform.position - position;

            // Monster needs to be in radius for it to be considered
            float magSqr = dis.sqrMagnitude;
            if (magSqr <= radSqr)
                monsters.Add(monster);
        }

        return monsters.Count > 0;
    }

    /// <summary>
    /// Helper for getting the number of monsters still active.
    /// Destroyed monsters are considered inactive, and are not counted
    /// </summary>
    /// <returns>Number of active monsters</returns>
    private int getNumMonsters()
    {
        int numMonsters = m_monsters.Count - m_destroyedMonsters.Count;

#if UNITY_EDITOR || UNITY_STANDALONE
        bool bDebug = Application.isEditor || Debug.isDebugBuild;
        if (bDebug && numMonsters < 0)
            Debug.LogError(string.Format("Number of monsters is less than zero. Num = {0}, ({1} - {2})",
                numMonsters, m_monsters.Count, m_destroyedMonsters.Count));
#endif

        return Mathf.Max(0, numMonsters);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Photon.Pun;

/// <summary>
/// Wraps the flow field used by the board manager to establish a path. Would like this
/// to be serialized so we don't have to generate it at runtime but unity doesn't
/// support serialization of dictionaries yet
/// </summary>
public struct BoardFlowfield
{
    // Directions we can move in
    public static Vector3Int[] moveDirs = new Vector3Int[]
    {
        new Vector3Int(0, 1, 0),    // Up
        new Vector3Int(0, -1, 0),   // Down
        new Vector3Int(-1, 0, 0),   // Left
        new Vector3Int(1, 0, 0)     // Right
    };

    // Map containing the directions to move to based on a tile, the
    // goal tile will simply have its direction point to itself
    public Dictionary<Vector3Int, Vector3Int> m_flowMap;

    // The goal tile, cached so we don't have to check
    // the flow maps direction value to see if what is the goal
    public Vector3Int m_goal;

    // If this flow field has been generated
    public bool Generated { get { return m_flowMap != null; } }

    /// <summary>
    /// Generates the flow map based off a tile map. This assumes there is no
    /// cost for going to a tile (don't need that for this game)
    /// </summary>
    /// <param name="tileMap">Tile map to generate based off</param>
    public bool generate(Tilemap tileMap)
    {
        m_flowMap = null;

        if (!tileMap)
            return false;

        m_goal = getGoal(tileMap);
        if (!tileMap.HasTile(m_goal))
            return false;

        m_flowMap = new Dictionary<Vector3Int, Vector3Int>();

        HashSet<Vector3Int> visitedTiles = new HashSet<Vector3Int>();
        Queue<Vector3Int> tilesToVisit = new Queue<Vector3Int>();

        // Start from goal tile, and move to outer tiles
        tilesToVisit.Enqueue(m_goal);

        // Cycle through all tiles we need to visit
        while (tilesToVisit.Count > 0)
        {
            Vector3Int tileIndex = tilesToVisit.Dequeue();
            visitedTiles.Add(tileIndex);

            // Check which neighbors we can travel to
            Vector3Int[] neighbors = getNeighbors(tileMap, tileIndex);
            for (int i = 0; i < neighbors.Length; ++i)
            {
                // Ignore tiles we have already visited
                Vector3Int neighbor = neighbors[i];
                if (!visitedTiles.Contains(neighbor))
                {
                    m_flowMap.Add(neighbor, tileIndex);
                    tilesToVisit.Enqueue(neighbor);
                }
            }
        }

        // Assign goal to move back to itself
        m_flowMap.Add(m_goal, Vector3Int.zero);

        return true;
    }

    /// <summary>
    /// Finds the goal tile of a tile map
    /// </summary>
    /// <param name="tileMap">Tile map to query</param>
    /// <returns>Location of goal</returns>
    static private Vector3Int getGoal(Tilemap tileMap)
    {
        foreach (Vector3Int tileIndex in tileMap.cellBounds.allPositionsWithin)
        {
            TDTileBase tile = tileMap.GetTile<TDTileBase>(tileIndex);
            if (!tile)
                continue;

            if (tile.TileType == TDTileType.Goal)
                return tileIndex;
        }

        return Vector3Int.zero;
    }

    /// <summary>
    /// Get all the neighbours of a tile that monsters can travel to
    /// </summary>
    /// <param name="tileMap">Tile map to query</param>
    /// <param name="source">Tile to query for neighbors</param>
    /// <returns>Array of tiles neighbors</returns>
    static private Vector3Int[] getNeighbors(Tilemap tileMap, Vector3Int source)
    {
#if UNITY_EDITOR
        Assert.IsTrue(tileMap.HasTile(source));
#endif

        List<Vector3Int> neighbors = new List<Vector3Int>();
        for (int i = 0; i < moveDirs.Length; ++i)
        {
            Vector3Int tileIndex = source + moveDirs[i];
            if (!tileMap.HasTile(tileIndex))
                continue;

            TDTileBase tile = tileMap.GetTile<TDTileBase>(tileIndex);
            if (!canTravelTo(tile))
                continue;

            neighbors.Add(tileIndex);
        }

        return neighbors.ToArray();
    }

    /// <summary>
    /// Get if monsters could travel to this tile
    /// </summary>
    /// <param name="tile">Tile to check</param>
    /// <returns>If monster can travel to tile</returns>
    static private bool canTravelTo(TDTileBase tile)
    {
        if (tile)
        {
            // We can travel to spawn and path tiles
            TDTileType tileType = tile.TileType;
            return tileType == TDTileType.Path || tileType == TDTileType.Spawn;
        }

        return false;
    }
}

/// <summary>
/// Manager for a single board in the world
/// </summary>
public class BoardManager : MonoBehaviourPun
{
    public Tilemap m_tileMap;       // Tilemap of board
    private Grid m_grid;            // Grid of board

    [SerializeField] private MonsterManager m_monsterManager;       // This boards monster manager

    [Header("Board Details")]
    [SerializeField] private BoardFlowfield m_flowField = new BoardFlowfield();     // Flow field used for path following
    [SerializeField] private Vector3Int[] m_spawnTiles = new Vector3Int[0];         // Spawn tiles used for faster spawning of monsters

    private Dictionary<Vector3Int, TowerBase> m_placedTowers = new Dictionary<Vector3Int, TowerBase>();     // Index to tile map, used for checking if tile exists
    private List<TowerBase> m_allTowers = new List<TowerBase>();                                            // All towers that have been placed on the map

    [SerializeField] private Transform m_cameraView;        // The position to place the camera when viewing this board

    public Vector3 ViewPosition { get { return m_cameraView ? m_cameraView.position : Vector3.zero; } }     // The position for viewing this board

    public MonsterManager MonsterManager { get { return m_monsterManager; } }

    void Awake()
    {
        if (!m_tileMap)
            m_tileMap = GetComponentInChildren<Tilemap>();

        // Need to generate this now since flow field isn't serialized
        if (m_tileMap)
            if (!m_flowField.generate(m_tileMap))
                Debug.LogError("Failed to Generate Flow Field!");
    }

    public Vector3 indexToPosition(Vector3Int tileIndex)
    {
        if (m_tileMap)
            return m_tileMap.GetCellCenterWorld(tileIndex);
        else
            return Vector3.zero;
    }

    public Vector3Int positionToIndex(Vector3 position)
    {
        if (m_tileMap)
            return m_tileMap.WorldToCell(position);
        else
            return Vector3Int.zero;
    }

    public bool isGoalTile(Vector3Int tileIndex)
    {
        return m_flowField.m_goal == tileIndex;
    }

    public bool isPlaceableTile(Vector3Int tileIndex)
    {
        if (m_tileMap)
        {
            TDTileBase tile = m_tileMap.GetTile<TDTileBase>(tileIndex);
            if (tile)
                return tile.TileType == TDTileType.Placeable;
        }

        return false;
    }

    public Vector3Int getRandomSpawnTile()
    {
        if (m_tileMap)
            if (m_spawnTiles.Length > 0)
            {
                Vector3Int tileIndex = m_spawnTiles[Random.Range(0, m_spawnTiles.Length)];
                return tileIndex;
            }

        return Vector3Int.zero;
    }

    public Vector3Int getNextPathTile(Vector3Int tileIndex)
    {
#if UNITY_EDITOR
        if (!m_flowField.Generated)
        {
            Debug.LogError("getNextPathTile called with no flow field generated");
            return Vector3Int.zero;
        }

        if (!m_flowField.m_flowMap.ContainsKey(tileIndex))
        {
            Debug.LogError("getNextPathTile called with invalid tile index");
            return Vector3Int.zero;
        }
#endif

        return m_flowField.m_flowMap[tileIndex];
    }

    public void placeTower(TowerBase tower, Vector3Int tileIndex)
    {
#if UNITY_EDITOR
        if (isOccupied(tileIndex))
        {
            Debug.LogWarning("Trying to place tower on tile that already has a tower!");
            return;
        }
#endif

        m_allTowers.Add(tower);
        m_placedTowers.Add(tileIndex, tower);
    }

    public void removeTower(TowerBase tower)
    {
        // TODO: Optimize

        var it = m_placedTowers.GetEnumerator();
        while (it.MoveNext())
        {
            if (it.Current.Value == tower)
            {
                m_allTowers.Remove(tower);
                m_placedTowers.Remove(it.Current.Key);
                return;
            }
        }
    }

    public bool isOccupied(Vector3Int tileIndex)
    {
        return m_placedTowers.ContainsKey(tileIndex);
    }

    public TowerBase getClosestTowerTo(Vector2 position, float radius)
    {
        // TODO: This is literal copy/past of MonsterManager.getClosestMonsterTo (make a helper function instead)

        float radSqr = radius * radius;

        // The current closest tower and its distance from position (squared)
        TowerBase closest = null;
        float closestDis = float.MaxValue;

        // Iterate each monster and find the closest one
        foreach (TowerBase tower in m_allTowers)
        {
            Vector2 dis = (Vector2)tower.transform.position - position;

            // Tower needs to be in radius for it to be considered
            float magSqr = dis.sqrMagnitude;
            if (magSqr <= radSqr && magSqr < closestDis)
            {
                closest = tower;
                closestDis = magSqr;
            }
        }

        return closest;
    }

    /// <summary>
    /// Helper for spawning a monster selected by passed in player. This
    /// is used by the PlayerController
    /// </summary>
    /// <param name="prefabName">Name of prefab</param>
    /// <param name="spawningPlayer">Player that is spawning in the monster</param>
    public void spawnMonster(string prefabName, Photon.Realtime.Player spawningPlayer)
    {
        photonView.RPC("spawnMonsterRPC", PhotonHelpers.getFirstPlayerThatIsnt(spawningPlayer), prefabName);
    }

    // Spawns a user selected monster
    [PunRPC]
    private void spawnMonsterRPC(string prefabName)
    {
        if (MonsterManager)
            MonsterManager.spawnMonster(prefabName, this);
    }

#if UNITY_EDITOR
    public void refreshProperties()
    {
        if (!m_tileMap)
            m_tileMap = GetComponent<Tilemap>();

        // Compress bounds so we work with latest size
        m_tileMap.CompressBounds();

        // Generate the flow field
        if (!m_flowField.generate(m_tileMap))
            Debug.LogWarning("Failed to generate the flow field! PathFollowing will not work correctly!");
        
        // Tiles we need to find and cache for use in game
        List<Vector3Int> spawnTiles = new List<Vector3Int>();

        foreach (Vector3Int tileIndex in m_tileMap.cellBounds.allPositionsWithin)
        {
            TDTileBase tile = m_tileMap.GetTile<TDTileBase>(tileIndex);
            if (!tile)
                continue;

            if (tile.TileType == TDTileType.Spawn)
                spawnTiles.Add(tileIndex);
        }

        // Cache information
        m_spawnTiles = spawnTiles.ToArray();
    }
#endif

    void OnDrawGizmos()
    {
        if (!m_tileMap)
            return;

        if (m_flowField.Generated)
        {
            foreach (var entry in m_flowField.m_flowMap)
            {
                if (entry.Key == m_flowField.m_goal)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(m_tileMap.GetCellCenterWorld(m_flowField.m_goal), m_tileMap.cellSize * 0.5f);
                }
                else
                {
                    Vector3 entryPos = m_tileMap.GetCellCenterWorld(entry.Key);
                    Vector3 goalPos = m_tileMap.GetCellCenterWorld(entry.Value);
              
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(entryPos, goalPos);

                    // Offset for placing the 'arrow' at the edge of the tile
                    Vector3 offset = m_tileMap.cellSize * 0.5f;
                    offset = m_tileMap.orientationMatrix.MultiplyVector(offset);

                    // The direction we move in, we use this to act as arrows basically
                    Vector3 moveDir = entry.Value - entry.Key;
                    moveDir.Scale(offset);

                    Gizmos.color = Color.red;
                    Gizmos.DrawCube(entryPos + moveDir, m_tileMap.cellSize * 0.1f);
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BoardManager))]
public class BoardManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BoardManager boardManager = serializedObject.targetObject as BoardManager;

        if (GUILayout.Button("Refresh Board"))
            boardManager.refreshProperties();
    }
}
#endif

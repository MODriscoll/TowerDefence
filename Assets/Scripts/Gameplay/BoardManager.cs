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
/// Wraps the flow field used by the board manager to establish multiple paths. While
/// the flow field isn't saved (since dictionaries can't be serialized), the individual paths
/// are stored instead and can be accessed by index
/// </summary>
[System.Serializable]
public struct BoardPaths
{
    // Directions we can move in
    public static Vector3Int[] moveDirs = new Vector3Int[]
    {
        new Vector3Int(0, 1, 0),    // Up
        new Vector3Int(0, -1, 0),   // Down
        new Vector3Int(-1, 0, 0),   // Left
        new Vector3Int(1, 0, 0)     // Right
    };

    // Wrapper around a list, so we can serialize the path
    [System.Serializable]
    private struct Path
    {
        // List of tiles needed to be able to reach goal tile.
        // This is ordered from spawn tile first to goal tile last
        public List<Vector3Int> m_list;
    }

    // The goal tile, cached so we don't have to check
    // the flow maps direction value to see if what is the goal
    [SerializeField, HideInInspector]
    private Vector3Int m_goal;

    [SerializeField, HideInInspector] 
    private List<Path> m_paths;

    // If the paths have been generated
    public bool Generated { get { return m_paths != null; } }

    // The goal tile of each path
    public Vector3Int GoalTile { get { return m_goal; } }

    // The number of paths that can be taken
    public int NumPaths { get { return Generated ? m_paths.Count : 0; } }

    /// <summary>
    /// Generates the flow map based off a tile map. This assumes there is no
    /// cost for going to a tile (don't need that for this game)
    /// </summary>
    /// <param name="tileMap">Tile map to generate based off</param>
    public bool generate(Tilemap tileMap)
    {
        m_paths = null;

        if (!tileMap)
            return false;

        m_goal = getGoal(tileMap);
        if (!tileMap.HasTile(m_goal))
            return false;

        m_paths = new List<Path>();

        // Map containing the directions to move to based on a tile. We generate
        // the flow field first which we then use to generate individual paths
        Dictionary<Vector3Int, Vector3Int> flowField = new Dictionary<Vector3Int, Vector3Int>();

        HashSet<Vector3Int> visitedTiles = new HashSet<Vector3Int>();
        Queue<Vector3Int> tilesToVisit = new Queue<Vector3Int>();

        // Used later to start creating individual paths
        List<Vector3Int> spawnTiles = new List<Vector3Int>();

        // Start from goal tile, and move to outer tiles
        tilesToVisit.Enqueue(m_goal);

        // Cycle through all tiles we need to visit
        while (tilesToVisit.Count > 0)
        {
            Vector3Int tileIndex = tilesToVisit.Dequeue();
            visitedTiles.Add(tileIndex);

            // We can use this tile later to generate the individual paths
            if (isTileOfType(tileMap, tileIndex, TDTileType.Spawn))
                spawnTiles.Add(tileIndex);

            // Check which neighbors we can travel to
            Vector3Int[] neighbors = getNeighbors(tileMap, tileIndex);
            for (int i = 0; i < neighbors.Length; ++i)
            {
                // Ignore tiles we have already visited
                Vector3Int neighbor = neighbors[i];
                if (!visitedTiles.Contains(neighbor))
                {
                    flowField.Add(neighbor, tileIndex);
                    tilesToVisit.Enqueue(neighbor);
                }
            }
        }

        // Using flow field, generate each path for each spawn point
        foreach (Vector3Int spawnTile in spawnTiles)
        {
            List<Vector3Int> pathList = new List<Vector3Int>();
            pathList.Add(spawnTile);

            // This flowfield map will not contain a key for the goal tile,
            // once we reach the goal tile, we will exit out
            Vector3Int curTile = spawnTile;
            while (flowField.ContainsKey(curTile))
            {
                curTile = flowField[curTile];
                pathList.Add(curTile);
            }

            Path path = new Path();
            path.m_list = pathList;
            m_paths.Add(path);
        }    

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
            TDTileType tileType = tile.TileType;
            return tileType == TDTileType.Path || tileType == TDTileType.Spawn;
        }

        return false;
    }

    /// <summary>
    /// Checks if tile at index is of given tile type
    /// </summary>
    /// <param name="tileMap">Tile map to query</param>
    /// <param name="tileIndex">Index of tile to check</param>
    /// <param name="type">Type the tile should be</param>
    /// <returns>If tile is of type</returns>
    static private bool isTileOfType(Tilemap tileMap, Vector3Int tileIndex, TDTileType type)
    {
#if UNITY_EDITOR
        Assert.IsTrue(tileMap.HasTile(tileIndex));
#endif

        TDTileBase tile = tileMap.GetTile<TDTileBase>(tileIndex);
        if (tile)
            return tile.TileType == type;

        return false;
    }

    /// <summary>
    /// Gets an index to a random path that can be used to follow
    /// </summary>
    /// <returns>Index >= 0 or -1 if not generated</returns>
    public int getRandomPathIndex()
    {
        if (Generated)
            return Random.Range(0, m_paths.Count);
        else
            return -1;
    }

    /// <summary>
    /// Gets path at given index
    /// </summary>
    /// <param name="index">Index of path to retrieve</param>
    /// <returns>Valid path or null</returns>
    public List<Vector3Int> getPath(int index)
    {
        if (m_paths != null)
            if (index >= 0 && index < m_paths.Count)
                return m_paths[index].m_list;

        return null;
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
    [SerializeField] private BoardPaths m_paths = new BoardPaths();             // Paths the monsters can possibly follow

    private Dictionary<Vector3Int, TowerBase> m_placedTowers = new Dictionary<Vector3Int, TowerBase>();     // Index to tile map, used for checking if tile exists
    private List<TowerBase> m_allTowers = new List<TowerBase>();                                            // All towers that have been placed on the map

    [SerializeField] private Transform m_cameraView;        // The position to place the camera when viewing this board

    public Vector3 ViewPosition { get { return m_cameraView ? m_cameraView.position : Vector3.zero; } }     // The position for viewing this board

    public MonsterManager MonsterManager { get { return m_monsterManager; } }

    void Awake()
    {
        if (!m_tileMap)
            m_tileMap = GetComponentInChildren<Tilemap>();

        // Generate paths even if waven't done so already
        if (m_tileMap)
            if (!m_paths.Generated && !m_paths.generate(m_tileMap))
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
        return m_paths.GoalTile == tileIndex;
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

    public bool isValidTile(Vector3Int tileIndex)
    {
        if (m_tileMap)
            return m_tileMap.cellBounds.Contains(tileIndex);

        return false;
    }

    public int getRandomPathToFollow()
    {
        return m_paths.getRandomPathIndex();
    }
    
    public Vector3 pathProgressToPosition(int pathIndex, float progress, out bool atGoalTile)
    {
        atGoalTile = false;

#if UNITY_EDITOR
        if (!m_paths.Generated)
        {
            Debug.LogError("pathProgressToPosition called with no paths generated");
            return Vector3.zero;
        }
#endif

        List<Vector3Int> path = m_paths.getPath(pathIndex);
        if (path == null || path.Count == 0)
        {
            Debug.LogError(string.Format("Path does not exist (or is empty). Board Name: {0}, Path Index: {1}",
                gameObject.ToString(), pathIndex));
            return Vector3.zero;
        }

        // Check if goal tile
        int curIndex = Mathf.FloorToInt(progress);
        if (curIndex >= (path.Count - 1))
        {
            atGoalTile = true;
            return indexToPosition(m_paths.GoalTile);
        }

        int nextIndex = Mathf.CeilToInt(progress);

        Vector3 curTilePos = indexToPosition(path[curIndex]);
        Vector3 nextTilePos = indexToPosition(path[nextIndex]);

        return Vector3.Lerp(curTilePos, nextTilePos, progress - curIndex);
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

    public TowerBase getTowerOnTile(Vector3Int tileIndex)
    {
        TowerBase tower = null;
        m_placedTowers.TryGetValue(tileIndex, out tower);
        return tower;
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

    public bool getTowersInRange(Vector2 position, float radius, ref List<TowerBase> towers)
    {
        // TODO: This is literal copy/past of getClosestTowerTo (make a helper function instead)

        float radSqr = radius * radius;

        // Iterate each monster and find the closest one
        foreach (TowerBase tower in m_allTowers)
        {
            Vector2 dis = (Vector2)tower.transform.position - position;

            // Tower needs to be in radius for it to be considered
            float magSqr = dis.sqrMagnitude;
            if (magSqr <= radSqr)
                towers.Add(tower);
        }

        return towers.Count > 0;
    }

    /// <summary>
    /// Helper for spawning a monster selected by passed in player. This
    /// is used by the PlayerController
    /// </summary>
    /// <param name="prefabName">Name of prefab</param>
    /// <param name="spawningPlayer">Player that is spawning in the monster</param>
    public void spawnMonster(string prefabName, Photon.Realtime.Player spawningPlayer)
    {
        if (PhotonNetwork.IsConnected)
            photonView.RPC("spawnMonsterRPC", PhotonHelpers.getFirstPlayerThatIsnt(spawningPlayer), prefabName);
        else
            spawnMonsterRPC(prefabName);
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

        // Generate the paths
        if (!m_paths.generate(m_tileMap))
            Debug.LogWarning("Failed to generate the paths for Board! PathFollowing will not work correctly!");
    }
#endif

    void OnDrawGizmos()
    {
        if (!m_tileMap)
            return;

        //foreach (var entry in m_flowField.m_flowMap)
        for (int i = 0; i < m_paths.NumPaths; ++i)
        {
            List<Vector3Int> path = m_paths.getPath(i);
            for (int j = 0; j < path.Count; ++j)
            {
                Vector3Int curTile = path[j];

                // Is this the goal tile (last tile in the list)
                if ((j + 1) >= path.Count)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(m_tileMap.GetCellCenterWorld(m_paths.GoalTile), m_tileMap.cellSize * 0.5f);
                }
                else
                {
                    Vector3Int nextTile = path[j + 1];

                    Vector3 entryPos = m_tileMap.GetCellCenterWorld(curTile);
                    Vector3 goalPos = m_tileMap.GetCellCenterWorld(nextTile);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(entryPos, goalPos);

                    // Offset for placing the 'arrow' at the edge of the tile
                    Vector3 offset = m_tileMap.cellSize * 0.5f;
                    offset = m_tileMap.orientationMatrix.MultiplyVector(offset);

                    // The direction we move in, we use this to act as arrows basically
                    Vector3 moveDir = nextTile - curTile;
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

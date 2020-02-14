using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manager for a single board in the world
/// </summary>
public class BoardManager : MonoBehaviour
{
    public Tilemap m_tilemap;       // Tilemap of board
    private Grid m_grid;            // Grid of board

    public Vector3Int[] m_spawnTiles = new Vector3Int[0];   // Cached spawn tiles to speed up spawning

    void Awake()
    {
        if (!m_tilemap)
            m_tilemap = GetComponent<Tilemap>();
    }

    public Vector3 getRandomSpawnTile()
    {
        if (m_tilemap)
            if (m_spawnTiles.Length > 0)
            {
                Vector3Int tileIndex = m_spawnTiles[Random.Range(0, m_spawnTiles.Length)];
                return m_tilemap.CellToWorld(tileIndex);
            }

        return Vector3.zero;
    }

#if UNITY_EDITOR
    public void refreshProperties()
    {
        if (!m_tilemap)
            m_tilemap = GetComponent<Tilemap>();

        // Compress bounds so we work with latest size
        m_tilemap.CompressBounds();
        
        // Tiles we need to find and cache for use in game
        List<Vector3Int> spawnTiles = new List<Vector3Int>();

        foreach (Vector3Int tileIndex in m_tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tt = m_tilemap.GetTile(tileIndex);
            TDTileBase tile = m_tilemap.GetTile<TDTileBase>(tileIndex);
            if (!tile)
                continue;

            if (tile.TileType == ETDTileType.Spawn)
                spawnTiles.Add(tileIndex);
        }

        // Cache information
        m_spawnTiles = spawnTiles.ToArray();
    }
#endif
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

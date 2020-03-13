using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TDTileType
{
    Cosmetic,       // Tile is purely cosmetic and is not used for anything
    Placeable,      // Towers can be placed on this tile
    Path,           // Monsters can travel to this tile
    Spawn,          // Monsters can be spawned on this tile
    Goal            // The goal tile for monsters to reach
}

[CreateAssetMenu(fileName = "TDTileBase", menuName = "Tower Defence Tile")]
public class TDTileBase : Tile
{
    [SerializeField] private TDTileType m_tileType;    // This tiles type

    public TDTileType TileType { get { return m_tileType; } }
}

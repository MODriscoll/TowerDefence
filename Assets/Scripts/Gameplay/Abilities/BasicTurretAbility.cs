using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This ability will disable an opponents turret for X seconds
public class BasicTurretAbility : AbilityBase
{
    // Begin AbilityBase Interface
    public override bool canUseAbilityHere(PlayerController controller, BoardManager board, Vector3 worldPos, Vector3Int tileIndex)
    {
        return board.getTowerOnTile(tileIndex) != null;
    }
}

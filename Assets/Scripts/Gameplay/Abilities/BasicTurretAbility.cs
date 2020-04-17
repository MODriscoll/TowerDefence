using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicTurretAbility : AbilityBase
{
    // Begin AbilityBase Interface
    public override bool canUseAbilityHere(PlayerController controller, BoardManager board, Vector3 worldPos, Vector3Int tileIndex)
    {
        return board.getTowerOnTile(tileIndex) != null;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthTurretAbility : AbilityBase
{
    // Begin AbilityBase Interface
    public virtual bool canUseAbilityHere(PlayerController controller, BoardManager board, Vector3 worldPos, Vector3Int tileIndex)
    {
        return true;
    }
}

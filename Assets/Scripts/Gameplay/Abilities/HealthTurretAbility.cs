using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthTurretAbility : AbilityBase
{
    // Begin AbilityBase Interface
    public virtual bool canUseAbilityHere(PlayerController controller, BoardManager board, Vector3 worldPos, Vector3Int tileIndex)
    {
        MonsterManager monsterManager = board.MonsterManager;
        if (!monsterManager)
            return false;

        MonsterBase monster = monsterManager.getHoveredMonster((Vector2)worldPos);
        if (!monster)
            return false;

        // We only want to use this ability on monsters that have been damaged
        return !monster.AtMaxHealth;
    }
}

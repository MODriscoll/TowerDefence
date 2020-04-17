using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MortarTurretAbilityAction : AbilityActionBase
{
    [SerializeField] private float m_removeDuration = 0.25f;                // Seconds to cut down travel duration by in monsters
    [SerializeField] private float m_radius = 1.5f;                         // Radius of abilities effect
    [SerializeField, Min(0.1f)] private float m_duration = 3f;              // Duration of this ability
    [SerializeField, Min(0.05f)] private float m_checkInterval = 0.1f;      // How often to check for overlapping mice

    private HashSet<MonsterBase> m_monstersInRange = new HashSet<MonsterBase>();        // All monsters currently in range

    // Begin AbilityActionBase Interface
    protected override void startAbilityActionImpl()
    {
        StartCoroutine(collisionRoutine());
    }

    private void updateCollision(bool doCheck = true)
    {
        MonsterManager monsterManager = m_board.MonsterManager;
        if (!monsterManager)
            return;

        List<MonsterBase> monsters = null;
        if (doCheck)
            monsters = monsterManager.getMonstersInRadius(m_position, m_radius);

        if (monsters != null)
        {
            // We will remove any monsters that are still in the range of this effect
            HashSet<MonsterBase> monstersOutOfRange = new HashSet<MonsterBase>(m_monstersInRange);

            foreach (MonsterBase monster in monsters)
            {
                // Monster just entered range, provide it the boost
                if (!m_monstersInRange.Contains(monster))
                    giveBoost(monster);

                monstersOutOfRange.Remove(monster);                
            }

            // Remove the boost from any monsters that are no longer in range
            foreach (MonsterBase monster in monstersOutOfRange)
                removeBoost(monster);
        }
        else
        {
            // We remove duration directly as we don't want to remove monsters from the list just yet
            foreach (MonsterBase monster in m_monstersInRange)
                if (monster)
                    monster.addTravelDuration(m_removeDuration);

            m_monstersInRange.Clear();
        }
    }

    private void giveBoost(MonsterBase monster)
    {
        m_monstersInRange.Add(monster);

        // Should be valid, check to be sure
        if (monster)
            monster.addTravelDuration(-m_removeDuration);
    }

    private void removeBoost(MonsterBase monster)
    {
        m_monstersInRange.Remove(monster);

        // Could possibly be null
        if (monster)
            monster.addTravelDuration(m_removeDuration);
    }

    private IEnumerator collisionRoutine()
    {
        float effectTime = Time.time + m_duration;
        while (Time.time < effectTime)
        {
            updateCollision();
            yield return new WaitForSeconds(m_checkInterval);
        }

        // This will remove the boost from all monsters
        // we had detected as being overlapped with
        updateCollision(false);

        finishAbilityAction();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, m_radius);
    }
}

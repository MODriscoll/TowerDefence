using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HealthTurret : TowerScript
{
    [SerializeField] private int m_healPower = 3;                   // How much this towers heals
    [SerializeField] private float m_range = 2.5f;                  // Amount of tiles this tower reaches
    [SerializeField] private bool m_healSingleTower = true;         // If only a single tower should be healed

    // TowerScript Interface
    protected override bool shouldPerformAction(MonsterBase target)
    {
        return true;
    }

    protected override void performAction(MonsterBase target)
    {
        BoardManager board = m_tower.Board;
        if (board == null)
            return;

        List<TowerBase> towers = new List<TowerBase>();
        if (board.getTowersInRange(transform.position, m_range, ref towers))
        {
            if (m_healSingleTower)
            {
                int index = Random.Range(0, towers.Count);
                towers[index].healTower(m_healPower);
            }
            else
            {
                foreach (TowerBase tower in towers)
                    tower.healTower(m_healPower);
            }
        }

        // Call this regardless
#if UNITY_EDITOR
        onHealTowers();
#else
        photonView.RPC("onHealTowers", RpcTarget.All);
#endif
    }

    [PunRPC]
    private void onHealTowers()
    {

    }
}

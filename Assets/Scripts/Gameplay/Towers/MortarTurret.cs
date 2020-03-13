using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

// Script for the mortar turret a player can place
public class MortarTurret : TowerScript
{
    [SerializeField] private TowerProjectile m_projectilePrefab;        // The projectile we shoot

    // TowerScript Interface
    protected override void performAction(MonsterBase target)
    {
        if (!target)
        {
            Debug.LogError("MortarTurret script is expecting target to be valid");
            return;
        }

        if (!m_projectilePrefab)
            return;

        GameObject projectileObject = PhotonNetwork.Instantiate(m_projectilePrefab.name, transform.position, Quaternion.identity);
        if (!projectileObject)
            return;

        TowerProjectile projectile = projectileObject.GetComponent<TowerProjectile>();
        Assert.IsNotNull(projectile);

        // For some reason, passing transform.rotation to Instantiate doesn't
        // actually update the rotation, so we need to manually do it ourselves
        // TODO: quick way (optimize)
        Vector3 moveDir = (target.transform.position - transform.position).normalized;

        projectile.initProjectile(moveDir, m_tower.Board, this);
    }
}

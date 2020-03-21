using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

// Script for the mortar turret a player can place
public class MortarTurret : TowerScript
{
    // The projectile we shoot
    [PhotonPrefab(typeof(MortarProjectile))]
    [SerializeField] private string m_projectilePrefab;

    [SerializeField] private Transform m_shootFrom;         // Transform which we shoot from

    // TowerScript Interface
    protected override void performAction(MonsterBase target)
    {
        if (!target)
        {
            Debug.LogError("MortarTurret script is expecting target to be valid");
            return;
        }

        if (string.IsNullOrEmpty(m_projectilePrefab))
            return;

        Transform spawnTransform = m_shootFrom ? m_shootFrom : transform;
        Vector3 eulerAngles = m_shootFrom.eulerAngles;

        // Since game is 2D, we only need one axis of rotation
        object[] spawnData = new object[2];
        spawnData[0] = GameManager.manager.getPlayerIdFromBoard(Board);
        spawnData[1] = eulerAngles.z;

        GameObject projectileObject = PhotonNetwork.Instantiate(m_projectilePrefab, m_shootFrom.position, Quaternion.identity, 0, spawnData);
        if (!projectileObject)
            return;

        MortarProjectile projectile = projectileObject.GetComponent<MortarProjectile>();
        Assert.IsNotNull(projectile);

        projectile.initProjectile(spawnTransform.eulerAngles, m_tower.Board, this);
    }
}

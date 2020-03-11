using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Script for the basic turret a player can place
public class BasicTurret : TowerScript
{
    [SerializeField] private int m_damage = 3;      // How much damage we do

    [Header("Aesthetics")]
    [SerializeField] private AudioClip m_shootSound;    // Shooting sound effect

    // TowerScript Interface
    protected override void performAction(MonsterBase target)
    {
        if (!target)
        {
            Debug.LogError("BasicTurret script is expecting target to be valid");
            return;
        }

        Vector2 targetPosition = target.transform.position;

        // Shoot the laser
        target.takeDamage(m_damage);

#if UNITY_EDITOR
        onFired(targetPosition);
#else
        photonView.RPC("onFired", RpcTarget.All, targetPosition);
#endif
    }

    [PunRPC]
    private void onFired(Vector2 targetPos)
    {
        SoundEffectsManager.playSoundEffect(m_shootSound);
    }
}

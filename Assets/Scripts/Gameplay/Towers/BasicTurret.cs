using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// Script for the basic turret a player can place
public class BasicTurret : TowerScript
{
    [SerializeField] private int m_damage = 3;      // How much damage we do

    [Header("Aesthetics")]
    [SerializeField] private BasicTurretEffect m_effectPrefab;          // Effect to instantiate when shooting
    [SerializeField] private AudioClip m_shootSound;                    // Shooting sound effect

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
        bool bKilled = target.takeDamage(m_damage, this);

        if (PhotonNetwork.IsConnected)
            photonView.RPC("onFired", RpcTarget.All, targetPosition, bKilled);
        else
            onFired(targetPosition, bKilled);
    }

    [PunRPC]
    private void onFired(Vector2 targetPos, bool killed)
    {
        if (m_effectPrefab)
            Instantiate(m_effectPrefab, new Vector3(targetPos.x, targetPos.y, -0.02f), Quaternion.Euler(0f, 0f, Random.Range(0, 360.0f)));    //value by z is -0.02f to make the prefab seen before the ground. - Ivan K.

        SoundEffectsManager.playSoundEffect(m_shootSound, Board);
    }
}

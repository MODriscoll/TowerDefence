using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HealthTurretAbilityAction : AbilityActionBase
{
    [SerializeField] private int m_healthToGive = 5;                // How much health to give each burst
    [SerializeField, Min(0)] private int m_bursts = 3;              // How many bursts of health to give
    [SerializeField] private float m_interval = 1f;                 // Interval between each burst

    private MonsterBase m_monsterToHeal = null;             // Monster we can heal

    public AudioClip m_healSound;           // Sound to play when healing

    // Begin AbilityActionBase Interface
    protected override void startAbilityActionImpl()
    {
        m_monsterToHeal = m_board.MonsterManager.getHoveredMonster((Vector2)m_position);
        if (m_monsterToHeal)
            StartCoroutine(healRoutine());
        else
            finishAbilityAction();
    }

    private IEnumerator healRoutine()
    {
        // Make sure we have valid interval
        float interval = m_interval;
        if (interval <= 0f)
            interval = 1f;

        int burstNum = 0;

        // Make sure we check if monster is still valid. It can possibly
        // be killed or reach the end of the goal before we finish
        while (burstNum < m_bursts && m_monsterToHeal)
        {
            PhotonView view = m_monsterToHeal.photonView;
            if (!view)
            {
                finishAbilityAction();
                yield break;
            }

            // This function will handle healing the monster but on the other players client.
            // We don't technically own this monster and only the owner can heal the monster
            // We send this event to all as we can use it to play aesthetics as well
            if (PhotonNetwork.IsConnected)
                photonView.RPC("onHealMonsterRPC", RpcTarget.All, view.ViewID, m_healthToGive);
            else
                onHealMonsterRPC(view.ViewID, m_healthToGive);

            ++burstNum;
            yield return new WaitForSeconds(m_interval);       
        }

        finishAbilityAction();
    }

    [PunRPC]
    private void onHealMonsterRPC(int photonId, int healthToGive)
    {
        MonsterBase monster = getMonsterFromPhotonId(photonId);
        if (!monster)
            return;

        if (!PhotonNetwork.IsConnected || monster.photonView.IsMine)
            monster.healMonster(healthToGive);

        // Play the heal sound
        SoundEffectsManager.playSoundEffect(m_healSound, m_board);
    }

    private MonsterBase getMonsterFromPhotonId(int photonId)
    {
        PhotonView view = PhotonNetwork.GetPhotonView(photonId);
        if (!view)
            return null;

        return view.GetComponent<MonsterBase>();
    }
}

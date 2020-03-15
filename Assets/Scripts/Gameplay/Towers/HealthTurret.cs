using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HealthTurret : TowerScript
{
    [SerializeField] private int m_healPower = 3;                   // How much this towers heals
    [SerializeField] private float m_range = 2.5f;                  // Amount of tiles this tower reaches
    [SerializeField] private bool m_healSingleTower = true;         // If only a single tower should be healed

    [SerializeField] private GameObject m_healPulseSource;              // Game object thta we scale for healing effect
    [SerializeField] private Vector3 m_pulseScale = Vector3.one;        // What to scale the pulse to
    [SerializeField, Min(0.1f)] private float m_pulseDuration = 0.5f;   // How long the pulse lasts for

    private Coroutine m_pulseRoutine = null;            // Pulse routine that is currently active

    void Start()
    {
        // TowerScript is set up right now to allow towers to fire upon
        // being spawned in. We don't want this for the healing turret though
        if (PhotonNetwork.IsConnected && photonView.IsMine)
            m_lastFireTime = Time.time + m_fireRate; 
    }

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
        if (PhotonNetwork.IsConnected)
            photonView.RPC("onHealTowers", RpcTarget.All);
        else
            onHealTowers();
    }

    [PunRPC]
    private void onHealTowers()
    {
        if (!m_healPulseSource)
            return;

        if (m_pulseRoutine != null)
            StopCoroutine(m_pulseRoutine);

        m_pulseRoutine = StartCoroutine(pulseRoutine());
    }

    private IEnumerator pulseRoutine()
    {
        m_healPulseSource.SetActive(true);

        Transform pulseTransform = m_healPulseSource.transform;
        Vector3 startScale = pulseTransform.localScale;

        float startTime = Time.time;
        float endTime = startTime + m_pulseDuration;
        while (isActiveAndEnabled && Time.time <= endTime)
        {
            float alpha = Mathf.Clamp01((Time.time - startTime) / m_pulseDuration);
            Vector3 curScale = Vector3.Lerp(startScale, m_pulseScale, alpha);

            pulseTransform.localScale = curScale;

            yield return null;
        }

        pulseTransform.localScale = startScale;
        m_healPulseSource.SetActive(false);

        m_pulseRoutine = null;
    }
}

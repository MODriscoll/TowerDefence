using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TestMonsterSpawner : MonoBehaviourPun
{
    public WaveSpawner m_spawner;             // Spawner to test with
    public float m_waveDelay = 1f;            // Interval between waves

    private int m_waveNum = -1;
    private bool m_active = false;

    void Start()
    {
        if (m_spawner)
            m_spawner.onWaveFinished += onWaveFinished;
    }

    void Update()
    {
        if (m_active)
            return;

        // Only start spawning once both players are in the game (only master client starts this)
#if !UNITY_EDITOR
        bool bStartNow = false;
        if (PhotonNetwork.IsConnected && PhotonNetwork.PlayerList.Length >= 2)
            if (PhotonNetwork.IsMasterClient)
                bStartNow = true;

        if (bStartNow)
#endif
        {
            m_active = true;

            // Calling this as it provides the initial delay
            onWaveFinished();
        }
    }

    private void startNextWave()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
            return;

        ++m_waveNum;
#if UNITY_EDITOR
        startWave(m_waveNum);
#else
        photonView.RPC("startWave", RpcTarget.All, m_waveNum);
#endif
    }

    private void onWaveFinished()
    {
        Invoke("startNextWave", m_waveDelay);
    }

    [PunRPC]
    private void startWave(int waveId)
    {
        m_active = true;
        m_spawner.initWave(waveId);
    }
}

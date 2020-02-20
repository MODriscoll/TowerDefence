using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMonsterSpawner : MonoBehaviour
{
    public WaveSpawner m_spawner;             // Spawner to test with
    public float m_waveDelay = 1f;            // Interval between waves

    private int m_waveNum = -1;

    void Start()
    {
        if (m_spawner)
        {
            m_spawner.onWaveFinished += onWaveFinished;

            // Calling this as it provides the initial delay
            onWaveFinished();
        }
    }

    private void startNextWave()
    {
        ++m_waveNum;

        if (m_spawner)
            m_spawner.initWave(m_waveNum);
    }

    private void onWaveFinished()
    {
        Invoke("startNextWave", m_waveDelay);
    }
}

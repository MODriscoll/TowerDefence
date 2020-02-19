using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[SerializeField]
public struct WaveSpawnInfo
{
    public int m_count;             // Amount of common monsters to spawn
    public int m_spawnInterval;     // Interval for spawning a new monster
}


// Responsible for spawning a wave of monsters for both players. This component
// is not networked, and is controlled by the master client
public class WaveSpawner : MonoBehaviour
{
    public WaveSpawnInfo[] m_waves = new WaveSpawnInfo[0];      // Waves to spawn in the game

    public delegate int OnWaveFinished();       // Delegate for finishing a wave
    public OnWaveFinished onWaveFinished;       // Event for when a wave has finished (only called on server)

    private Coroutine m_spawnRoutine;

    public bool initWave(int waveId)
    {
        if (!PhotonNetwork.IsMasterClient)
            return false;

        if (!isValidWaveId(waveId))
            return false;

        if (m_spawnRoutine != null)
        {
            Debug.LogWarning("Initializing wave when current wave is still active!");
            StopCoroutine(m_spawnRoutine);
            m_spawnRoutine = null;
        }

        m_spawnRoutine = StartCoroutine(spawnRoutine(m_waves[waveId]));
        return m_spawnRoutine != null;
    }

    private IEnumerator spawnRoutine(WaveSpawnInfo info)
    {
        int remainingSpawns = info.m_count;
        while (remainingSpawns > 0)
        {
            spawnMonsterFor(GameManager.manager.m_p1Info);
            spawnMonsterFor(GameManager.manager.m_p2Info);

            yield return new WaitForSeconds(info.m_spawnInterval);

            --remainingSpawns;
        }

        if (onWaveFinished != null)
            onWaveFinished.Invoke();
    }

    private void spawnMonsterFor(PlayerInfo info)
    {
        if (!info || !info.isValid)
            return;

        PlayerController controller = info.controller;
        if (controller.commomMonster)
            info.m_monsterManager.spawnMonster(controller.commomMonster, info.boardManager);
    }

    public bool isValidWaveId(int id)
    {
        return id >= 0 && id < m_waves.Length;
    }
}

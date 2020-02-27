﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

[System.Serializable]
public struct WaveSpawnInfo
{
    public int m_count;             // Amount of common monsters to spawn
    public int m_spawnInterval;     // Interval for spawning a new monster
}


public class WaveSpawner : MonoBehaviourPun
{
    public WaveSpawnInfo[] m_waves = new WaveSpawnInfo[0];      // Waves to spawn in the game

    public delegate void OnWaveFinished();          // Delegate for finishing a wave
    public OnWaveFinished onWaveFinished;           // Event for when a wave has finished (only called on server)

    private Coroutine m_spawnRoutine;               // Routine currently handling spawning

    public bool IsWaveInProgress { get { return m_spawnRoutine != null; } }         // If a wave is current being processed

    public MonsterBase m_monsterPrefab;         // Monster to spawn (can be set in editor for testing)

    /// <summary>
    /// Initializes the wave. Needs to be called locally on all clients
    /// </summary>
    /// <param name="waveId">Id of wave</param>
    /// <returns>If wave has started</returns>
    public bool initWave(int waveId)
    {
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

    /// <summary>
    /// Routine for handling spawning
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    private IEnumerator spawnRoutine(WaveSpawnInfo info)
    {
#if !UNITY_EDITOR
        BoardManager board = getLocalBoardManager();
        if (!board)
        {
            // Be sure to null this (see isWaveInProgress)
            m_spawnRoutine = null;
            yield break;
        }
#endif

        int remainingSpawns = info.m_count;
        while (remainingSpawns > 0)
        {
#if UNITY_EDITOR
            spawnMonster(GameManager.manager.getBoardManager(0));
            spawnMonster(GameManager.manager.getBoardManager(1));
#else
            spawnMonster(board);
#endif

            // We want to call onWaveFinished as 
            // soon as the last monster is spawned
            --remainingSpawns;
            if (remainingSpawns <= 0)
                break;

            yield return new WaitForSeconds(info.m_spawnInterval);     
        }

        // This may call initWave (it shouldn't), so we null out
        // spawnRoutine after so any call to initWave falls out
        if (onWaveFinished != null)
            onWaveFinished.Invoke();

        m_spawnRoutine = null;
    }

    private void spawnMonster(BoardManager board)
    {
        if (!board)
            return;

        if (m_monsterPrefab)
            board.monsterManager.spawnMonster(m_monsterPrefab, board);
    }

    public bool isValidWaveId(int id)
    {
        return id >= 0 && id < m_waves.Length;
    }

    private BoardManager getLocalBoardManager()
    {
        return GameManager.manager.PlayersBoard;
    }
}

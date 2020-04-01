using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameManagerCosmetics : MonoBehaviourPun
{
    [SerializeField] private AudioClip m_friendlyGoalHurtSound;     // Sound to play when local players goal is reached by enemy
    [SerializeField] private AudioClip m_enemyGoalHurtSound;        // Sound to play when enemy players goal is reached by friendly

    [SerializeField] private AudioSource m_waveSoundsSource;        // Audio source to use for playing wave sounds
    [SerializeField] private AudioClip m_waveStartSound;            // Sound to play when the wave starts
    [SerializeField] private AudioClip m_waveFinishedSound;         // Sound to play when the wave finishes

    void Start()
    {
        // Doing this in Start on purposes!
        // GameManager instance gets set in Awake, we want to do this after
        GameManager instance = GameManager.manager;
        if (!instance)
            return;

        instance.onWaveStart += onWaveStart;
        instance.onWaveFinish += onWaveFinished;
    }

    private void onWaveStart()
    {
        playWaveSound(m_waveStartSound);
    }

    private void onWaveFinished()
    {
        playWaveSound(m_waveFinishedSound);
    }

    private void playWaveSound(AudioClip clip)
    {
        if (m_waveSoundsSource && clip)
        {
            m_waveSoundsSource.clip = clip;
            m_waveSoundsSource.loop = false;
            m_waveSoundsSource.Play();
        }
    }

    public static void playGoalHurtSound(int boardId)
    {
        GameManagerCosmetics cosmetics = GameManager.getCosmetics();
        if (cosmetics)
        {
            if (PhotonNetwork.IsConnected)
                cosmetics.photonView.RPC("goalHurtRPC", RpcTarget.All, boardId);
            else
                cosmetics.goalHurtRPC(boardId);
        }
    }

    [PunRPC]
    private void goalHurtRPC(int boardId)
    {
        BoardManager board = GameManager.manager.getBoardManager(boardId);

        AudioClip clipToPlay = null;
        if (board == GameManager.manager.PlayersBoard)
            clipToPlay = m_friendlyGoalHurtSound;
        else
            clipToPlay = m_enemyGoalHurtSound;

        playWaveSound(clipToPlay);
    }
}

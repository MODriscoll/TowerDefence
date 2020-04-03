using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameManagerCosmetics : MonoBehaviourPun
{
    [SerializeField] private AudioSource m_generalSoundsSource;     // Audio source to use for playing global game sounds (that shouldn't overlap

    [SerializeField] private AudioClip m_friendlyGoalHurtSound;     // Sound to play when local players goal is reached by enemy
    [SerializeField] private AudioClip m_enemyGoalHurtSound;        // Sound to play when enemy players goal is reached by friendly

    [SerializeField] private AudioClip m_playerWinsSound;           // Sound to play when match ends in victory
    [SerializeField] private AudioClip m_playerLoseSound;           // Sound to play when match ends in a draw or a lose

    [SerializeField] private AudioClip m_waveStartSound;            // Sound to play when the wave starts

    // Shh... Temp while we still don't have a sound (be sure to update onWaveFinished when uncommenting this)
    /*[SerializeField]*/ private AudioClip m_waveFinishedSound;         // Sound to play when the wave finishes

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
        playGeneralSound(m_waveStartSound);
    }

    private void onWaveFinished()
    {
        playGeneralSound(null/*m_waveFinishedSound*/);
    }

    /// <summary>
    /// Easy access for playing a goal hurt sound for a specific board
    /// </summary>
    /// <param name="boardId"></param>
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
        PlayerController controller = PlayerController.getController(boardId);
        if (!controller)
        {
            Debug.LogError("Unable to play GoalHurt sound as player who was hurt was unable to be determined");
            return;
        }

        // We play a general sound for when local player is hurt,
        // so no matter which board they are looking at they will hear the sound
        if (controller == PlayerController.localPlayer)
            playGeneralSound(m_friendlyGoalHurtSound);
        else // Must be remote player
            SoundEffectsManager.playSoundEffect(m_enemyGoalHurtSound, boardId);
    }

    /// <summary>
    /// Quick function for playing a sound based on if match was one
    /// </summary>
    /// <param name="wonMatch">If local player won the match</param>
    public void playMatchEndSound(bool wonMatch)
    {
        if (wonMatch)
            playGeneralSound(m_playerWinsSound);
        else
            playGeneralSound(m_playerLoseSound);
    }

    private void playGeneralSound(AudioClip clip)
    {
        if (m_generalSoundsSource && clip)
        {
            m_generalSoundsSource.clip = clip;
            m_generalSoundsSource.loop = false;
            m_generalSoundsSource.Play();
        }
    }
}

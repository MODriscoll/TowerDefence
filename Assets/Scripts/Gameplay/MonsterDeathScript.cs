using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MonsterDeathScript : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [SerializeField] private AudioClip m_sound;     // Sound to play

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        int boardId = (int)info.photonView.InstantiationData[0];
        SoundEffectsManager.playSoundEffect(m_sound, boardId);
    }
}

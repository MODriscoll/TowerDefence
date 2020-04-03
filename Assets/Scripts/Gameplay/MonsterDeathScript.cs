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

        float lifeSpan = 0.2f;
        if (m_sound)
            lifeSpan = m_sound.length;

        Invoke("destroySelf", lifeSpan);
    }

    private void destroySelf()
    {
        if (PhotonNetwork.IsConnected && photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
        else if (!PhotonNetwork.IsConnected)
            Destroy(gameObject);
    }
}

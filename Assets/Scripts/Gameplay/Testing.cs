using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Testing : MonoBehaviourPun
{
    // Start is called before the first frame update
    void Start()
    {
        Invoke("KillSelf", 2f);
    }

    void KillSelf()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            Debug.LogError("Destroying self on non owner client");
            PhotonNetwork.Destroy(gameObject);
        }
    }
}

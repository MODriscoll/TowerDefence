using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// This script prevents the game from starting until both players are ready
// This script destroys itself when the match has started. This does not
// need to be placed manually, as the game manager will spawn one itself
public class GameStartHandler : MonoBehaviour
{
    void Update()
    {
        // Don't need to exist if match is already in progress
        if (GameManager.manager.hasMatchStarted)
        {
            Destroy(this);
            return;
        }

#if UNITY_EDITOR   
        startMatch();
        return;
#else
        if (PhotonNetwork.IsConnected && PhotonNetwork.PlayerList.Length >= 2)
            // Only start match if master client
            if (PhotonNetwork.IsMasterClient)
            {
                startMatch();
                return;
            }
#endif
    }

    private void startMatch()
    {
        GameManager.manager.startMatch();
        Destroy(this);
    }
}

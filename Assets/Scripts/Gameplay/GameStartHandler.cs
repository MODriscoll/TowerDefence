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

        if (PhotonNetwork.IsConnected)
        {
            // Wait for both to be ready before starting
            if (PhotonNetwork.IsMasterClient &&
                PhotonNetwork.PlayerList.Length >= 2)
            {
                startMatch();
            }
        }
#if UNITY_EDITOR
        else
        {
            // Most likely playing in editor, just start now
            startMatch();
        }
#endif
    }

    private void startMatch()
    {
        GameManager.manager.startMatch();
        Destroy(this);
    }
}

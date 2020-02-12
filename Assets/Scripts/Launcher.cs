﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public readonly string gameVersion = "1";
    public readonly byte maxPlayers = 2;

    public string m_gameLevel;                  // The level to join when both players connect
    private bool m_bIsConnecting = false;       // If we are in the progress of connecting to a room

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        Connect();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN");

        if (m_bIsConnecting)
        {
            PhotonNetwork.JoinRandomRoom();
            m_bIsConnecting = false;
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("OnDisconnected() was called by PUN with reason {0}", cause);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom() called by PUN. Now this client is in a room.");

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            PhotonNetwork.LoadLevel(m_gameLevel);
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayers });
    }

    /// <summary>
    /// Connects to a random room
    /// </summary>
    protected void Connect()
    {
        PhotonNetwork.GameVersion = gameVersion;
        m_bIsConnecting = PhotonNetwork.ConnectUsingSettings();   
    }
}

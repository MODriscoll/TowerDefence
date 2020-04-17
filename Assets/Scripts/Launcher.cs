using System.Collections;
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
        // Register helpers used throughout application
        PhotonHelpers.register();

        PhotonNetwork.AutomaticallySyncScene = true;

        // Connect to photon
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = gameVersion;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        m_bIsConnecting = false;
    }

    public override void OnJoinedRoom()
    {
        // If we are the first one in the room, go and wait in the game level
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            PhotonNetwork.LoadLevel(m_gameLevel);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Failed to connect to a random room, create our own one instead
        string roomName = null;
        if (PlayerPrefs.HasKey("PlayerName"))
            roomName = PlayerPrefs.GetString("PlayerName");

        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = maxPlayers });
    }

    /// <summary>
    /// Connects to a room by creating one or joining one
    /// </summary>
    public void Connect()
    {
        if (m_bIsConnecting)
            return;

        m_bIsConnecting = true;   

        if (PhotonNetwork.IsConnected)
        {
            string roomName = null;
            if (PlayerPrefs.HasKey("PlayerRoom"))
                roomName = PlayerPrefs.GetString("PlayerRoom");

            // Possible that room name has been set to nothing, in
            // that case fall back to joining a random room
            if (roomName.Length == 0)
                roomName = null;

            if (roomName != null)
                PhotonNetwork.JoinOrCreateRoom(PlayerPrefs.GetString("PlayerRoom"), getDefaultRoomOptions(), TypedLobby.Default);
            else
                PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            m_bIsConnecting = false;
        }
    }

    /// <summary>
    /// Tries to connect to a random room.
    /// Will create a room if no room is found
    /// </summary>
    public void ConnectRandom()
    {
        if (m_bIsConnecting)
            return;

        m_bIsConnecting = true;

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.JoinRandomRoom();
        else
            m_bIsConnecting = false;
    }

    private RoomOptions getDefaultRoomOptions()
    {
        return new RoomOptions { MaxPlayers = maxPlayers };
    }
}

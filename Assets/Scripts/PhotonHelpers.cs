using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public struct PhotonHelpers
{
    public static readonly byte vector3IntCode = 0;

    public static void register()
    {
        PhotonPeer.RegisterType(typeof(Vector3Int), vector3IntCode, serializeVector3Int, deserializeVector3Int);
    }

    // Not optimized
    public static byte[] serializeVector3Int(object customType)
    {
        Vector3Int c = (Vector3Int)customType;

        // Assuming c.z is always zero
        byte[] bytes = new byte[sizeof(int) * 2];
        Buffer.BlockCopy(BitConverter.GetBytes(c.x), 0, bytes, sizeof(int) * 0, sizeof(int));
        Buffer.BlockCopy(BitConverter.GetBytes(c.y), 0, bytes, sizeof(int) * 1, sizeof(int));

        return bytes;
    }

    // Not optimized
    public static object deserializeVector3Int(byte[] data)
    {
        Vector3Int c = Vector3Int.zero;
        c.x = BitConverter.ToInt32(data, sizeof(int) * 0);
        c.y = BitConverter.ToInt32(data, sizeof(int) * 1);

        return c;
    }

    /// <summary>
    /// Gets the first player that isn't the passed in player. This simply iterates
    /// over all players, and since there will only ever be 2, we simply iterate the array
    /// </summary>
    /// <param name="notThis">Player to ignore</param>
    /// <returns>Player or null</returns>
    public static Photon.Realtime.Player getFirstPlayer(Photon.Realtime.Player notThis)
    {
        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            if (player != notThis)
                return player;

        return null;
    }
}

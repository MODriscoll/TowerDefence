using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BackgroundLoader : MonoBehaviour
{
    static private PlayerController playerController;

    //public SpriteRenderer PlayerBackgroundImage;
    public Material[] backgrounds;
    public MeshRenderer P1Background;
    public MeshRenderer P2Background;

    private ExitGames.Client.Photon.Hashtable myCustomProperties = new ExitGames.Client.Photon.Hashtable();

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();

        // Create Player Hashtable
        myCustomProperties["Background"] = PlayerPrefs.GetInt("selectedBackground");
        PhotonNetwork.LocalPlayer.CustomProperties = myCustomProperties;

        SetCustomBackground(PhotonNetwork.LocalPlayer);
    }

    public void SetCustomBackground(Player player)
    {

        // Set background to default
        int background = 0;
        int playerID = playerController.playerId;
        if (player.CustomProperties.ContainsKey("Background"))
        {
            // Set Background
            background = (int)player.CustomProperties["Background"];
            //PlayerBackgroundImage.sprite = backgrounds[background - 1];

            if (playerID == 0)
            {
                P1Background.material = backgrounds[background];
            }
            else if (playerID == 1)
            {
                P2Background.material = backgrounds[background];
            }
        }
    }

    //public void UpdateCustomBackground(Player player, int playerID)
    //{
    //    if (playerID == 0)
    //    {

    //    }
    //}
}

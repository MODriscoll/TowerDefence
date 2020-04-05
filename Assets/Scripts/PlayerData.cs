using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class PlayerData : MonoBehaviour
{
    public TextMeshProUGUI m_moneyText;

    private int backgroundCost = 3000;

    private void Start()
    {
        ResetPlayerData();
        Load();
    }

    public void Load()
    {
        SetPremiumCurrencyText();
    }

    public void BuyBackground(int BackgroundPattern)
    {
        // If the player hasn't bought it yet
        if (PlayerPrefs.GetInt(BackgroundPattern.ToString(), 0) == 0)
        {
            if (PlayerPrefs.HasKey("playerCash"))
            {
                // If player has enough money
                if (PlayerPrefs.GetInt("playerCash", 0) >= backgroundCost)
                {
                    // Save background as being "bought"
                    PlayerPrefs.SetInt(BackgroundPattern.ToString(), 1);
                    // Update and save player's currency
                    PlayerPrefs.SetInt("playerCash", PlayerPrefs.GetInt("playerCash") - backgroundCost);
                    SetPremiumCurrencyText();
                    // Set Background
                    PlayerPrefs.SetInt("selectedBackground", BackgroundPattern);
                }
                // If player doesn't have enough money
                else
                {

                }
            }
        }
        // If the player has bought it
        else if(PlayerPrefs.GetInt(BackgroundPattern.ToString(), 0) == 1)
        {
            PlayerPrefs.SetInt("selectedBackground", BackgroundPattern);
        }
    }

    public void BuyGoldCheese(int amount)
    {
        PlayerPrefs.SetInt("playerCash", PlayerPrefs.GetInt("playerCash") + amount);
        SetPremiumCurrencyText();
    }

    private void SetPremiumCurrencyText()
    {
        if (PlayerPrefs.GetInt("playerCash") > 999)
            m_moneyText.text = PlayerPrefs.GetInt("playerCash").ToString("0,000");
        else
            m_moneyText.text = PlayerPrefs.GetInt("playerCash").ToString();
    }

    private void ResetPlayerData()
    {
        PlayerPrefs.SetInt("playerCash", 0);
        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.SetInt(i.ToString(), 0);
        }
    }
}

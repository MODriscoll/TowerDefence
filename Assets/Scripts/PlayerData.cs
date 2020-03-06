using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerData : MonoBehaviour
{
    [SerializeField] private int playerCash;
    public Text m_moneyText;

    private int backgroundCost = 3000;

    private void Start()
    {
        ResetPlayerData();
        Load();
    }

    public void Load()
    {
        playerCash = PlayerPrefs.GetInt("playerCash");
        SetPremiumCurrencyText();
    }

    public void BuyBackground(string name)
    {
        // If the player hasn't bought it yet
        if (PlayerPrefs.GetInt(name, 0) == 0)
        {
            if (PlayerPrefs.HasKey("playerCash"))
            {
                if (PlayerPrefs.GetInt("playerCash", 0) >= backgroundCost)
                {
                    PlayerPrefs.SetInt(name, 1);
                    PlayerPrefs.SetInt("playerCash", PlayerPrefs.GetInt("playerCash") - backgroundCost);
                    SetPremiumCurrencyText();
                }
            }

        }
        // If the player has bought it
        else
        {
            PlayerPrefs.SetString("selectedBackground", name);
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
            m_moneyText.text = "$" + PlayerPrefs.GetInt("playerCash").ToString("0,000");
        else
            m_moneyText.text = "$" + PlayerPrefs.GetInt("playerCash").ToString();
    }

    private void ResetPlayerData()
    {
        PlayerPrefs.SetInt("playerCash", 0);
        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.SetInt("background" + i.ToString("00"), 0);
        }
    }
}

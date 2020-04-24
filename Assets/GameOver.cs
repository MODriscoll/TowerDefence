using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameOver : MonoBehaviour
{
    private MenuManager menuManager;

    public void Awake()
    {
        if (FindObjectOfType<MenuManager>())
        {
            menuManager = FindObjectOfType<MenuManager>();
            SwapMenu(PlayerPrefs.GetString("GameOverState"));
        }
    }

    private void SwapMenu(string menu)
    {
        if (menu == "Win")
        {
            menuManager.LoadMenu(menu + "Menu");
            SetGameOverScreen("None");
        }
        else if (menu == "Lose")
        {
            menuManager.LoadMenu(menu + "Menu");
            SetGameOverScreen("None");
        }
        else if (menu == "Draw")
        {
            menuManager.LoadMenu(menu + "Menu");
            SetGameOverScreen("None");
        }
        else
        {
            menuManager.LoadMenu("Logo" + "Menu");
            SetGameOverScreen("None");
        }
    }

    public void SetGameOverScreen(string state)
    {
        PlayerPrefs.SetString("GameOverState", state);
    }

}

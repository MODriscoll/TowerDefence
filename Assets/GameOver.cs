using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOver : MonoBehaviour
{
    public GameObject winScreen;
    public GameObject loseScreen;
    public GameObject drawScreen;

    private MenuManager menuManager;

    public void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (FindObjectOfType<MenuManager>())
        {
            menuManager = FindObjectOfType<MenuManager>();
        }
    }

    public enum WinState
    {
        Win,
        Lose,
        Draw,
    }

    WinState winState;

    public void UpdateWinState()
    {
        switch(winState)
        {
            case WinState.Win:
                SwapMenu("Win");
                break;
            case WinState.Lose:
                SwapMenu("Lose");
                break;
            case WinState.Draw:
                SwapMenu("Draw");
                break;
            default:
                SwapMenu("n/a");
                break;
        }
    }

    private void SwapMenu(string menu)
    {
        if (menu == "Win")
        {
            winScreen.SetActive(true);
            loseScreen.SetActive(false);
            drawScreen.SetActive(false);
        }
        else if (menu == "Lose")
        {
            winScreen.SetActive(false);
            loseScreen.SetActive(true);
            drawScreen.SetActive(false);
        }
        else if (menu == "Draw")
        {
            winScreen.SetActive(false);
            loseScreen.SetActive(false);
            drawScreen.SetActive(true);
        }
        else
        {
            winScreen.SetActive(false);
            loseScreen.SetActive(false);
            drawScreen.SetActive(false);
        }
    }

    public void SetGameOverScreen(string state)
    {
        PlayerPrefs.SetString("GameOverState", state);
    }

}

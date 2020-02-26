using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsMenu;
    public GameObject shopMenu;
    public GameObject matchTypeMenu;
    public GameObject privateMatchMenu;
    static private GameObject[] menus;

    // Start is called before the first frame update
    void Start()
    {
        menus = new GameObject[5] { mainMenu, matchTypeMenu, optionsMenu, shopMenu, privateMatchMenu };
    }

    public void LoadMenu(string newMenu)
    {
        foreach (GameObject menu in menus)
        {
            if (menu.name == newMenu)
                menu.SetActive(true);
            else
                menu.SetActive(false);
        }
    }


    public void LoadScene(string scene)
    {
        LoadScene(scene);
    }
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] public GameObject[] menus;

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

    public void quit()
    {
        Application.Quit();
    }

    public void ToggleObject(GameObject item)
    {
        item.gameObject.SetActive(!item.gameObject.activeSelf);
    }
}

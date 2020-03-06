using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConcedeButtonLogic : MonoBehaviour
{
    private void Awake()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1)
            gameObject.SetActive(true);
        else
            gameObject.SetActive(false);
    }

    public void ExitToMain()
    {
        GameManager.manager.LeaveGame();
    }
}

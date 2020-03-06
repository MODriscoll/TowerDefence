using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveButton : MonoBehaviour
{
    public GameObject yourbutton;

    public void DisableButton()
    {
        yourbutton.SetActive(false);
    }
}

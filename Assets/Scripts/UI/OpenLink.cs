using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenLink : MonoBehaviour
{
    public void Hyperlink(string link)
    {
        Application.OpenURL(link);
    }
}

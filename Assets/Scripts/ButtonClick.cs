using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonClick : MonoBehaviour
{
    public AudioClip ClickSound;

    public AudioSource audiosource;
    public void PlaySound()
    {
        audiosource.PlayOneShot(ClickSound);
    }

}

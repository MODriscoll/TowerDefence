using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonClick : MonoBehaviour
{
    public AudioClip ClickSound;

    private AudioSource audioSource;

    private void Start()
    {
        if (audioSource == null)
            audioSource = FindObjectOfType<AudioSource>();
    }

    public void PlaySound()
    {
        if (audioSource == null)
        {
            Debug.Log("No AudioSource found, relocating...");
            audioSource = FindObjectOfType<AudioSource>();
        }
        audioSource.PlayOneShot(ClickSound);
    }

}

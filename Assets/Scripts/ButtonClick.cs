using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonClick : MonoBehaviour
{
    public AudioClip ClickSound;

    private AudioSource audioSource;

    private void Start()
    {
        GetAudioSource();
    }

    public void PlaySound()
    {
        GetAudioSource();
        audioSource.PlayOneShot(ClickSound);
    }

    private void GetAudioSource()
    {
        if (audioSource == null)
        {
            foreach (AudioSource source in FindObjectsOfType<AudioSource>())
            {
                if (source.name.Contains("SFX"))
                    audioSource = source;
            }
        }
    }

}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Used by SoundEffectManager to make sure no sound of the same clip plays again
[RequireComponent(typeof(AudioSource))]
public class SoundEffectHandler : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource;         // Source of this effects sound

    public AudioClip clip { get { return m_audioSource ? m_audioSource.clip : null; } }     // The sound effect we are playing

    private VolumeManager m_volumeManager;      // The controller which stores the sfx's volume

    // Tf this effect should be muted
    public bool Mute
    {
        get
        {
            return m_audioSource ? m_audioSource.mute : false;
        }
        set
        {
            if (m_audioSource)
                m_audioSource.mute = value;
        }
    }

    public delegate void OnSoundEffectFinished(SoundEffectHandler handler);     // Delegate for when an effect has finished
    public OnSoundEffectFinished onEffectFinished;                              // Event called when audio has finished playing

    void OnDestroy()
    {
        if (onEffectFinished != null && m_audioSource.isPlaying)
            onEffectFinished.Invoke(this);
    }

    /// <summary>
    /// Plays the sound clip from beginning
    /// </summary>
    /// <param name="clip">Audio to play</param>
    /// <param name="volume">Volume to play the clip at</param>
    /// <param name="startMuted">If clip should play but initially be muted</param>
    public void playClip(AudioClip clip, float volume, bool startMuted = false)
    {
        CancelInvoke("onAudioFinished");

        if (!m_audioSource)
        {
            m_audioSource = gameObject.AddComponent<AudioSource>();
            m_audioSource.spatialize = false;
        }

        m_audioSource.clip = clip;
        m_audioSource.mute = startMuted;
        m_audioSource.volume = volume;
        m_audioSource.Play();

        enabled = true;
        Invoke("onAudioFinished", clip.length);
    }

    private void onAudioFinished()
    {
        if (onEffectFinished != null)
        {
            onEffectFinished.Invoke(this);
            onEffectFinished = null;
        }

        Destroy(gameObject);
    }
}

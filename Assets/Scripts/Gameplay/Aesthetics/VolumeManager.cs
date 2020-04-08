using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [Range(0, 1)] public float m_musicVolume;
    [Range(0, 1)] public float m_sfxVolume;

    public Slider musicSlider;
    public Slider sfxSlider;

    private AudioSource musicAudioSource;
    private AudioSource sfxAudioSource;

    // Start is called before the first frame update
    void Start()
    {
        musicAudioSource = FindObjectOfType<AudioSource>();
        musicAudioSource.volume = PlayerPrefs.GetFloat("MusicVolume");
        sfxAudioSource.volume = PlayerPrefs.GetFloat("SFXVolume");
    }

    public void UpdateMusicVolume()
    {
        m_musicVolume = musicSlider.value;
        musicAudioSource.volume = m_musicVolume;
        PlayerPrefs.SetFloat("MusicVolume", m_musicVolume);
    }

    public void UpdateSFXVolume()
    {
        m_sfxVolume = sfxSlider.value;
        sfxAudioSource.volume = m_sfxVolume;
        PlayerPrefs.SetFloat("SFXVolume", m_sfxVolume);
    }
}

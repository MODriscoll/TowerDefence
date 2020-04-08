using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [Range(0, 1)] public float m_masterVolume;
    [Range(0, 1)] public float m_musicVolume;
    [Range(0, 1)] public float m_sfxVolume;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private AudioSource musicAudioSource;

    // Start is called before the first frame update
    void Start()
    {
        musicAudioSource = FindObjectOfType<AudioSource>();
        UpdateMasterVolume();
        UpdateMusicVolume();
        UpdateSFXVolume();
    }

    public void UpdateMasterVolume()
    {
        m_masterVolume = masterSlider.value;
        musicAudioSource.volume = m_musicVolume * m_masterVolume;
        PlayerPrefs.SetFloat("MasterVolume", m_masterVolume);
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
        PlayerPrefs.SetFloat("SFXVolume", m_sfxVolume);
    }
}

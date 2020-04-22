using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeManager : MonoBehaviour
{
    [Range(0, 1)] public static float m_masterVolume;
    [Range(0, 1)] public static float m_musicVolume;
    [Range(0, 1)] public static float m_sfxVolume;

    public Toggle toggleMaster;
    public Toggle toggleMusic;
    public Toggle toggleSFX;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private AudioSource musicAudioSource;
    private AudioSource sfxAudioSource;

    // Start is called before the first frame update
    void Awake()
    {
        // Set AudioSources to their corresponding source
        foreach (AudioSource source in FindObjectsOfType<AudioSource>())
        {
            if (source.name.Contains("Music"))
                musicAudioSource = source;
            else if (source.name.Contains("SFX"))
                sfxAudioSource = source;
        }
        
        StartSequence();
        gameObject.SetActive(false);
    }

    public void StartSequence()
    {
        toggleMaster.isOn = (PlayerPrefs.GetInt("MasterToggle") > 0) ? false : true;
        toggleMusic.isOn = (PlayerPrefs.GetInt("MusicToggle") > 0) ? false : true;
        toggleSFX.isOn = (PlayerPrefs.GetInt("SFXToggle") > 0) ? false : true;

        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume");
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume");

        SetMasterVolume();
        SetMusicVolume();
        SetSFXVolume();
    }

    public void ToggleMaster()
    {
        UpdateToggle("MasterToggle");
        SetMasterVolume();
    }
    public void ToggleMusic()
    {
        UpdateToggle("MusicToggle");
        SetMusicVolume();
    }
    public void ToggleSFX()
    {
        UpdateToggle("SFXToggle");
        SetSFXVolume();
    }
    public void UpdateToggle(string id)
    {
        int state = Mathf.Abs(PlayerPrefs.GetInt(id) - 1);
        PlayerPrefs.SetInt(id, state);
    }

    public void SetMasterVolume()
    {
        m_masterVolume = masterSlider.value;
        SetMusicVolume();
        SetSFXVolume();
    }
    public void SetMusicVolume()
    {
        m_musicVolume = musicSlider.value;
        musicAudioSource.volume = m_masterVolume * m_musicVolume * PlayerPrefs.GetInt("MasterToggle") * PlayerPrefs.GetInt("MusicToggle");
    }
    public void SetSFXVolume()
    {
        m_sfxVolume = sfxSlider.value;
        sfxAudioSource.volume = m_masterVolume * m_sfxVolume * PlayerPrefs.GetInt("MasterToggle") * PlayerPrefs.GetInt("SFXToggle");
    }

    public float GetMusicVolume()
    {
        return m_masterVolume * m_musicVolume * PlayerPrefs.GetInt("MasterToggle") * PlayerPrefs.GetInt("MusicToggle");
    }
    public float GetSFXVolume()
    {
        return m_masterVolume* m_sfxVolume *PlayerPrefs.GetInt("MasterToggle") * PlayerPrefs.GetInt("SFXToggle");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerCosmetics : MonoBehaviour
{
    [SerializeField] private AudioSource m_waveSoundsSource;        // Audio source to use for playing wave sounds
    [SerializeField] private AudioClip m_waveStartSound;            // Sound to play when the wave starts
    [SerializeField] private AudioClip m_waveFinishedSound;         // Sound to play when the wave finishes
}

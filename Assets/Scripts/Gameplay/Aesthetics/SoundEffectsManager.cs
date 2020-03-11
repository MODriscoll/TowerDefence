using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// This is a manager for sound effects, it does not manage all audio
public class SoundEffectsManager : MonoBehaviour
{
    static public SoundEffectsManager instance = null;      // Instance of the sound effect manager

    // If a sound effect is already being played, should we just
    // restart that sound effect as opposed to creating a new one?
    public bool m_restartActiveEffects = false;

    [SerializeField] private SoundEffectHandler m_effectPrefab;     // Prefab to use to instantiate instead of creating new objects

    private Dictionary<AudioClip, List<SoundEffectHandler>> m_activeSoundEffects = null;    // All sounds effects playing a certain clip that are active

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("SoundEffectsManager already exists");
            return;
        }

        instance = this;
        m_activeSoundEffects = new Dictionary<AudioClip, List<SoundEffectHandler>>();
    }

    void OnDestroy()
    {
        if (instance && instance == this)
            instance = null;
    }

    /// <summary>
    /// Plays the sound effect following the rules provided by the singleton manager
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    public static void playSoundEffect(AudioClip clip)
    {
        if (instance)
            instance.playSoundEffectImpl(clip);
    }

    private void playSoundEffectImpl(AudioClip clip)
    {
        if (!clip)
            return;

        List<SoundEffectHandler> handlerList = getHandlerList(clip, true);
        Assert.IsNotNull(handlerList);

        SoundEffectHandler handler = null;
        if (m_restartActiveEffects)
            if (handlerList.Count > 0)
                handler = handlerList[0];

        if (!handler)
        {
            handler = spawnHandler();
            handlerList.Add(handler);
        }

        Assert.IsNotNull(handler);
        handler.playClip(clip);

        // Temp testing
        Debug.Log(string.Format("Playing Audio Clip ({0}). Number of instances: {1}", clip.name, handlerList.Count));
    }

    private void onEffectFinished(SoundEffectHandler handler)
    {
        List<SoundEffectHandler> handlerList = getHandlerList(handler.clip, false);
        if (handlerList == null)
        {
            Debug.LogWarning("Manager listened for handler being destroyed that was not in its map");
            return;
        }

        // Remove the list if no other sounds are active
        handlerList.Remove(handler);
        if (handlerList.Count == 0)
            m_activeSoundEffects.Remove(handler.clip);

        Debug.Log("Sound Effect Destroyed, Number remaining: " + handlerList.Count);
    }

    private SoundEffectHandler spawnHandler()
    {
        SoundEffectHandler effectHandler = null;
        if (m_effectPrefab)
            effectHandler = Instantiate(m_effectPrefab);
        else
            effectHandler = new GameObject().AddComponent<SoundEffectHandler>();

        effectHandler.onEffectFinished += onEffectFinished;
        return effectHandler;
    }

    private List<SoundEffectHandler> getHandlerList(AudioClip clip, bool createIfRequired)
    {
        if (m_activeSoundEffects.ContainsKey(clip))
            return m_activeSoundEffects[clip];

        if (createIfRequired)
        {
            List<SoundEffectHandler> handlerList = new List<SoundEffectHandler>();
            m_activeSoundEffects.Add(clip, handlerList);
            return handlerList;
        }

        return null;
    }
}

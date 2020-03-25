using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// This is a manager for sound effects, it does not manage all audio
public class SoundEffectsManager : MonoBehaviour
{
    static public SoundEffectsManager instance = null;      // Instance of the sound effect manager

    // Handles playing sound effects per board. We need to split it up so sounds for each board
    // are muted when the player is currently not looking at that board
    protected class SingleManager
    {
        public SoundEffectHandler m_handlerPrefab;

        // All sounds effects playing a certain clip that are active
        private Dictionary<AudioClip, List<SoundEffectHandler>> m_activeSoundEffects = new Dictionary<AudioClip, List<SoundEffectHandler>>();

        public void playInstance(AudioClip clip, bool restartActive, bool startMuted = false)
        {
            if (!clip)
                return;

            List<SoundEffectHandler> handlerList = getHandlerList(clip, true);
            Assert.IsNotNull(handlerList);

            SoundEffectHandler handler = null;
            if (restartActive)
                if (handlerList.Count > 0)
                    handler = handlerList[0];

            if (!handler)
            {
                handler = spawnHandler();
                handlerList.Add(handler);
            }

            Assert.IsNotNull(handler);
            handler.playClip(clip, startMuted);
        }

        public void muteGroup(bool mute)
        {
            if (m_activeSoundEffects == null)
                return;

            // Not the best implementation, but sound effects should be short, meaning
            // that there shouldn't be that many audio source to mute regardless
            // TODO: Cache each handler in an array which we use for muting?
            foreach (var clipGroup in m_activeSoundEffects.Values)
                foreach (SoundEffectHandler handler in clipGroup)
                    handler.Mute = mute;
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
        }

        private SoundEffectHandler spawnHandler()
        {
            SoundEffectHandler effectHandler = null;
            if (m_handlerPrefab)
                effectHandler = Instantiate(m_handlerPrefab);
            else
            {
                GameObject handlerObject = new GameObject();
#if UNITY_EDITOR
                handlerObject.name = "SoundEffectHandlerInstance";
#endif
                effectHandler = handlerObject.AddComponent<SoundEffectHandler>();
            }

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

    // If a sound effect is already being played, should we just
    // restart that sound effect as opposed to creating a new one?
    public bool m_restartActiveEffects = false;

    [SerializeField] private SoundEffectHandler m_effectPrefab;     // Prefab to use to instantiate instead of creating new objects

    private Dictionary<int, SingleManager> m_groups = new Dictionary<int, SingleManager>();     // Managers per group, created when needed
    private int m_activeGroup = -1;                                                             // Id of active group

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("SoundEffectsManager already exists");
            return;
        }

        instance = this;
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
    /// <param name="groupID">Group which to play this sound in</param>
    public static void playSoundEffect(AudioClip clip, int groupID)
    {
        if (instance)
            instance.playSoundEffectImpl(clip, groupID);
    }

    /// <summary>
    /// Plays the sound effect following the rules provided by the singleton manager
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="board">The board to play this sound on</param>
    public static void playSoundEffect(AudioClip clip, BoardManager board)
    {
        int groupId = GameManager.manager.getPlayerIdFromBoard(board);
        playSoundEffect(clip, groupId);
    }

    /// <summary>
    /// Sets the group whose sounds we should be playing
    /// </summary>
    /// <param name="groupId">group of sounds to play</param>
    public static void setActiveGroup(int groupId)
    {
        if (instance)
            instance.setActiveGroupImpl(groupId);
    }

    private void playSoundEffectImpl(AudioClip clip, int groupId)
    {
        if (!clip)
            return;

        SingleManager manager = null;
        if (m_groups.ContainsKey(groupId))
            manager = m_groups[groupId];
        else
            manager = createManager(groupId);

        bool startMuted = groupId != m_activeGroup;
        manager.playInstance(clip, m_restartActiveEffects, startMuted);
    }

    private void setActiveGroupImpl(int groupId)
    {
        if (groupId != m_activeGroup)
        {
            // First mute old group
            if (m_groups.ContainsKey(m_activeGroup))
                m_groups[m_activeGroup].muteGroup(true);

            m_activeGroup = groupId;

            // Now unmute new group
            if (m_groups.ContainsKey(m_activeGroup))
                m_groups[m_activeGroup].muteGroup(false);
        }
    }

    private SingleManager createManager(int groupId)
    {
        SingleManager manager = new SingleManager();
        manager.m_handlerPrefab = m_effectPrefab;

        m_groups.Add(groupId, manager);
        return manager;
    }
}

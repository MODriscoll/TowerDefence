using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

public class MonsterBase : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback, IPunObservable
{
    [Min(0.01f)] public float m_travelDuration = 2f;        // Amount of time (in seconds) it takes to traverse 1 tile
    [Min(0)] public int m_reward = 10;                      // Reward to give player when we are killed
    [Min(1)] public int m_damage = 5;                       // The amount of damage this monster deals when reaching the Goal Tile
    public int m_health = 10;                               // How much health this monster has
    public HealthUI healthBar;                              // Reference healthbar UI for Entity

    [SerializeField] private AudioClip m_spawnSound;        // This sound is played when we are spawned in

    // Script to spawn when we die (not when reaching goal)
    [PhotonPrefab(typeof(MonsterDeathScript))]
    [SerializeField] private string m_deathScriptPrefab;

    protected BoardManager m_board;                 // The board we are active on

    //private int newHealth = 0;              // How much health enemy monsters have

    private float m_progress = 0f;          // Progress along current path. Is used by board manager to find where we are
    private int m_pathIndex = -1;           // Index of the path we are following
    private bool m_canBeDamaged = true;     // If this monster can be damaged

    private float m_networkProgress = 0f;   // Progress that our owner is up to, we use this is smooth our movement on remote monsters
    private float m_progressDelta = 0f;     // Delta between local progress and network progress when progress was last replicated

    public delegate void OnTakeDamage(int damage, bool bKilled);
    public OnTakeDamage OnMonsterTakenDamage;

    [SerializeField] private Renderer m_renderer;       // This monsters renderer. Is used for collision checks

    public BoardManager Board { get { return m_board; } }
    public float TilesTravelled { get { return m_progress; } }

    public bool HasBounds { get { return m_renderer != null; } }
    public Bounds Bounds { get { return m_renderer ? m_renderer.bounds : new Bounds(); } }

    void Start()
    {
        // Add ourselves to our board on the instigators side (since monsters are spawned on player who owns the board)
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            MonsterManager monsterManager = m_board.MonsterManager;
            if (monsterManager)
                monsterManager.addExternalMonster(this);
        }

        healthBar.SetMaxHealth(m_health);   //Set Healthbar UI
    }

    void OnDestroy()
    {
        // Like in start, we need to remove ourselves on the instigators side
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            MonsterManager monsterManager = m_board.MonsterManager;
            if (monsterManager)
                monsterManager.removeExternalMonster(this);

        }
    }

    public virtual void initMoster(BoardManager boardManager, int pathToFollow)
    {
        m_board = boardManager;

        m_progress = 0f;
        m_pathIndex = pathToFollow;

        updatePositionAlongPath(0f);

        // Spawn cosmetics here as this gets called after Start()
        SoundEffectsManager.playSoundEffect(m_spawnSound, m_board);
    }

    public virtual void tick(float deltaTime)
    {
        if (!PhotonNetwork.IsConnected || photonView.IsMine)
        {
            float delta = deltaTime / m_travelDuration;

            // Travel along our path, destroy ourselves once we reach the goal
            m_progress += delta;
            if (updatePositionAlongPath(m_progress))
            {
                // Damage the player
                PlayerController.localPlayer.applyDamage(m_damage);

                AnalyticsHelpers.reportMonsterReachedGoal(this);

                // We destroy ourselves after tick has concluded
                MonsterManager.destroyMonster(this, false);
            }
        }
        else
        {
            // Mimics how PhotonTransformView works
            m_progress = Mathf.MoveTowards(m_progress, m_networkProgress, m_progressDelta * (1.0f / PhotonNetwork.SerializationRate));
            updatePositionAlongPath(m_progress);
        }
    }

    // TODO: Document (returns true if killed)
    public bool takeDamage(int amount, Object instigator = null)
    {
        if (m_health <= 0 || !m_canBeDamaged)
            return false;

        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return false;

        if (amount <= 0)
        {
            Debug.LogError("Cannot apply less than zero damage to a monster");
            return false;
        }


        m_health = Mathf.Max(m_health - amount, 0);

        healthBar.SetHealth(m_health);  //Update Healthbar UI

        bool bKilled = m_health <= 0;

        // Execute events before potentially destroying ourselves
        if (OnMonsterTakenDamage != null)
            OnMonsterTakenDamage.Invoke(amount, bKilled);

        // Nothing more needs to be done if still alive
        if (m_health > 0)
            return false;

        AnalyticsHelpers.reportMonsterDeath(this, instigator ? instigator.name : "unknown");

        // We can give gold to the local player, since we already
        // checked that this monster belongs to them
        PlayerController.localPlayer.giveGold(m_reward);

        if (!string.IsNullOrEmpty(m_deathScriptPrefab))
        {
            object[] spawnData = new object[1];
            spawnData[0] = GameManager.manager.getPlayerIdFromBoard(m_board);
            PhotonNetwork.Instantiate(m_deathScriptPrefab, transform.position, Quaternion.identity, 0, spawnData);
        }

        MonsterManager.destroyMonster(this);
        return true;
    }


    public void setCanBeDamaged(bool bCanDamage)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        m_canBeDamaged = bCanDamage;
    }

    /// <summary>
    /// Updates monsters position along the path it is following based on progress
    /// </summary>
    /// <param name="progress">Progress along path we are</param>
    /// <returns>If monster is at goal tile or false if not</returns>
    private bool updatePositionAlongPath(float progress)
    {
        bool atGoalTile = false;
        if (m_board)
            transform.position = m_board.pathProgressToPosition(m_pathIndex, progress, out atGoalTile);

        return atGoalTile;
    }

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        int boardId = (int)info.photonView.InstantiationData[0];
        m_board = GameManager.manager.getBoardManager(boardId);

        m_pathIndex = (int)info.photonView.InstantiationData[1];
        updatePositionAlongPath(0f);

        // Play sound here as this gets called after Start()
        SoundEffectsManager.playSoundEffect(m_spawnSound, m_board);
    }

    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(m_progress);
            stream.SendNext(m_canBeDamaged);
            stream.SendNext(m_health);
        }
        else
        {
            m_networkProgress = (float)stream.ReceiveNext();
            m_canBeDamaged = (bool)stream.ReceiveNext();

            m_health = (int)stream.ReceiveNext();
            healthBar.SetHealth(m_health);  //Update Healthbar UI


            m_progressDelta = m_networkProgress - m_progress;
        }



    }


}

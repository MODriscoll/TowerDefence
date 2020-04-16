using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Photon.Pun;

public class TowerBase : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback//, IPunObservable
{
    [Min(0)] public int m_cost = 10;                // The cost to build this tower (0 = free)
    public int m_health = 10;                       // How much health this tower has
    public int m_maxHealth = 10;                    // Max health this tower can have
    public float destroyedShakeIntensity;           // How intense the shaking will be
    public float destroyedShakeRotation;            // If there will be rotation shake and how much
    public float destroyedShakeDuration;            // How long the shake effect will last for
    public HealthUI healthBar;                      // Reference healthbar UI for Entity

    private int m_ownerId = -1;         // Id of player that owns this tower
    private BoardManager m_board;       // Cached board for fast access
    private float m_spawnTime = -1f;    // Time when we were spawned

    [SerializeField] private AbilityBase m_ability;        // Ability that is unlocked for building this type of tower

    [SerializeField] private AudioClip m_spawnSound;        // Sound to play when spawned
    [SerializeField] private AudioClip m_destroyedSound;    // Sound to play when destroyed
    [SerializeField] private AudioClip m_bulldozedSound;    // Sound to play when bulldozed

    public float LifeSpan { get { return Mathf.Max(0f, Time.time - m_spawnTime); } }

    public BoardManager Board { get { return m_board; } }

    public AbilityBase Ability { get { return m_ability; } }

    void Start()
    {
        m_spawnTime = Time.time;
        healthBar.SetMaxHealth(m_health);   //Set Healthbar UI
    }

    void OnDestroy()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            if (m_board)
                m_board.removeTower(this);
    }

    public void setOwnerId(int playerId)
    {
        m_ownerId = playerId;
        m_board = GameManager.manager.getBoardManager(m_ownerId);
    }

    public void healTower(int amount)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (amount <= 0)
        {
            Debug.LogError("Cannot apply less than zero health to a tower");
            return;
        }

        // No point in healing if already at max health
        if (m_health >= m_maxHealth)
            return;

        if (PhotonNetwork.IsConnected)
            photonView.RPC("HealTowerRPC", RpcTarget.All, amount);
        else
            HealTowerRPC(amount);

        healthBar.SetHealth(m_health);  //Update Healthbar UI

    }

    [PunRPC]
    private void HealTowerRPC(int amount)
    {
        m_health = Mathf.Min(m_maxHealth, m_health + amount);
        healthBar.SetHealth(m_health);  //Update Healthbar UI
    }

    public void takeDamage(int amount, Object instigator)
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (amount <= 0)
        {
            Debug.LogError("Cannot apply less than zero damage to a tower");
            return;
        }

        if (PhotonNetwork.IsConnected)
            photonView.RPC("TakeDamageRPC", RpcTarget.All, amount);
        else
            TakeDamageRPC(amount);

        healthBar.SetHealth(m_health);  //Update Healthbar UI

        if (m_health <= 0)
        {
            AnalyticsHelpers.reportTowerDestroyed(this, instigator ? instigator.name : "Unknown");

            if (CameraEffectsController.instance)
            {
                CameraEffectsController.instance.CameraShake(destroyedShakeIntensity, destroyedShakeDuration, destroyedShakeRotation);
            }
            
            destroyTower(this);
        }
    }

    [PunRPC]
    private void TakeDamageRPC(int amount)
    {
        m_health = Mathf.Max(m_health - amount, 0);
        healthBar.SetHealth(m_health);  //Update Healthbar UI
    }

    public static TowerBase spawnTower(string prefabId, int playerId, Vector3Int tileIndex, Vector3 spawnPos)
    {
        object[] spawnData = new object[2];
        spawnData[0] = playerId;
        spawnData[1] = tileIndex;

        GameObject towerObj = PhotonNetwork.Instantiate(prefabId, spawnPos, Quaternion.identity, 0, spawnData);
        if (!towerObj)
            return null;

        TowerBase tower = towerObj.GetComponent<TowerBase>();
        Assert.IsNotNull(tower);

        PlayerController controller = PlayerController.getController(playerId);
        if (controller)
            controller.onTowerBuilt(tower);

        // This only gets called once per server
        AnalyticsHelpers.reportTowerPlaced(tower, tileIndex);

        return tower;
    }

    public static void destroyTower(TowerBase tower, bool bulldozed = false)
    {
        PhotonView photonView = tower.photonView;
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        if (tower.m_board)
            tower.m_board.removeTower(tower);

        PlayerController controller = PlayerController.getController(tower.m_ownerId);
        if (controller)
            controller.onTowerDestroyed(tower);

        if (PhotonNetwork.IsConnected)
            tower.photonView.RPC("destroyTowerRPC", RpcTarget.All, bulldozed);
        else
            tower.destroyTowerRPC(bulldozed);
    }

    [PunRPC]
    private void destroyTowerRPC(bool bulldozed)
    {
        if (bulldozed)
            SoundEffectsManager.playSoundEffect(m_bulldozedSound, m_board);
        else
            SoundEffectsManager.playSoundEffect(m_destroyedSound, m_board);

        if (!PhotonNetwork.IsConnected || photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
    }

    void IPunInstantiateMagicCallback.OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        setOwnerId((int)instantiationData[0]);

        Vector3Int tile = (Vector3Int)instantiationData[1];
        if (m_board)
        {
            m_board.placeTower(this, tile);
            SoundEffectsManager.playSoundEffect(m_spawnSound, m_board);
        }
    }
}

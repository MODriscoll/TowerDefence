using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using Photon.Pun;

public struct AnalyticsHelpers
{
    public static readonly string specialMonsterDeployed = "specialMiceDeployed";
    public static readonly string monsterDeath = "miceDeath";
    public static readonly string monsterReachedGoal = "miceReachedGoal";
    public static readonly string towerPlaced = "towerPlaced";
    public static readonly string towerDestroyed = "towerDestroyed";
    public static readonly string towerBulldozed = "towerBulldozed";

    private static int currentRound = -1;
    private static Dictionary<string, int> monstersDestroyedThisRound = null;


    /// <summary>
    /// If reports are being tracked. Is set up to only track if in a online match
    /// </summary>
    static public bool isTrackingEvents
    {
        get
        {
#if UNITY_ANALYTICS && !UNITY_EDITOR
            return PhotonNetwork.IsConnected;
#else
            return false;
#endif
        }
    }

    static public void reportSpecialMonsterSpawned(SpecialMonster monster)
    {
#if UNITY_ANALYTICS
        if (!isTrackingEvents)
            return;

        AnalyticsEvent.Custom(specialMonsterDeployed, new Dictionary<string, object>
        {
            { "name", monster.gameObject.name }
        });
#endif
    }

    static public void reportMonsterDeath(MonsterBase monster, string killedBy)
    {
#if UNITY_ANALYTICS
        if (!isTrackingEvents)
            return;

        string monsterName = monster.GetType().ToString();
        if (monstersDestroyedThisRound != null)
        {
            if (monstersDestroyedThisRound.ContainsKey(monsterName))
                monstersDestroyedThisRound[monsterName]++;
            else
                monstersDestroyedThisRound.Add(monsterName, 1);
        }

        bool bIsCommon = monster.GetType().IsSubclassOf(typeof(SpecialMonster));

        AnalyticsEvent.Custom(monsterDeath, new Dictionary<string, object>
        {
            { "name", monsterName },
            { "common", bIsCommon },
            { "killedBy", killedBy },
            { "tilesTravelled", monster.TilesTravelled }
        });
#endif
    }

    static public void reportMonsterReachedGoal(MonsterBase monster)
    {
#if UNITY_ANALYTICS
        if (!isTrackingEvents)
            return;

        bool bIsCommon = monster.GetType().IsSubclassOf(typeof(SpecialMonster));

        AnalyticsEvent.Custom(monsterReachedGoal, new Dictionary<string, object>
        {
            { "name", monster.gameObject.name },
            { "common", bIsCommon }
        });
#endif
    }

    static public void reportTowerPlaced(TowerBase tower, Vector3Int tileIndex)
    {
#if UNITY_ANALYTICS
        if (!isTrackingEvents)
            return;

        AnalyticsEvent.Custom(towerPlaced, new Dictionary<string, object>
        {
            { "name", tower.gameObject.name },
            { "tile", tileIndex }
        });
#endif
    }

    static public void reportTowerDestroyed(TowerBase tower, string destroyedBy)
    {
#if UNITY_ANALYTICS
        if (!isTrackingEvents)
            return;

        AnalyticsEvent.Custom(towerDestroyed, new Dictionary<string, object>
        {
            { "name" , tower.gameObject.name },
            { "destroyedBy", destroyedBy },
            { "lifeSpan", tower.LifeSpan }
        });
#endif
    }

    static public void reportTowerBulldozed(TowerBase tower)
    {
#if UNITY_ANALYTICS
        if (!isTrackingEvents)
            return;

        AnalyticsEvent.Custom(towerBulldozed, new Dictionary<string, object>
        {
            { "name", tower.gameObject.name },
            { "lifeSpan", tower.LifeSpan }
        });
#endif
    }

    static public void startRound(int roundNum)
    {
        currentRound = roundNum;
        monstersDestroyedThisRound = new Dictionary<string, int>();
    }

    static public void finishRound()
    {
#if UNITY_ANALYTICS
        if (!isTrackingEvents)
            return;

        AnalyticsEvent.Custom("monstersKilledSet", new Dictionary<string, object>
        {
            { "round" , currentRound },
            { "map", monstersDestroyedThisRound }
        });
#endif

        currentRound = -1;
        monstersDestroyedThisRound = null;
    }
}

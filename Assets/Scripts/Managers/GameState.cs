using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameState
{
    // Progressed Player Stats
    public float MaxHealth;
    public float MoveSpeed;
    public float AttackInterval;
    public float Cooldown;
    public float LockOnRadius;
    public float CurrentDamage;
    
    // PowerUp Counts
    public Dictionary<PowerUpType, int> PowerUpCounts = new Dictionary<PowerUpType, int>();
    
    // Arsenal State
    public List<ArrowUsageState> Arrows;

    public GameState()
    {
        Arrows = new List<ArrowUsageState>();
        // Dictionary won't serialize nicely with Newtonsoft by default sometimes or is messy in JSON
        // but it's fine for runtime. For JSON, I'll use a List of KeyValue pairs if needed.
    }

    public void SyncFromMetadata(PlayerData p, List<ArrowStats> arsenal)
    {
        MaxHealth = p.MaxHealth;
        MoveSpeed = p.MoveSpeed;
        AttackInterval = p.AttackInterval;
        Cooldown = p.Cooldown;
        LockOnRadius = p.LockOnRadius;
        CurrentDamage = p.BaseDamage;
        
        PowerUpCounts.Clear();
        foreach (PowerUpType type in Enum.GetValues(typeof(PowerUpType)))
        {
            PowerUpCounts[type] = p.InitialPowerUpCount;
        }
    
        Arrows.Clear();
        if (arsenal != null)
        {
            foreach (var a in arsenal)
            {
                Arrows.Add(new ArrowUsageState { ArrowType = a.ArrowType, LastUsedTime = -100f });
            }
        }
    }

    public int GetPowerUpCount(PowerUpType type)
    {
        if (PowerUpCounts.ContainsKey(type)) return PowerUpCounts[type];
        return 0;
    }

    public void UsePowerUp(PowerUpType type)
    {
        if (PowerUpCounts.ContainsKey(type) && PowerUpCounts[type] > 0)
        {
            PowerUpCounts[type]--;
        }
    }
}

[Serializable]
public class ArrowUsageState
{
    public string ArrowType;
    public float LastUsedTime;
}

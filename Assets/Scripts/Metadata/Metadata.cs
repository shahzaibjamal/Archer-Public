using System.Collections.Generic;

[System.Serializable]
public class Metadata
{
    public List<LevelData> Levels;
    public PlayerData PlayerStats;
    public List<EnemyData> EnemyStats;
    public List<ArrowStats> Arsenal;
    public List<ThrowableData> Throwables;
}

[System.Serializable]
public class ThrowableData
{
    public string Type;
    public int MinLevel;
    public float Duration;
    public float EffectRadius;
    public float DamagePerSecond;
    public float SlowMultiplier;
    public bool BlocksEnemies;
}

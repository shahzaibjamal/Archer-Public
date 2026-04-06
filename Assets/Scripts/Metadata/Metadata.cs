using System.Collections.Generic;

[System.Serializable]
public class Metadata
{
    public List<LevelData> Levels;
    public PlayerData PlayerStats;
    public List<EnemyData> EnemyStats;
    public List<ArrowStats> Arsenal;
}

using System.Collections.Generic;

[System.Serializable]
public class LevelWaveData
{
    public List<LevelEnemySpawn> EnemySpawns;
    public float PreWaveDelay = 2f;
}

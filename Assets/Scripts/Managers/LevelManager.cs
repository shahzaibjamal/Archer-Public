using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

[System.Serializable]
public class LevelDataList
{
    public List<LevelDataPayload> levels;
}

[System.Serializable]
public class LevelDataPayload
{
    public int levelId;
    public List<EnemySpawnData> enemySpawns;
}

[System.Serializable]
public class EnemySpawnData
{
    public string enemyType;
    public int count;
    public int rewardGold;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject meleePrefab;
    [SerializeField] private GameObject rangedPrefab;
    [SerializeField] private GameObject healerPrefab;
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawning Setup")]
    [SerializeField] private Transform[] spawnPoints;

    private LevelDataList allLevels;

    private void Awake()
    {
        Instance = this;
        LoadLevelData();
    }

    private void LoadLevelData()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("LevelData");
        if (jsonText != null)
        {
            allLevels = JsonConvert.DeserializeObject<LevelDataList>(jsonText.text);
            Debug.Log("Successfully loaded sequential Level JSON utilizing Newtonsoft!");
        }
    }

    public void StartLevel(int levelId)
    {
        if (allLevels == null) return;
        var level = allLevels.levels.Find(l => l.levelId == levelId);
        if (level == null) return;

        foreach (var spawnData in level.enemySpawns)
        {
            for (int i = 0; i < spawnData.count; i++)
            {
                SpawnEnemy(spawnData.enemyType, spawnData.rewardGold);
            }
        }
    }

    private void SpawnEnemy(string typeStr, int reward)
    {
        GameObject prefab = typeStr switch
        {
            "Melee" => meleePrefab,
            "Ranged" => rangedPrefab,
            "Healer" => healerPrefab,
            "Tank" => tankPrefab,
            "Boss" => bossPrefab,
            _ => meleePrefab
        };

        if (prefab != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemyObj = Instantiate(prefab, sp.position, sp.rotation);

            if (enemyObj.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.SetReward(reward);
            }
        }
    }
}

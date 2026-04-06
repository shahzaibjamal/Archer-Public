using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject meleePrefab;
    [SerializeField] private GameObject rangedPrefab;
    [SerializeField] private GameObject healerPrefab;
    [SerializeField] private GameObject tankPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Spawning Setup")]
    [SerializeField] private Transform[] spawnPoints;

    private List<Enemy> _activeEnemies = new List<Enemy>();
    private LevelData _currentLevel;
    private int _currentWaveIndex;

    public IReadOnlyList<Enemy> ActiveEnemies => _activeEnemies;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.EnemySpawned += AddEnemy;
        GameEvents.EnemyDestroyed += RemoveEnemy;
    }

    private void OnDisable()
    {
        GameEvents.EnemySpawned -= AddEnemy;
        GameEvents.EnemyDestroyed -= RemoveEnemy;
    }

    private void AddEnemy(Enemy enemy)
    {
        if (!_activeEnemies.Contains(enemy)) _activeEnemies.Add(enemy);
    }

    private void RemoveEnemy(Enemy enemy)
    {
        if (_activeEnemies.Contains(enemy))
        {
            _activeEnemies.Remove(enemy);
            CheckWaveCompletion();
        }
    }

    public Enemy GetHealTarget(Enemy requester, float maxRange)
    {
        Enemy bestTarget = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in _activeEnemies)
        {
            if (enemy == requester) continue;
            float dist = Vector3.Distance(requester.transform.position, enemy.transform.position);
            if (dist < closestDist && dist <= maxRange)
            {
                closestDist = dist;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }

    /// <summary>
    /// Returns a coordinated world position for an enemy to target based on its type and count.
    /// This prevents enemies from overlapping at the exact player position.
    /// </summary>
    public Vector3 GetCombatPosition(Enemy requester, Transform playerTransform, float targetDistance)
    {
        if (playerTransform == null) return requester.transform.position;

        // Group enemies of the same type targeting the player
        List<Enemy> peers = _activeEnemies.FindAll(e => e.enemyTypeName == requester.enemyTypeName);
        int myIndex = peers.IndexOf(requester);
        int totalCount = peers.Count;

        if (myIndex == -1) return playerTransform.position;

        Vector3 dirToPlayer = (requester.transform.position - playerTransform.position).normalized;
        if (dirToPlayer == Vector3.zero) dirToPlayer = Vector3.forward;

        // Determine spacing based on enemy type
        float spreadAngle = requester.enemyTypeName == "Ranged" ? 45f : 35f;
        
        // Calculate the 'offset' index relative to the center (0, -1, 1, -2, 2...)
        float offsetMultiplier = 0;
        if (totalCount > 1)
        {
            // Center the formation on the player
            float startAngle = -(totalCount - 1) * spreadAngle * 0.5f;
            offsetMultiplier = startAngle + (myIndex * spreadAngle);
        }

        // Rotate the direction to the requester by the calculated offset
        Quaternion rotation = Quaternion.AngleAxis(offsetMultiplier, Vector3.up);
        Vector3 finalDir = rotation * dirToPlayer;

        return playerTransform.position + finalDir * targetDistance;
    }

    private void CheckWaveCompletion()
    {
        if (_activeEnemies.Count == 0 && _currentLevel != null)
        {
            _currentWaveIndex++;
            if (_currentWaveIndex < _currentLevel.Waves.Count)
            {
                StartCoroutine(SpawnWaveWithDelay(_currentLevel.Waves[_currentWaveIndex]));
            }
            else
            {
                Debug.Log("Level Complete!");
                _currentLevel = null;
            }
        }
    }

    public void StartLevel(int levelId)
    {
        if (DataManager.Instance == null || DataManager.Instance.Metadata == null) return;
        
        _currentLevel = DataManager.Instance.Metadata.Levels.Find(l => l.LevelId == levelId);
        if (_currentLevel == null || _currentLevel.Waves == null || _currentLevel.Waves.Count == 0) return;

        _currentWaveIndex = 0;
        StopAllCoroutines();
        StartCoroutine(SpawnWaveWithDelay(_currentLevel.Waves[_currentWaveIndex]));
    }

    private System.Collections.IEnumerator SpawnWaveWithDelay(LevelWaveData wave)
    {
        yield return new WaitForSeconds(wave.PreWaveDelay);

        foreach (var spawnData in wave.EnemySpawns)
        {
            for (int i = 0; i < spawnData.Count; i++)
            {
                SpawnEnemy(spawnData.EnemyType, spawnData.RewardGold);
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
                enemy.enemyTypeName = typeStr;
            }
        }
    }
}

using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

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

    public Vector3 GetCombatPosition(Enemy requester, Transform playerTransform, float targetDistance)
    {
        if (playerTransform == null) return requester.transform.position;

        // Group enemies of the same type targeting the player using Enum comparison
        List<Enemy> peers = _activeEnemies.FindAll(e => e.enemyType == requester.enemyType);
        int myIndex = peers.IndexOf(requester);
        int totalCount = peers.Count;

        if (myIndex == -1) return playerTransform.position;

        Vector3 dirToPlayer = (requester.transform.position - playerTransform.position).normalized;
        if (dirToPlayer == Vector3.zero) dirToPlayer = Vector3.forward;

        float spreadAngle = (requester.enemyType == EnemyType.Ranged || requester.enemyType == EnemyType.Healer) ? 45f : 35f;
        
        float offsetMultiplier = 0;
        if (totalCount > 1)
        {
            float startAngle = -(totalCount - 1) * spreadAngle * 0.5f;
            offsetMultiplier = startAngle + (myIndex * spreadAngle);
        }

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

    private async void SpawnEnemy(EnemyType type, int reward)
    {
        if (AssetLoader.Instance == null || spawnPoints == null || spawnPoints.Length <= 0) return;

        EnemyData stats = DataManager.Instance.GetEnemyStats(type);
        if (stats == null || string.IsNullOrEmpty(stats.PrefabName)) return;

        string key = stats.PrefabName;
        GameObject prefab = await AssetLoader.Instance.LoadAsset(key);

        if (prefab != null)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemyObj = Instantiate(prefab, sp.position, sp.rotation);

            if (enemyObj.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.SetReward(reward);
                enemy.enemyType = type;
            }
        }
    }
}

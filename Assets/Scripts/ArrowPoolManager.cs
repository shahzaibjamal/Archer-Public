using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ArrowPoolManager : MonoBehaviour
{
    public static ArrowPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;

    private Dictionary<ArrowType, Queue<BaseArrow>> _pools = new Dictionary<ArrowType, Queue<BaseArrow>>();
    private Dictionary<ArrowType, GameObject> _prefabs = new Dictionary<ArrowType, GameObject>();
    public List<BaseArrow> ActiveArrows { get; private set; } = new List<BaseArrow>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Initialize the pools for each arrow type
        foreach (ArrowType type in System.Enum.GetValues(typeof(ArrowType)))
        {
            _pools[type] = new Queue<BaseArrow>();
            LoadArrowPrefab(type);
        }
    }

    private async void LoadArrowPrefab(ArrowType type)
    {
        if (AssetLoader.Instance == null) return;

        if (DataManager.Instance == null || DataManager.Instance.Metadata == null) return;
        var stats = DataManager.Instance.Metadata.Arsenal.Find(a => a.ArrowType == type.ToString());
        if (stats == null || string.IsNullOrEmpty(stats.PrefabName)) return;

        string key = stats.PrefabName;
        GameObject prefab = await AssetLoader.Instance.LoadAsset(key);

        if (prefab != null)
        {
            _prefabs[type] = prefab;
            // Pre-warm the pool
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewArrow(type);
            }
            Debug.Log($"[ArrowPool] Successfully loaded and pooled {type} via AssetLoader.");
        }
    }

    private BaseArrow CreateNewArrow(ArrowType type)
    {
        if (!_prefabs.ContainsKey(type)) return null;

        var arrowObj = Instantiate(_prefabs[type], transform);
        arrowObj.SetActive(false);
        var arrow = arrowObj.GetComponent<BaseArrow>();
        arrow.OnDespawn += RecycleArrow;
        _pools[type].Enqueue(arrow);
        return arrow;
    }

    public void FireArrow(ArrowType type, Vector3 spawnPos, Quaternion spawnRot, float speed, float range, float damageAmount, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        if (!_pools.ContainsKey(type) || _pools[type].Count == 0)
        {
            if (CreateNewArrow(type) == null) return;
        }

        if (_pools[type].Count == 0) return;

        var arrow = _pools[type].Dequeue();
        arrow.gameObject.SetActive(true);
        arrow.transform.position = spawnPos;
        arrow.transform.rotation = spawnRot;

        arrow.Launch(speed, range, damageAmount, targetPos, isEnemyProjectile);
        ActiveArrows.Add(arrow);
    }

    private void RecycleArrow(BaseArrow arrow)
    {
        arrow.gameObject.SetActive(false);
        ActiveArrows.Remove(arrow);
        if (_pools.ContainsKey(arrow.type))
        {
            _pools[arrow.type].Enqueue(arrow);
        }
    }
}

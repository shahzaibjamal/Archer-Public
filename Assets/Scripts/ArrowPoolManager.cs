using System.Collections.Generic;
using UnityEngine;

public class ArrowPoolManager : MonoBehaviour
{
    public static ArrowPoolManager Instance { get; private set; }

    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int poolSize = 25;

    public List<BaseArrow> ActiveArrows { get; private set; } = new List<BaseArrow>();

    private Queue<BaseArrow> _arrowPool = new Queue<BaseArrow>();

    private void Awake()
    {
        // Make this a true Singleton disjoint from the Player!
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            var arrowObj = Instantiate(arrowPrefab);
            arrowObj.SetActive(false);

            var arrow = arrowObj.GetComponent<BaseArrow>();
            arrow.OnDespawn += RecycleArrow;
            _arrowPool.Enqueue(arrow);
        }
    }

    public void FireArrow(Vector3 spawnPos, Quaternion spawnRot, float speed, float range, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        if (_arrowPool.Count == 0) return;

        var arrow = _arrowPool.Dequeue();
        arrow.gameObject.SetActive(true);
        arrow.transform.position = spawnPos;
        arrow.transform.rotation = spawnRot;

        arrow.Launch(speed, range, targetPos, isEnemyProjectile);
        ActiveArrows.Add(arrow);
    }

    private void RecycleArrow(BaseArrow arrow)
    {
        arrow.gameObject.SetActive(false);
        ActiveArrows.Remove(arrow);
        _arrowPool.Enqueue(arrow);
    }
}

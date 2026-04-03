using System.Collections.Generic;
using UnityEngine;

public class ArrowPoolManager : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int poolSize = 5;

    private Queue<BaseArrow> _arrowPool = new Queue<BaseArrow>();

    private void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var arrowObj = Instantiate(arrowPrefab);
            arrowObj.SetActive(false);
            var arrow = arrowObj.GetComponent<BaseArrow>();

            // Hook into arrow’s OnDespawn action
            arrow.OnDespawn += RecycleArrow;
            _arrowPool.Enqueue(arrow);
        }
    }

    public void FireArrow(float speed, float range, Vector3? targetPos = null)
    {
        if (_arrowPool.Count == 0) return;

        var arrow = _arrowPool.Dequeue();
        arrow.gameObject.SetActive(true);
        arrow.transform.position = firePoint.position;
        arrow.transform.rotation = firePoint.rotation;

        arrow.Launch(speed, range, targetPos);
    }

    private void RecycleArrow(BaseArrow arrow)
    {
        arrow.gameObject.SetActive(false);
        _arrowPool.Enqueue(arrow);
    }
}

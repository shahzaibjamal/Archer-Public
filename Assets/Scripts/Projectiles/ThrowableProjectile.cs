using UnityEngine;

public class ThrowableProjectile : MonoBehaviour
{
    private ThrowableData data;
    private Vector3 targetPos;
    private float arcHeight = 5f;
    private float duration = 1.25f;
    private float timer;
    private Vector3 startPos;

    public void Launch(ThrowableData data, Vector3 target)
    {
        this.data = data;
        this.targetPos = target;
        this.startPos = transform.position;
        this.timer = 0f;
        Destroy(gameObject, duration + 0.1f);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = timer / duration;

        if (progress >= 1f)
        {
            SpawnPuddle();
            Destroy(gameObject);
            return;
        }

        Vector3 nextPos = Vector3.Lerp(startPos, targetPos, progress);
        nextPos.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
        transform.position = nextPos;
        transform.rotation = Quaternion.LookRotation(nextPos - transform.position);
    }

    private void SpawnPuddle()
    {
        var puddleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        puddleObj.transform.position = targetPos;
        puddleObj.transform.rotation = Quaternion.identity;
        
        var p = puddleObj.AddComponent<Puddle>();
        p.Initialize(data);
    }
}

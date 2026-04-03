using UnityEngine;

public class PiercingArrow : BaseArrow
{
    [SerializeField] private float overridingRange = 30f;

    public override void Launch(float speed, float range, Vector3? targetPos = null)
    {
        base.Launch(speed, overridingRange, targetPos);
    }

    private void Update()
    {
        transform.position += moveDir * speed * Time.deltaTime;
        CheckRange();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeDamage(damage);
            if (hitVfxPrefab != null) Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            if (hitSfx != null) AudioSource.PlayClipAtPoint(hitSfx, transform.position);
        }
    }

    protected override void OnMaxRangeReached()
    {
        Poof();
    }
}

using UnityEngine;

public class PiercingArrow : BaseArrow
{
    [SerializeField] private float overridingRange = 30f;

    public override void Launch(float speed, float range, float damageAmount, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        base.Launch(speed, overridingRange, damageAmount, targetPos, isEnemyProjectile);
    }

    private void Update()
    {
        transform.position += moveDir * speed * Time.deltaTime;
        CheckRange();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsValidHit(other, out var damageable))
        {
            damageable.TakeDamage(damage);
            // Optionally play hit effects per hit organically
            if (hitVfxPrefab != null) Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            if (hitSfx != null) AudioSource.PlayClipAtPoint(hitSfx, transform.position);
        }
    }

    protected override void OnMaxRangeReached()
    {
        Poof();
    }
}

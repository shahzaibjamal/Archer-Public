using UnityEngine;

public class NormalArrow : BaseArrow
{
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
            Poof();
        }
    }

    protected override void OnMaxRangeReached()
    {
        Poof();
    }
}

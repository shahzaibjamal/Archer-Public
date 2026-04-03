using UnityEngine;

public class PiercingArrow : BaseArrow
{
    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
        CheckRange();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeDamage(damage);
        }
    }

    protected override void OnMaxRangeReached()
    {
        Poof();
    }
}

using UnityEngine;

public class NormalArrow : BaseArrow
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
            Poof();
        }
    }

    protected override void OnMaxRangeReached()
    {
        Poof();
    }
}

using UnityEngine;

public class ProjectileArrow : BaseArrow
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float gravityMultiplier = 1f;

    private Vector3 velocity;

    public override void Launch(float speed, float range, Vector3? targetPos = null)
    {
        base.Launch(speed, range, targetPos);

        // Time to travel horizontal 'range' is range/speed
        float flightTime = range / speed;
        // Gravity to reach original Y exactly at flightTime
        float actualGravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
        float vy = 0.5f * actualGravity * flightTime;
        
        velocity = moveDir * speed + Vector3.up * vy;
    }

    private void Update()
    {
        velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (velocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(velocity);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Enemy>(out var enemy))
        {
            enemy.TakeDamage(damage);
            Poof();
            return;
        }

        // Check ground collision to poof
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            Poof();
        }
    }

    protected override void OnMaxRangeReached()
    {
        // Ignored for projectile arrow, it poofs on ground
    }
}

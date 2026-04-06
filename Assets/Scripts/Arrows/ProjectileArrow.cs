using UnityEngine;

public class ProjectileArrow : BaseArrow
{
    [Header("Physics Settings")]
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected float gravityMultiplier = 1f;
    [SerializeField] protected float targetHitHeight = 0.5f;
    [SerializeField] protected float arcHeight = 3.0f;

    protected Vector3 _velocity;
    protected bool _isInitialized = false;

    public override void Launch(float speed, float range, float damageAmount, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        base.Launch(speed, range, damageAmount, targetPos, isEnemyProjectile);

        if (targetPos.HasValue)
        {
            _velocity = GetBallisticVelocity(transform.position, targetPos.Value, out _);
        }
        else
        {
            // Simple arc logic for non-targeted shots
            float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            float v0y = Mathf.Sqrt(2 * g * arcHeight);
            _velocity = (transform.forward * speed) + (Vector3.up * v0y);
        }

        _isInitialized = true;
    }

    protected Vector3 GetBallisticVelocity(Vector3 start, Vector3 end, out float flightTime)
    {
        Vector3 target = new Vector3(end.x, end.y + targetHitHeight, end.z);
        float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
        if (g < 0.1f) g = 0.1f;

        float h = Mathf.Max(start.y, target.y) + arcHeight;
        float v0y = Mathf.Sqrt(2 * g * Mathf.Max(0.1f, h - start.y));

        float timeToPeak = v0y / g;
        float timeFromPeak = Mathf.Sqrt(2 * g * Mathf.Max(0.1f, h - target.y)) / g;
        flightTime = timeToPeak + timeFromPeak;

        Vector3 diff = new Vector3(target.x - start.x, 0, target.z - start.z);
        float horizontalDist = diff.magnitude;
        float horizontalSpeed = flightTime > 0.01f ? horizontalDist / flightTime : 0f;

        return (diff.normalized * horizontalSpeed) + Vector3.up * v0y;
    }

    protected virtual void Update()
    {
        if (!_isInitialized) return;

        _velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(_velocity);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (IsValidHit(other, out var damageable))
        {
            damageable.TakeDamage(damage);
            Poof();
        }
        else if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            Poof();
        }
    }

    protected override void OnMaxRangeReached()
    {
    }

}
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

    public override void Launch(float speed, float range, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        base.Launch(speed, range, targetPos, isEnemyProjectile);

        if (targetPos.HasValue)
        {
            Vector3 start = transform.position;
            Vector3 target = new Vector3(targetPos.Value.x, targetPos.Value.y + targetHitHeight, targetPos.Value.z);

            float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            if (g < 0.1f) g = 0.1f; // Prevent div by zero

            // Ensure we always have an upward arc by picking a peak height above both shooter and target
            float h = Mathf.Max(start.y, target.y) + arcHeight;
            
            // v0y = sqrt(2 * g * (h - y0))
            float v0y = Mathf.Sqrt(2 * g * Mathf.Max(0.1f, h - start.y));
            
            // Time to reach peak + time from peak to target
            float timeToPeak = v0y / g;
            float timeFromPeak = Mathf.Sqrt(2 * g * Mathf.Max(0.1f, h - target.y)) / g;
            float totalTime = timeToPeak + timeFromPeak;

            Vector3 diff = new Vector3(target.x - start.x, 0, target.z - start.z);
            float horizontalDist = diff.magnitude;

            // Horizontal speed is derived from totalTime to hit the spot
            float horizontalSpeed = horizontalDist / totalTime;
            
            _velocity = (diff.normalized * horizontalSpeed) + Vector3.up * v0y;
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

    private void Update()
    {
        if (!_isInitialized) return;

        _velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;

        if (_velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(_velocity);
    }

    private void OnTriggerEnter(Collider other)
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
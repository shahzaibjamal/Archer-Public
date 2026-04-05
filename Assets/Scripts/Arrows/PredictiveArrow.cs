using UnityEngine;
using UnityEngine.AI;

public class PredictiveBallisticArrow : BaseArrow
{
    [Header("Prediction & Difficulty")]
    [Range(0f, 1f)] public float predictionSkill = 1.0f;
    [Range(0f, 5f)] public float mechanicalSpread = 0.0f;

    [Header("Ballistic Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private float arcHeight = 3.0f;
    [SerializeField] private float targetHitHeight = 0.8f;

    private Vector3 _velocity;
    private bool _isInitialized = false;

    public override void Launch(float speed, float range, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        base.Launch(speed, range, targetPos, isEnemyProjectile);

        if (!targetPos.HasValue)
        {
            _velocity = transform.forward * speed;
            _isInitialized = true;
            return;
        }

        Vector3 startPos = transform.position;
        Vector3 playerPos = targetPos.Value;
        Vector3 finalTargetPos = playerPos;

        if (isEnemyProjectile)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 pVel = GetTargetVelocity(player);

                if (pVel.sqrMagnitude > 0.01f)
                {
                    // Pass 1: Initial Time
                    float t1 = CalculateFlightTime(startPos, playerPos);

                    // Pass 2: Refined Time (Where they will be after t1)
                    Vector3 guess1 = playerPos + (pVel * t1);
                    float t2 = CalculateFlightTime(startPos, guess1);

                    // Pass 3: Final Precision (Corrects for moving away/towards shooter)
                    Vector3 guess2 = playerPos + (pVel * t2);
                    float t3 = CalculateFlightTime(startPos, guess2);

                    finalTargetPos = playerPos + (pVel * t3 * predictionSkill);
                }
            }

            // Mechanical Spread
            if (mechanicalSpread > 0.01f)
            {
                Vector3 spread = Random.insideUnitSphere * mechanicalSpread;
                spread.y = 0;
                finalTargetPos += spread;
            }
        }

        _velocity = CalculateBallisticVelocity(startPos, finalTargetPos);
        _isInitialized = true;
    }

    private float CalculateFlightTime(Vector3 start, Vector3 end)
    {
        float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
        if (g < 0.1f) g = 0.1f;

        float targetY = end.y + targetHitHeight;
        // Ensure arcHeight is always relative to the highest point
        float h = Mathf.Max(start.y, targetY) + arcHeight;

        float hStart = Mathf.Max(0.01f, h - start.y);
        float hTarget = Mathf.Max(0.01f, h - targetY);

        return Mathf.Sqrt(2 * hStart / g) + Mathf.Sqrt(2 * hTarget / g);
    }

    private Vector3 CalculateBallisticVelocity(Vector3 start, Vector3 end)
    {
        Vector3 target = new Vector3(end.x, end.y + targetHitHeight, end.z);
        float g = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
        float h = Mathf.Max(start.y, target.y) + arcHeight;

        float v0y = Mathf.Sqrt(2 * g * (h - start.y));
        float totalTime = CalculateFlightTime(start, end);

        if (totalTime <= 0) return transform.forward;

        Vector3 horizontalDiff = new Vector3(target.x - start.x, 0, target.z - start.z);
        // This is the line that determines if it "drops short"
        Vector3 vHorizontal = horizontalDiff / totalTime;

        return vHorizontal + Vector3.up * v0y;
    }

    private Vector3 GetTargetVelocity(GameObject target)
    {
        // IMPORTANT: Ensure your Player script exposes these correctly
        if (target.TryGetComponent<PlayerController>(out var pc))
        {
            // If you are using _agent.Move(move * speed), your velocity is:
            // The directional input multiplied by the speed.
            Vector2 input = pc.GetMoveInput();
            Vector3 velocity = new Vector3(input.x, 0, input.y) * pc.GetMoveSpeed();
            Debug.LogError(velocity);
            return velocity;
        }

        if (target.TryGetComponent<NavMeshAgent>(out var agent))
            return agent.velocity;

        return Vector3.zero;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        _velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        transform.position += _velocity * Time.deltaTime;

        if (_velocity.sqrMagnitude > 0.001f)
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

    protected override void OnMaxRangeReached() { Poof(); }
}
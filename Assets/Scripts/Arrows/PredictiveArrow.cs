using UnityEngine;
using UnityEngine.AI;

public class PredictiveBallisticArrow : ProjectileArrow
{
    [Header("Bokoblin Prediction")]
    [Range(0f, 1f)] public float predictionSkill = 1.0f;
    [Range(0f, 5f)] public float mechanicalSpread = 0.4f;

    public override void Launch(float speed, float range, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        // Call base to set up initial flags and fallback velocity if no target
        base.Launch(speed, range, targetPos, isEnemyProjectile);

        if (!targetPos.HasValue) return;

        Vector3 startPos = transform.position;
        Vector3 playerPos = targetPos.Value;
        Vector3 finalTargetPos = playerPos;

        if (isEnemyProjectile)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 pVel = GetTargetVelocity(player);

                if (pVel.sqrMagnitude > 0.05f)
                {
                    // Pass 1: Initial Time
                    _ = GetBallisticVelocity(startPos, playerPos, out float t1);

                    // Pass 2: Refined Time (Where they will be after t1)
                    Vector3 guess1 = playerPos + (pVel * t1);
                    _ = GetBallisticVelocity(startPos, guess1, out float t2);

                    // Pass 3: Final Precision (Corrects for moving away/towards shooter)
                    Vector3 guess2 = playerPos + (pVel * t2);
                    _ = GetBallisticVelocity(startPos, guess2, out float t3);

                    finalTargetPos = playerPos + (pVel * t3 * predictionSkill);
                }
            }

            // Apply Mechanical Spread to the final calculated point
            if (mechanicalSpread > 0.01f)
            {
                Vector3 spread = Random.insideUnitSphere * mechanicalSpread;
                spread.y = 0;
                finalTargetPos += spread;
            }
        }

        // Finalize ballistic velocity using the shared solver
        _velocity = GetBallisticVelocity(startPos, finalTargetPos, out _);
        _isInitialized = true;
    }

    private Vector3 GetTargetVelocity(GameObject target)
    {
        if (target.TryGetComponent<PlayerController>(out var pc))
        {
            Vector2 input = pc.GetMoveInput();
            return new Vector3(input.x, 0, input.y) * pc.GetMoveSpeed();
        }

        if (target.TryGetComponent<NavMeshAgent>(out var agent))
            return agent.velocity;

        return Vector3.zero;
    }

    protected override void OnMaxRangeReached() { Poof(); }
}
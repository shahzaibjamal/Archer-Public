using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Mechanics")]
    [SerializeField] private ArrowType arrowType = ArrowType.Predictive;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 2.5f;
    [SerializeField] private float shootHitDelay = 0.4f;
    [SerializeField] private float fleeRange = 5f;
    private float _shootTimer;

    protected override void BehaviorUpdate()
    {
        if (playerTarget == null) return;

        Vector3 flatTargetPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float dist = Vector3.Distance(transform.position, flatTargetPos);

        if (dist > aggroRange) return;

        if (dist < fleeRange)
        {
            // Flee: Turn away from player and run forward
            Vector3 fleeDir = (transform.position - flatTargetPos).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * 3f;

            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(fleeTarget);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, fleeTarget, moveSpeed * Time.deltaTime);
            }
            transform.LookAt(new Vector3(fleeTarget.x, transform.position.y, fleeTarget.z));
        }
        else if (dist > attackRange || !HasLineOfSight())
        {
            // Approach: Move towards player if too far OR if player is behind an obstacle
            // Fan out using coordinated positions at optimal firing distance
            Vector3 destination = playerTarget.position;
            if (BattleManager.Instance != null)
            {
                destination = BattleManager.Instance.GetCombatPosition(this, playerTarget, attackRange * 0.9f);
            }

            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(destination);
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
                transform.LookAt(flatTargetPos);
            }
        }
        else
        {
            // Within attack range AND has LOS: Stay static and just look at player
            if (agent != null) agent.isStopped = true;
            transform.LookAt(flatTargetPos);
        }

        _shootTimer -= Time.deltaTime;
        if (_shootTimer <= 0 && dist <= attackRange && HasLineOfSight())
        {
            Vector3 dirToPlayer = (flatTargetPos - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, dirToPlayer);
            bool isFacingPlayer = dot > 0.7f; // Roughly 45 degrees

            if (isFacingPlayer)
            {
                if (animator != null) animator.SetTrigger("Attack01");
                _shootTimer = shootInterval;
                LockAttackState(1f, shootHitDelay, Shoot);
            }
        }
    }

    public override bool HasLineOfSight()
    {
        if (playerTarget == null) return false;
        Transform fp = firePoint != null ? firePoint : transform;
        Vector3 start = fp.position;
        // Strict horizontal check matching the corrected projectile trajectory from Shoot()
        Vector3 end = new Vector3(playerTarget.position.x, start.y, playerTarget.position.z);
        return !Physics.Linecast(start, end, obstacleLayer);
    }

    private void Shoot()
    {
        if (playerTarget != null && ArrowPoolManager.Instance != null)
        {
            Transform fp = firePoint != null ? firePoint : transform;

            // Simple fire: Just point at the target and tell the arrow where it is
            Vector3 targetPos = playerTarget.position;
            Vector3 targetDir = (targetPos - fp.position).normalized;

            // Let the factory handle the arrow type based on Addressables
            ArrowPoolManager.Instance.FireArrow(arrowType, fp.position, Quaternion.LookRotation(targetDir), 15f, aggroRange, damage, targetPos, true);
        }
    }
}

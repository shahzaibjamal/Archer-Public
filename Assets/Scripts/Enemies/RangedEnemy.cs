using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Mechanics")]
    [SerializeField] private BaseArrow projectilePrefab;
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
            Vector3 fleeTarget = transform.position + fleeDir * 2f;
            transform.LookAt(new Vector3(fleeTarget.x, transform.position.y, fleeTarget.z));
            transform.position = Vector3.MoveTowards(transform.position, fleeTarget, moveSpeed * Time.deltaTime);
        }
        else if (dist > attackRange)
        {
            // Approach: Look at and move towards player
            transform.LookAt(flatTargetPos);
            transform.position = Vector3.MoveTowards(transform.position, flatTargetPos, moveSpeed * Time.deltaTime);
        }
        else
        {
            // Within attack range: Stay static and just look at player
            transform.LookAt(flatTargetPos);
        }

        _shootTimer -= Time.deltaTime;
        if (_shootTimer <= 0 && dist <= attackRange)
        {
            if (animator != null) animator.SetTrigger("Attack01");
            _shootTimer = shootInterval;
            LockAttackState(1f, shootHitDelay, Shoot); // Halts FSM stagger states gracefully while securely scheduling arrow payload 
        }
    }

    private void Shoot()
    {
        if (projectilePrefab != null && playerTarget != null)
        {
            Transform fp = firePoint != null ? firePoint : transform;
            
            // Force a perfectly horizontal (Y=0) launch trajectory at the firePoint's height
            Vector3 targetFlattened = new Vector3(playerTarget.position.x, fp.position.y, playerTarget.position.z);
            Vector3 targetDir = (targetFlattened - fp.position).normalized;
            
            // Set launch transform and pass non-homing target position in that horizontal direction
            Vector3 targetPos = fp.position + targetDir * 10f;
            
            BaseArrow arrow = Instantiate(projectilePrefab, fp.position, Quaternion.LookRotation(targetDir));
            arrow.Launch(15f, aggroRange, targetPos, true);
        }
    }
}

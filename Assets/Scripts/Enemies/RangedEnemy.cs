using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Mechanics")]
    [SerializeField] private BaseArrow projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 2.5f;
    [SerializeField] private float shootHitDelay = 0.4f;
    private float _shootTimer;

    protected override void BehaviorUpdate()
    {
        if (playerTarget == null) return;

        Vector3 flatTargetPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float dist = Vector3.Distance(transform.position, flatTargetPos);

        if (dist > aggroRange) return;

        transform.LookAt(flatTargetPos);

        if (dist < attackRange - 4f)
        {
            transform.position = Vector3.MoveTowards(transform.position, flatTargetPos, -moveSpeed * Time.deltaTime);
        }
        else if (dist > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, flatTargetPos, moveSpeed * Time.deltaTime);
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
        if (projectilePrefab != null)
        {
            Transform fp = firePoint != null ? firePoint : transform;
            Vector3 flatTarget = new Vector3(playerTarget.position.x, fp.position.y, playerTarget.position.z);
            
            // Decoupled explicitly from universal ArrowManager
            BaseArrow arrow = Instantiate(projectilePrefab, fp.position, fp.rotation);
            arrow.Launch(15f, aggroRange, flatTarget, true);
        }
    }
}

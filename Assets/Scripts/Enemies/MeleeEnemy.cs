using UnityEngine;

public class MeleeEnemy : Enemy
{
    [Header("Melee Combat")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackHitDelay = 0.5f;
    private float _attackTimer;

    protected override void Start()
    {
        base.Start();
    }

    protected override void BehaviorUpdate()
    {
        if (playerTarget == null) return;
        
        // FLATTEN THE TARGET Y COORDINATE TO ZERO OUT GROUND-CLIPPING
        Vector3 flatTargetPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float dist = Vector3.Distance(transform.position, flatTargetPos);

        if (dist <= aggroRange && dist > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, flatTargetPos, moveSpeed * Time.deltaTime);
            transform.LookAt(flatTargetPos);
        }
        else if (dist <= attackRange)
        {
            // Within strike range! Turn to face but do not MoveTowards physically into them!
            transform.LookAt(flatTargetPos);
            
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0)
            {
                if (animator != null) animator.SetTrigger(Random.value > 0.5f ? "Attack01" : "Attack02");
                _attackTimer = attackCooldown;
                LockAttackState(1f, attackHitDelay, () => {
                    // Deal damage natively to the player!
                    if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= attackRange * 1.5f)
                    {
                        if (playerTarget.TryGetComponent<IDamageable>(out var dmg))
                        {
                            dmg.TakeDamage(damage);
                        }
                    }
                }); // Formally halts FSM natively preventing hit stagger disruptions while locking payload!
            }
        }
    }
}

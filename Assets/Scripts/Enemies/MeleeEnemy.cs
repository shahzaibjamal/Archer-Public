using UnityEngine;

public class MeleeEnemy : Enemy
{
    [Header("Melee Combat")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackHitDelay = 0.5f;
    [SerializeField] private float tauntCooldown = 6f;
    [SerializeField, Range(0f, 1f)] private float tauntChance = 0.5f;
    private float _attackTimer;
    private float _tauntTimer;

    protected override void Start()
    {
        base.Start();
        _tauntTimer = Random.Range(1f, tauntCooldown); // random initial delay
    }

    protected override void Update()
    {
        base.Update();
        if (_tauntTimer > 0) _tauntTimer -= Time.deltaTime;
    }

    protected override void BehaviorUpdate()
    {
        if (playerTarget == null) return;
        
        // FLATTEN THE TARGET Y COORDINATE TO ZERO OUT GROUND-CLIPPING
        Vector3 flatTargetPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float dist = Vector3.Distance(transform.position, flatTargetPos);

        if (dist <= aggroRange && dist > attackRange)
        {
            if (_tauntTimer <= 0f && currentState != EnemyState.Victory)
            {
                _tauntTimer = tauntCooldown;
                if (Random.value <= tauntChance)
                {
                    if (animator != null) animator.SetTrigger("Taunt");
                    ChangeState(EnemyState.Taunt);
                    return;
                }
            }

            // Get coordinated position from BattleManager if available
            Vector3 destination = playerTarget.position;
            if (BattleManager.Instance != null)
            {
                destination = BattleManager.Instance.GetCombatPosition(this, playerTarget, attackRange * 0.8f);
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
        else if (dist <= attackRange)
        {
            if (agent != null) agent.isStopped = true;
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

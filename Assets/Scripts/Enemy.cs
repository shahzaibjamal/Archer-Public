using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public enum EnemyState { Idle, Patrol, Combat, UsingAbility, Hit, Stunned, Attacking, Block, Taunt, Dead, Victory }

public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Components")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected GameObject highlightObject;


    [Header("Base Stats")]
    [SerializeField] protected float maxHealth = 30f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float aggroRange = 15f;
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float visualYOffset = 1f;

    [Header("AI & FSM pacing")]
    [SerializeField] protected float baseIdleTime = 2f;
    [SerializeField] protected float patrolRange = 5f;
    [SerializeField] protected float abilityCheckInterval = 1.5f;

    [Header("Hit / Stun Dynamics")]
    [SerializeField] protected float hitStunDuration = 0.5f;
    [SerializeField] protected float stunDuration = 2f;
    [SerializeField] protected int hitsToStun = 3;

    [Header("New States")]
    [SerializeField] protected float blockDuration = 1f;
    [SerializeField] protected float blockCooldown = 3f;
    [SerializeField] protected float tauntDuration = 2f;
    [SerializeField] protected float deathDuration = 3f;
    protected float blockCooldownTimer;

    protected EnemyState currentState = EnemyState.Idle;
    protected float stateTimer;
    protected Vector3 patrolTargetPos;

    protected Transform playerTarget;
    private EnemyAbility[] equippedAbilities;
    protected EnemyAbility activeAbility;
    protected int rewardGold;
    private Vector3 _lastPosition;
    private int _hitsTaken = 0;

    protected Action pendingAttackAction;
    protected float attackActionTimer;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public Animator EnemyAnimator => animator;
    public Action<float, float> OnHealthChanged;
    public string enemyTypeName = "Melee";

    protected virtual void Start()
    {
        _lastPosition = transform.position;

        // Overlay JSON Metadata natively on top of any inspector tuning smoothly 
        if (DataManager.Instance != null)
        {
            var stats = DataManager.Instance.GetEnemyStats(enemyTypeName);
            if (stats != null)
            {
                if (stats.MaxHealth > 0) maxHealth = stats.MaxHealth;
                if (stats.MoveSpeed > 0) moveSpeed = stats.MoveSpeed;
                if (stats.AttackRange > 0) attackRange = stats.AttackRange;
                if (stats.AggroRange > 0) aggroRange = stats.AggroRange;
                if (stats.Damage > 0) damage = stats.Damage;
                if (stats.BaseIdleTime > 0) baseIdleTime = stats.BaseIdleTime;
                if (stats.PatrolRange > 0) patrolRange = stats.PatrolRange;
                if (stats.AbilityCheckInterval > 0) abilityCheckInterval = stats.AbilityCheckInterval;
                if (stats.HitsToStun > 0) hitsToStun = stats.HitsToStun;
            }
        }

        currentHealth = maxHealth;
        equippedAbilities = GetComponents<EnemyAbility>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;

        GameEvents.PlayerDied += OnPlayerDied;
        GameEvents.TriggerEnemySpawned(this);
        ChangeState(EnemyState.Idle);
    }

    private void OnPlayerDied()
    {
        if (currentState != EnemyState.Dead)
        {
            ChangeState(EnemyState.Victory);
            if (animator != null) animator.SetTrigger("Victory");
        }
    }

    protected virtual void Update()
    {
        if (currentHealth <= 0) return;

        transform.position = new Vector3(transform.position.x, visualYOffset, transform.position.z);

        float currentSpeed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
        _lastPosition = transform.position;
        if (animator != null) animator.SetFloat("Speed", currentSpeed);

        UpdateCooldowns();

        switch (currentState)
        {
            case EnemyState.Idle: HandleIdle(); break;
            case EnemyState.Patrol: HandlePatrol(); break;
            case EnemyState.Combat: HandleCombat(); break;
            case EnemyState.UsingAbility: HandleAbility(); break;
            case EnemyState.Hit: HandleHitStun(); break;
            case EnemyState.Stunned: HandleStun(); break;
            case EnemyState.Attacking: HandleAttacking(); break;
            case EnemyState.Block: HandleBlock(); break;
            case EnemyState.Taunt: HandleTaunt(); break;
        }
    }

    protected virtual void CheckForIncomingAttacks()
    {
        if (blockCooldownTimer > 0) return;
        if (ArrowPoolManager.Instance == null) return;

        foreach (var arrow in ArrowPoolManager.Instance.ActiveArrows)
        {
            if (arrow == null || arrow.IsEnemyProjectile) continue;

            Vector3 toEnemy = transform.position - arrow.transform.position;
            if (toEnemy.sqrMagnitude < 64f)
            {
                if (Vector3.Dot(arrow.transform.forward, toEnemy.normalized) > 0.85f)
                {
                    blockCooldownTimer = blockCooldown;
                    if (animator != null) animator.SetTrigger("Block");
                    ChangeState(EnemyState.Block);
                    return;
                }
            }
        }
    }

    private void UpdateCooldowns()
    {
        if (equippedAbilities != null)
        {
            foreach (var ability in equippedAbilities)
                ability.TickCooldown(Time.deltaTime);
        }

        if (blockCooldownTimer > 0) blockCooldownTimer -= Time.deltaTime;
    }

    public void LockAttackState(float duration, float hitDelay = 0f, Action onHit = null)
    {
        // Enforces animation lock so they cannot be natively knocked out of their Attack state by generic arrows
        stateTimer = duration;
        attackActionTimer = hitDelay;
        pendingAttackAction = onHit;
        ChangeState(EnemyState.Attacking);
    }

    protected virtual void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Idle) stateTimer = baseIdleTime;
        else if (newState == EnemyState.Combat) stateTimer = abilityCheckInterval;
        else if (newState == EnemyState.Hit) stateTimer = hitStunDuration;
        else if (newState == EnemyState.Stunned) stateTimer = stunDuration;
        else if (newState == EnemyState.Block) stateTimer = blockDuration;
        else if (newState == EnemyState.Taunt) stateTimer = tauntDuration;
        else if (newState == EnemyState.Dead) stateTimer = deathDuration;
    }

    protected virtual bool CheckAggro()
    {
        if (playerTarget == null) return false;
        if (Vector3.Distance(transform.position, playerTarget.position) <= aggroRange)
        {
            ChangeState(EnemyState.Combat);
            return true;
        }
        return false;
    }

    protected virtual void HandleIdle()
    {
        CheckForIncomingAttacks();
        if (currentState == EnemyState.Block) return;
        if (CheckAggro()) return;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            Vector2 randomDir = Random.insideUnitCircle * patrolRange;
            patrolTargetPos = transform.position + new Vector3(randomDir.x, 0, randomDir.y);
            ChangeState(EnemyState.Patrol);
        }
    }

    protected virtual void HandlePatrol()
    {
        CheckForIncomingAttacks();
        if (currentState == EnemyState.Block) return;
        if (CheckAggro()) return;

        float dist = Vector3.Distance(transform.position, patrolTargetPos);
        if (dist > 0.5f)
        {
            transform.position = Vector3.MoveTowards(transform.position, patrolTargetPos, moveSpeed * 0.5f * Time.deltaTime);
            transform.LookAt(new Vector3(patrolTargetPos.x, transform.position.y, patrolTargetPos.z));
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }

    protected virtual void HandleCombat()
    {
        CheckForIncomingAttacks();
        if (currentState == EnemyState.Block) return;

        if (playerTarget == null || Vector3.Distance(transform.position, playerTarget.position) > aggroRange * 1.5f)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            stateTimer = abilityCheckInterval;
            if (TryRollAbility()) return;
        }

        BehaviorUpdate();
    }

    private bool TryRollAbility()
    {
        if (equippedAbilities == null || equippedAbilities.Length == 0) return false;

        foreach (var ability in equippedAbilities)
        {
            if (ability.IsReady())
            {
                if (Random.value <= ability.chanceToTrigger)
                {
                    stateTimer = ability.executionDuration;
                    activeAbility = ability;
                    ability.ExecuteOnStart(this);
                    ChangeState(EnemyState.UsingAbility);
                    return true;
                }
                else
                {
                    ability.ResetCooldown();
                }
            }
        }
        return false;
    }

    protected virtual void HandleAbility()
    {
        if (activeAbility != null) activeAbility.UpdateAbility(this);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            activeAbility = null;
            ChangeState(EnemyState.Combat);
        }
    }

    protected virtual void HandleHitStun()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0) ChangeState(EnemyState.Combat);
    }

    protected virtual void HandleStun()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0) ChangeState(EnemyState.Combat);
    }

    protected virtual void HandleAttacking()
    {
        if (attackActionTimer > 0f)
        {
            attackActionTimer -= Time.deltaTime;
            if (attackActionTimer <= 0f)
            {
                pendingAttackAction?.Invoke();
                pendingAttackAction = null;
            }
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0) ChangeState(EnemyState.Combat);
    }

    protected virtual void HandleBlock()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0) ChangeState(EnemyState.Combat);
    }

    protected virtual void HandleTaunt()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0) ChangeState(EnemyState.Combat);
    }

    public void SetReward(int reward) => rewardGold = reward;

    public virtual void SetHighlighted(bool isHighlighted)
    {
        if (highlightObject != null)
            highlightObject.SetActive(isHighlighted);
    }

    protected abstract void BehaviorUpdate();

    public virtual void TakeDamage(float amount)
    {
        if (currentState == EnemyState.Block || currentState == EnemyState.Dead || currentState == EnemyState.Victory) return;

        currentHealth -= amount;

        if (currentHealth > 0f)
        {
            _hitsTaken++;
            if (_hitsTaken >= hitsToStun)
            {
                _hitsTaken = 0;
                // Force Stun regardless of whether they are in the middle of attacking!
                if (animator != null) animator.SetTrigger("Stun");
                ChangeState(EnemyState.Stunned);
            }
            else if (currentState != EnemyState.Attacking && currentState != EnemyState.UsingAbility)
            {
                // Standard Hit Stagger cancels generic movement completely
                if (animator != null) animator.SetTrigger("TakeDamage");
                ChangeState(EnemyState.Hit);
            }

            OnHitVFXStub();
        }
        else
        {
            Die();
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void OnHitVFXStub()
    {
        // Insert Particle Systems or Blood Splatters here!
        // Debug.Log($"Spawned Hit VFX on {name}"); // Natively stubbed out for later FX integrations
    }

    public virtual void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void Die()
    {
        ChangeState(EnemyState.Dead);
        if (animator != null) animator.SetTrigger("Death");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, deathDuration);
    }

    private void OnDestroy()
    {
        GameEvents.PlayerDied -= OnPlayerDied;
        GameEvents.TriggerEnemyDestroyed(this);
    }
}

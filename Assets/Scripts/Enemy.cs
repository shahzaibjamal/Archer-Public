using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

public enum EnemyState { Idle, Patrol, Combat, UsingAbility }

public abstract class Enemy : MonoBehaviour, IDamageable
{
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

    protected EnemyState currentState = EnemyState.Idle;
    protected float stateTimer;
    protected Vector3 patrolTargetPos;

    protected Transform playerTarget;
    private EnemyAbility[] equippedAbilities;
    protected int rewardGold;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public Action<float, float> OnHealthChanged;
    protected virtual void Start()
    {
        currentHealth = maxHealth;
        equippedAbilities = GetComponents<EnemyAbility>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTarget = playerObj.transform;

        GameEvents.TriggerEnemySpawned(this);
        ChangeState(EnemyState.Idle);
    }

    protected virtual void Update()
    {
        if (currentHealth <= 0) return;

        // Force rigid Y offset for generic capsule placeholders strictly mathematically
        transform.position = new Vector3(transform.position.x, visualYOffset, transform.position.z);

        UpdateCooldowns();

        // Standard Finite State Machine routing
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Patrol:
                HandlePatrol();
                break;
            case EnemyState.Combat:
                HandleCombat();
                break;
            case EnemyState.UsingAbility:
                HandleAbility();
                break;
        }
    }

    private void UpdateCooldowns()
    {
        if (equippedAbilities == null) return;
        foreach (var ability in equippedAbilities)
            ability.TickCooldown(Time.deltaTime);
    }

    protected virtual void ChangeState(EnemyState newState)
    {
        currentState = newState;

        if (newState == EnemyState.Idle)
            stateTimer = baseIdleTime;
        else if (newState == EnemyState.Combat)
            stateTimer = abilityCheckInterval;
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
        if (CheckAggro()) return;

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            Debug.LogError("HandleIdle - " + transform.position);

            Vector2 randomDir = Random.insideUnitCircle * patrolRange;
            patrolTargetPos = transform.position + new Vector3(randomDir.x, 0, randomDir.y);
            Debug.LogError("patrol - " + patrolTargetPos);
            ChangeState(EnemyState.Patrol);
        }
    }

    protected virtual void HandlePatrol()
    {
        if (CheckAggro()) return;

        float dist = Vector3.Distance(transform.position, patrolTargetPos);
        if (dist > 0.5f)
        {
            Debug.LogError("HandlePatrol - " + transform.position);
            // Move slower when patrolling
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
        // Disengage if walked/pushed too far away
        if (playerTarget == null || Vector3.Distance(transform.position, playerTarget.position) > aggroRange * 1.5f)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        // Pacing & Abilities
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            stateTimer = abilityCheckInterval;
            if (TryRollAbility())
            {
                return; // Suppress normal combat behavior directly for this frame
            }
        }

        // Execution of standard subclass behavioral pathing
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
                    // Lock them into Ability animation state length!
                    stateTimer = ability.executionDuration;
                    ability.ExecuteOnStart(this);
                    ChangeState(EnemyState.UsingAbility);
                    return true;
                }
                else
                {
                    // Put it on cooldown anyway so they don't brute-force a 10% chance roll by checking every frame
                    ability.ResetCooldown();
                }
            }
        }
        return false;
    }

    protected virtual void HandleAbility()
    {
        // Enemy is completely busy doing ability animations/logic
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            ChangeState(EnemyState.Combat);
        }
    }

    public void SetReward(int reward) => rewardGold = reward;

    public virtual void SetHighlighted(bool isHighlighted) { }

    protected abstract void BehaviorUpdate();

    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f) Die();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public virtual void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        GameEvents.TriggerEnemyDestroyed(this);
    }
}

using System;
using UnityEngine.AI;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;
using System.Collections.Generic;

public enum EnemyState { Idle, Patrol, Combat, UsingAbility, Hit, Stunned, Attacking, Block, Taunt, Dead, Victory }

public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Components")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected GameObject highlightObject;
    protected NavMeshAgent agent;

    [Header("Base Stats")]
    [SerializeField] protected float maxHealth = 30f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float aggroRange = 15f;
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float visualYOffset = 1f;

    [Header("AI & FSM Pacing")]
    [SerializeField] protected float baseIdleTime = 2f;
    [SerializeField] protected float patrolRange = 5f;
    [SerializeField] protected float abilityCheckInterval = 1.5f;
    [SerializeField] protected float patrolSpeedMultiplier = 0.5f;

    [Header("Hit / Stun Dynamics")]
    [SerializeField] protected float hitStunDuration = 0.5f;
    [SerializeField] protected float stunDuration = 2f;
    [SerializeField] protected int hitsToStun = 3;

    [Header("Detection / Blocking")]
    [SerializeField] protected LayerMask obstacleLayer;
    [SerializeField] protected float blockDetectionRadius = 8f;
    [SerializeField] protected float blockAngleThreshold = 0.85f;
    [SerializeField] protected float blockDuration = 1f;
    [SerializeField] protected float blockCooldown = 3f;
    [SerializeField] protected float incomingDetectionSqrRange = 64f;

    [Header("Movement Settings")]
    [SerializeField] protected float angularSpeed = 120f;
    [SerializeField] protected float acceleration = 20f;
    [SerializeField] protected float stoppingDistance = 0.1f;

    [Header("Miscellaneous")]
    [SerializeField] protected float tauntDuration = 2f;
    [SerializeField] protected float deathDuration = 3f;
    [SerializeField] protected float fireCheckHeight = 1.5f;
    [SerializeField] protected bool useInstantDamageAnim = true; // Set to false to see previous "queued" behavior

    protected float blockCooldownTimer;
    protected EnemyState currentState = EnemyState.Idle;
    protected float stateTimer;
    protected Vector3 patrolTargetPos;

    protected Transform playerTarget;
    private EnemyAbility[] equippedAbilities;
    protected EnemyAbility activeAbility;
    protected int rewardGold;
    protected int enemyLevel;
    protected float speedMultiplier = 1f;
    protected List<float> activeSlows = new List<float>();
    private Vector3 _lastPosition;
    private int _hitsTaken = 0;

    protected Action pendingAttackAction;
    protected float attackActionTimer;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public Animator EnemyAnimator => animator;
    public Action<float, float> OnHealthChanged;
    public EnemyType enemyType = EnemyType.Melee;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    protected virtual void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    protected virtual void Start()
    {
        _lastPosition = transform.position;

        if (DataManager.Instance != null && DataManager.Instance.Metadata != null)
        {
            var stats = DataManager.Instance.GetEnemyStats(enemyType);
            if (stats != null)
            {
                enemyLevel = stats.Level;
                if (stats.MaxHealth > 0) maxHealth = stats.MaxHealth;
                if (stats.MoveSpeed > 0) moveSpeed = stats.MoveSpeed;
                if (stats.AttackRange > 0) attackRange = stats.AttackRange;
                if (stats.AggroRange > 0) aggroRange = stats.AggroRange;
                if (stats.Damage > 0) damage = stats.Damage;
                if (stats.BaseIdleTime > 0) baseIdleTime = stats.BaseIdleTime;
                if (stats.PatrolRange > 0) patrolRange = stats.PatrolRange;
                if (stats.AbilityCheckInterval > 0) abilityCheckInterval = stats.AbilityCheckInterval;
                if (stats.HitsToStun > 0) hitsToStun = stats.HitsToStun;

                // NavMeshAgent Settings
                if (stats.AngularSpeed > 0) angularSpeed = stats.AngularSpeed;
                if (stats.Acceleration > 0) acceleration = stats.Acceleration;
                if (stats.StoppingDistance > 0) stoppingDistance = stats.StoppingDistance;

                // Sync block detection
                if (stats.BlockDetectionRadius > 0)
                {
                    blockDetectionRadius = stats.BlockDetectionRadius;
                    incomingDetectionSqrRange = blockDetectionRadius * blockDetectionRadius;
                }
                if (stats.BlockAngleThreshold > 0) blockAngleThreshold = stats.BlockAngleThreshold;

                // NEW: Sync NavMesh Mask
                currentHealth = maxHealth;
                equippedAbilities = GetComponents<EnemyAbility>();
                agent = GetComponent<NavMeshAgent>();

                if (agent != null)
                {
                    agent.speed = moveSpeed * speedMultiplier;
                    agent.angularSpeed = angularSpeed;
                    agent.acceleration = acceleration;
                    agent.stoppingDistance = stoppingDistance;
                }
            }
        }

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

        float currentSpeed = 0f;
        if (agent != null)
        {
            currentSpeed = agent.velocity.magnitude;
        }
        else
        {
            currentSpeed = (transform.position - _lastPosition).magnitude / Time.deltaTime;
            _lastPosition = transform.position;
        }

        if (animator != null) animator.SetFloat("Speed", currentSpeed);

        UpdateCooldowns();
        if (currentHealth > 0) CheckForIncomingArrows();

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
        // Handled by CheckForIncomingArrows but keeping for specific block logic if needed
    }

    protected virtual void CheckForIncomingArrows()
    {
        if (ArrowPoolManager.Instance == null || currentState == EnemyState.Dead) return;

        foreach (var arrow in ArrowPoolManager.Instance.ActiveArrows)
        {
            if (arrow == null || arrow.IsEnemyProjectile) continue;

            Vector3 toEnemy = transform.position - arrow.transform.position;
            float distSqr = toEnemy.sqrMagnitude;

            // Reactive sensing within 6 meters
            if (distSqr < 36f)
            {
                // Check if arrow is heading generally towards us
                Vector3 arrowVel = arrow.transform.forward;
                float dot = Vector3.Dot(arrowVel, toEnemy.normalized);

                if (dot > 0.8f) // Heading straight at us
                {
                    TryReactiveDodge();
                    return;
                }
            }
        }
    }

    private void TryReactiveDodge()
    {
        if (currentState == EnemyState.UsingAbility || currentState == EnemyState.Attacking || currentState == EnemyState.Stunned) return;

        foreach (var ability in GetComponents<EnemyAbility>())
        {
            if (ability is DodgeAbility dodge && dodge.IsReady())
            {
                // Force triggering the dodge
                activeAbility = dodge;
                dodge.ExecuteOnStart(this);
                ChangeState(EnemyState.UsingAbility);
                return;
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

        if (agent != null)
        {
            if (agent.destination != patrolTargetPos)
            {
                agent.isStopped = false;
                agent.SetDestination(patrolTargetPos);
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                ChangeState(EnemyState.Idle);
            }
        }
        else
        {
            float dist = Vector3.Distance(transform.position, patrolTargetPos);
            if (dist > stoppingDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, patrolTargetPos, moveSpeed * speedMultiplier * patrolSpeedMultiplier * Time.deltaTime);
                transform.LookAt(new Vector3(patrolTargetPos.x, transform.position.y, patrolTargetPos.z));
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
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

    public virtual bool HasLineOfSight()
    {
        if (playerTarget == null) return false;
        Vector3 start = transform.position + Vector3.up * fireCheckHeight;
        Vector3 end = new Vector3(playerTarget.position.x, start.y, playerTarget.position.z);
        return !Physics.Linecast(start, end, obstacleLayer);
    }

    public virtual void SetHighlighted(bool isHighlighted)
    {
        if (highlightObject != null)
            highlightObject.SetActive(isHighlighted);
    }

    public Transform GetPlayerTarget() => playerTarget;
    public int GetEnemyLevel() => enemyLevel;
    public void SetSpeedMultiplier(float multiplier, bool apply)
    {
        if (apply) activeSlows.Add(multiplier);
        else activeSlows.Remove(multiplier);

        speedMultiplier = 1f;
        foreach (float s in activeSlows)
        {
            if (s < speedMultiplier) speedMultiplier = s; // Take strongest slow
        }

        if (agent != null) agent.speed = moveSpeed * speedMultiplier;
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
                if (animator != null) animator.SetTrigger("Stun");
                ChangeState(EnemyState.Stunned);
            }
            else if (currentState != EnemyState.Attacking && currentState != EnemyState.UsingAbility)
            {
                if (animator != null)
                {
                    if (useInstantDamageAnim)
                        animator.Play("TakeDamage", 0, 0f);
                    else
                        animator.SetTrigger("TakeDamage");
                }
                ChangeState(EnemyState.Hit);
            }

            OnHitVFXStub();
            ApplyColor(Color.red);
            DOTween.To(() => Color.red, x => ApplyColor(x), Color.white, 0.2f)
                .SetEase(Ease.InQuad);
        }
        else
        {
            Die();
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void OnHitVFXStub() { }

    private void ApplyColor(Color color)
    {
        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, color);
            r.SetPropertyBlock(_propBlock);
        }
    }

    public virtual void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void Die()
    {
        if (currentState == EnemyState.Dead) return;
        StartCoroutine(DieSequence());
    }

    private System.Collections.IEnumerator DieSequence()
    {
        ChangeState(EnemyState.Dead);
        if (animator != null) animator.SetTrigger("Death");

        OnDeathVFXStub();
        OnDeathSFXStub();

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (agent != null) agent.enabled = false;

        yield return new WaitForSeconds(deathDuration);

        if (AssetLoader.Instance != null)
        {
            AssetLoader.Instance.ReleaseInstance(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDeathVFXStub() { }
    protected virtual void OnDeathSFXStub() { }

    private void OnDestroy()
    {
        GameEvents.PlayerDied -= OnPlayerDied;
        GameEvents.TriggerEnemyDestroyed(this);
    }
}

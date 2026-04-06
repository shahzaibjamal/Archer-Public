using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour, IDamageable
{
    public InputSystem_Actions _actions;
    [SerializeField] private FloatingJoystick _floatingJoystick;
    private Vector2 _moveInput;
    private PlayerAnimator _playerAnim;
    private NavMeshAgent _agent;
    private CharacterController _controller;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _rotationSpeed = 15f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;
    private bool _isDead = false;

    [Header("Targeting")]
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private bool _isActive = true;
    [SerializeField] private bool _targetByMoveDirection = true;
    [SerializeField] private float _lockOnRadius = 10f;
    [SerializeField] private float _loseTargetRadius = 15f;
    [SerializeField] private float _meleeRadius = 2f;
    [SerializeField, Range(0.1f, 3f)] private float _attackInterval = 1f;
    [SerializeField, Range(0f, 1f)] private float _cooldown = 0.2f; 
    [SerializeField, Range(0f, 1.5f)] private float _rangedFireTime = 0.4f; 
    [SerializeField] private float _rangedAnimClipDuration = 1f; 
    [SerializeField] private float _meleeAnimClipDuration = 1f; 
    private float _attackTimer;
    private float _directionParam = 1f;
    private bool _hasFiredInCurrentCycle = true;
    private Enemy _currentTarget;
    private Enemy _previousTarget;

    // Power-Up State
    private bool _isInvincible = false;
    private float _attackBuffMultiplier = 1f;
    private float _defenseBuffMultiplier = 1f;
    private float _enemyDefenseDebuff = 1f;

    // Arsenal & Special Shot
    private ArrowType? _queuedSpecialArrow = null;
    private bool _nextAttackIsSpecial = false;

    public Action<float, float> OnHealthChanged;
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (_actions == null)
            _actions = new InputSystem_Actions();
        _controller = GetComponent<CharacterController>();
        _agent = GetComponent<NavMeshAgent>();
        if (_agent != null)
        {
            _agent.updateRotation = false; 
            _agent.updateUpAxis = false;
        }

        _playerAnim = gameObject.GetComponent<PlayerAnimator>() ?? gameObject.AddComponent<PlayerAnimator>();
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        RefreshStats();
        _currentHealth = _maxHealth;

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.StartLevel(1);
        }
    }

    private void OnEnable()
    {
        _actions.Enable();
        GameEvents.PlayerStatsUpdated += RefreshStats;
        GameEvents.SpecialArrowRequested += QueueSpecialArrow;
        GameEvents.PowerUpActivated += ActivatePowerUp;
    }

    private void OnDisable()
    {
        _actions.Disable();
        GameEvents.PlayerStatsUpdated -= RefreshStats;
        GameEvents.SpecialArrowRequested -= QueueSpecialArrow;
        GameEvents.PowerUpActivated -= ActivatePowerUp;
    }

    public void RefreshStats()
    {
        if (DataManager.Instance == null || DataManager.Instance.GameState == null) return;
        var gs = DataManager.Instance.GameState;

        _moveSpeed = gs.MoveSpeed;
        _attackInterval = gs.AttackInterval;
        _cooldown = gs.Cooldown;
        _lockOnRadius = gs.LockOnRadius;
        _maxHealth = gs.MaxHealth;

        if (_agent != null)
        {
            _agent.speed = _moveSpeed;
            _agent.angularSpeed = _rotationSpeed * 50f;
        }
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }

    private void QueueSpecialArrow(ArrowType arrowType)
    {
        _queuedSpecialArrow = arrowType;
    }

    private void ActivatePowerUp(PowerUpType type)
    {
        if (DataManager.Instance == null || DataManager.Instance.GameState == null) return;
        var gs = DataManager.Instance.GameState;
        var pData = DataManager.Instance.Metadata.PlayerStats;

        if (gs.GetPowerUpCount(type) <= 0) return;
        
        gs.UsePowerUp(type);
        DataManager.Instance.SaveGameState();

        switch (type)
        {
            case PowerUpType.Heal:
                _currentHealth = Mathf.Min(_maxHealth, _currentHealth + pData.HealAmount);
                OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
                ApplyColor(Color.green);
                DOTween.To(() => Color.green, x => ApplyColor(x), Color.white, 0.5f);
                break;

            case PowerUpType.Invincibility:
                StartCoroutine(InvincibilityRoutine(pData.InvincibilityDuration));
                break;

            case PowerUpType.AttackUp:
                StartCoroutine(AttackUpRoutine(pData.AttackUpMultiplier, 10f)); // Duration hardcoded for now or add to pData
                break;

            case PowerUpType.DefenseUp:
                StartCoroutine(DefenseUpRoutine(pData.DefenseUpReduction, 10f));
                break;

            case PowerUpType.DefenseDown:
                StartCoroutine(DefenseDownRoutine(pData.DefenseDownDebuff, 10f));
                break;
        }
    }

    private System.Collections.IEnumerator InvincibilityRoutine(float duration)
    {
        _isInvincible = true;
        ApplyColor(Color.yellow);
        yield return new WaitForSeconds(duration);
        _isInvincible = false;
        ApplyColor(Color.white);
    }

    private System.Collections.IEnumerator AttackUpRoutine(float multiplier, float duration)
    {
        _attackBuffMultiplier = multiplier;
        // Simple visual feedback: slightly reddish
        ApplyColor(new Color(1f, 0.5f, 0.5f)); 
        yield return new WaitForSeconds(duration);
        _attackBuffMultiplier = 1f;
        ApplyColor(Color.white);
    }

    private System.Collections.IEnumerator DefenseUpRoutine(float reduction, float duration)
    {
        // reduction = 0.5f means 50% less damage taken
        _defenseBuffMultiplier = (1f - reduction);
        ApplyColor(new Color(0.5f, 0.5f, 1f)); // Blueish
        yield return new WaitForSeconds(duration);
        _defenseBuffMultiplier = 1f;
        ApplyColor(Color.white);
    }

    private System.Collections.IEnumerator DefenseDownRoutine(float debuff, float duration)
    {
        // enemy takes 1 + debuff damage. e.g. debuff = 0.5 means 1.5x damage to enemies
        _enemyDefenseDebuff = (1f + debuff);
        // Visual indicator on player? Maybe a sparkle or something later.
        yield return new WaitForSeconds(duration);
        _enemyDefenseDebuff = 1f;
    }

    private void Update()
    {
        if (_isDead) return;

        _moveInput = _actions.Player.Move.ReadValue<Vector2>();

        if (_floatingJoystick != null && _floatingJoystick.Direction.sqrMagnitude > 0.01f)
        {
            _moveInput = _floatingJoystick.Direction;
        }

        UpdateTargeting();
        Move();
        AutoAttack();
    }

    public Vector3 GetMoveInput() => _moveInput;
    public float GetMoveSpeed() => _moveSpeed;

    private void AutoAttack()
    {
        if (!_isActive) return;
        _attackTimer += Time.deltaTime;

        float currentInterval = _nextAttackIsSpecial ? _attackInterval * 1.5f : _attackInterval;
        float timeForAnim = Mathf.Max(0.01f, currentInterval);
        float rangedSpeedMult = _rangedAnimClipDuration / timeForAnim;
        float meleeSpeedMult = _meleeAnimClipDuration / timeForAnim;

        _playerAnim.UpdateAttackRates(rangedSpeedMult, meleeSpeedMult);

        float scaledFireTime = _rangedFireTime / rangedSpeedMult;
        float totalCycleTime = currentInterval + _cooldown;

        if (!_hasFiredInCurrentCycle && _attackTimer >= scaledFireTime)
        {
            if (ArrowPoolManager.Instance != null)
            {
                float flightTimeAvailable = _attackInterval - scaledFireTime;
                float calculatedArrowSpeed = flightTimeAvailable > 0.01f ? _lockOnRadius / flightTimeAvailable : _lockOnRadius;
                Transform fp = _firePoint != null ? _firePoint : transform;

                ArrowType launchType = ArrowType.Normal;
                if (_nextAttackIsSpecial && _queuedSpecialArrow.HasValue)
                {
                    launchType = _queuedSpecialArrow.Value;
                }

                Vector3? tgtPos = null;
                if (_currentTarget != null)
                {
                    tgtPos = _currentTarget.transform.position;
                }

                float currentDmg = (DataManager.Instance?.GameState.CurrentDamage ?? 10f) * _attackBuffMultiplier * _enemyDefenseDebuff;
                ArrowPoolManager.Instance.FireArrow(launchType, fp.position, fp.rotation, calculatedArrowSpeed, _lockOnRadius, currentDmg, tgtPos, false);
                
                _queuedSpecialArrow = null;
                _nextAttackIsSpecial = false;
            }
            _hasFiredInCurrentCycle = true;
        }

        if (_currentTarget != null && _attackTimer >= totalCycleTime)
        {
            float sqrDist = (transform.position - _currentTarget.transform.position).sqrMagnitude;

            if (sqrDist <= _meleeRadius * _meleeRadius)
            {
                PlayAttack("MeleeAttack");
                PerformMeleeHit();
                _attackTimer = 0f;
                _hasFiredInCurrentCycle = true;
            }
            else if (sqrDist <= _lockOnRadius * _lockOnRadius)
            {
                if (_queuedSpecialArrow.HasValue)
                {
                    _nextAttackIsSpecial = true;
                }

                PlayAttack("Attack");
                _attackTimer = 0f;
                _hasFiredInCurrentCycle = false;
            }
        }
    }

    private void PerformMeleeHit()
    {
        if (_currentTarget != null)
        {
            float damage = DataManager.Instance.GameState.CurrentDamage * _attackBuffMultiplier * _enemyDefenseDebuff * 1.5f;
            _currentTarget.TakeDamage(damage);
        }
    }

    private void UpdateTargeting()
    {
        if (_currentTarget != null)
        {
            float sqrDist = (transform.position - _currentTarget.transform.position).sqrMagnitude;
            if (sqrDist > _loseTargetRadius * _loseTargetRadius)
            {
                _currentTarget = null;
            }
            if (!_targetByMoveDirection && _currentTarget != null) return;
        }

        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        bool hasMoveInput = moveDir.sqrMagnitude > 0.01f;

        if (_targetByMoveDirection && !hasMoveInput && _currentTarget != null) return;

        Enemy best = null;
        float bestScore = float.MinValue;
        float closestDistanceSqr = _lockOnRadius * _lockOnRadius;

        if (BattleManager.Instance == null) return;

        foreach (var enemy in BattleManager.Instance.ActiveEnemies)
        {
            if (enemy == null) continue;
            float distanceSqr = (transform.position - enemy.transform.position).sqrMagnitude;

            if (distanceSqr <= _lockOnRadius * _lockOnRadius || enemy == _currentTarget)
            {
                if (!IsLineOfSightClear(enemy)) continue;

                if (_targetByMoveDirection)
                {
                    if (hasMoveInput)
                    {
                        Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
                        float score = Vector3.Dot(moveDir, dirToEnemy);
                        if (enemy == _currentTarget) score += 0.2f;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = enemy;
                        }
                    }
                    else
                    {
                        if (distanceSqr <= closestDistanceSqr)
                        {
                            closestDistanceSqr = distanceSqr;
                            best = enemy;
                        }
                    }
                }
                else
                {
                    if (distanceSqr <= closestDistanceSqr)
                    {
                        closestDistanceSqr = distanceSqr;
                        best = enemy;
                    }
                }
            }
        }

        if (best != null) _currentTarget = best;
        else _currentTarget = null;

        if (_currentTarget != _previousTarget)
        {
            if (_previousTarget != null) _previousTarget.SetHighlighted(false);
            if (_currentTarget != null) _currentTarget.SetHighlighted(true);
            _previousTarget = _currentTarget;
        }
    }

    private void Move()
    {
        Vector3 move = new Vector3(_moveInput.x, 0, _moveInput.y);

        if (_agent != null && _agent.enabled)
        {
            _agent.Move(move * Time.deltaTime * _moveSpeed);
        }
        else if (_controller != null)
        {
            _controller.Move(move * Time.deltaTime * _moveSpeed);
        }

        float animSpeed = _moveInput.magnitude;

        if (_currentTarget != null)
        {
            Vector3 directionToTarget = _currentTarget.transform.position - transform.position;
            directionToTarget.y = 0;

            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
            }
        }
        else if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
        }

        float targetDirection = 1f;
        if (_currentTarget != null && move.sqrMagnitude > 0.01f)
        {
            float dotProduct = Vector3.Dot(transform.forward, move.normalized);
            if (_directionParam == 1f && dotProduct < -0.2f) targetDirection = -1f;
            else if (_directionParam == -1f && dotProduct < 0f) targetDirection = -1f;
        }
        _directionParam = targetDirection;
        if (animSpeed == 0) _directionParam = 0;
        _playerAnim.UpdateLocomotion(animSpeed, _directionParam);
    }

    private void PlayAttack(string triggerName)
    {
        _playerAnim.PlayAttack(triggerName);
    }

    public void TakeDamage(float amount)
    {
        if (_isDead || _isInvincible) return;

        float finalDamage = amount * _defenseBuffMultiplier;

        _currentHealth -= finalDamage;
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        if (_currentHealth <= 0) Die();
        else
        {
            ApplyColor(Color.red);
            DOTween.To(() => Color.red, x => ApplyColor(x), Color.white, 0.2f).SetEase(Ease.InQuad);
        }
    }

    private void ApplyColor(Color color)
    {
        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorId, color);
            r.SetPropertyBlock(_propBlock);
        }
    }

    private void Die()
    {
        _isDead = true;
        _playerAnim.UpdateLocomotion(0, 0);
        if (TryGetComponent<Animator>(out var anim)) anim.SetTrigger("Death");
        GameEvents.TriggerPlayerDied();
    }

    private bool IsLineOfSightClear(Enemy target)
    {
        if (target == null) return false;
        Vector3 start = _firePoint != null ? _firePoint.position : transform.position + Vector3.up * 1f;
        Vector3 end = new Vector3(target.transform.position.x, start.y, target.transform.position.z);
        return !Physics.Linecast(start, end, _obstacleLayer);
    }
}
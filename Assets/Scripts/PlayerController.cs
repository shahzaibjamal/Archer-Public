using System.Collections.Generic;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IDamageable
{
    public InputSystem_Actions _actions;
    [SerializeField] private FloatingJoystick _floatingJoystick;
    private Vector2 _moveInput;
    private PlayerAnimator _playerAnim;
    private CharacterController _controller;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _rotationSpeed = 15f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;
    private bool _isDead = false;

    [Header("Targeting")]
    [SerializeField] private bool _targetByMoveDirection = true;
    [SerializeField] private float _lockOnRadius = 10f;
    [SerializeField] private float _loseTargetRadius = 15f;
    [SerializeField] private float _meleeRadius = 2f;
    [SerializeField, Range(0.1f, 2f)] private float _attackInterval = 1f;
    [SerializeField, Range(0f, 1f)] private float _cooldown = 0.2f; // Time spent recovering mathematically after animation
    [SerializeField, Range(0f, 1.5f)] private float _rangedFireTime = 0.4f; // NATIVE time in seconds when arrow leaves bow string
    [SerializeField] private float _rangedAnimClipDuration = 1f; // Length of the shooting anim
    [SerializeField] private float _meleeAnimClipDuration = 1f; // Length of the melee anim
    private float _attackTimer;
    private float _directionParam = 1f;
    private bool _hasFiredInCurrentCycle = true;
    private Enemy _currentTarget;
    private Enemy _previousTarget;

    private void Awake()
    {
        if (_actions == null)
            _actions = new InputSystem_Actions();
        _controller = GetComponent<CharacterController>();
        _playerAnim = gameObject.GetComponent<PlayerAnimator>() ?? gameObject.AddComponent<PlayerAnimator>();
    }
    private void Start()
    {
        // Instantly overlay JSON player stats!
        if (DataManager.Instance != null && DataManager.Instance.Metadata != null)
        {
            var p = DataManager.Instance.Metadata.PlayerStats;
            if (p != null)
            {
                if (p.MoveSpeed > 0) _moveSpeed = p.MoveSpeed;
                if (p.RotationSpeed > 0) _rotationSpeed = p.RotationSpeed;
                if (p.AttackInterval > 0) _attackInterval = p.AttackInterval;
                if (p.Cooldown > 0) _cooldown = p.Cooldown;
                if (p.LockOnRadius > 0) _lockOnRadius = p.LockOnRadius;
                if (p.LoseTargetRadius > 0) _loseTargetRadius = p.LoseTargetRadius;
                if (p.MeleeRadius > 0) _meleeRadius = p.MeleeRadius;
                if (p.MaxHealth > 0) _maxHealth = p.MaxHealth;
            }
        }

        _currentHealth = _maxHealth;

        if (BattleManager.Instance != null)
        {
            BattleManager.Instance.StartLevel(1);
        }
    }

    private void OnEnable()
    {
        _actions.Enable();
    }

    private void OnDisable()
    {
        _actions.Disable();
    }

    private void Update()
    {
        if (_isDead) return;

        _moveInput = _actions.Player.Move.ReadValue<Vector2>();

        // Seamless execution overlay allowing Touch Joystick structures to cleanly override base Keyboard payloads dynamically!
        if (_floatingJoystick != null && _floatingJoystick.Direction.sqrMagnitude > 0.01f)
        {
            _moveInput = _floatingJoystick.Direction;
        }

        UpdateTargeting();
        Move();
        AutoAttack();
    }

    private void AutoAttack()
    {
        _attackTimer += Time.deltaTime;

        // The animation natively spans the entire _attackInterval
        float timeForAnim = Mathf.Max(0.01f, _attackInterval);
        float rangedSpeedMult = _rangedAnimClipDuration / timeForAnim;
        float meleeSpeedMult = _meleeAnimClipDuration / timeForAnim;

        _playerAnim.UpdateAttackRates(rangedSpeedMult, meleeSpeedMult);

        float scaledFireTime = _rangedFireTime / rangedSpeedMult;
        float totalCycleTime = _attackInterval + _cooldown;

        // 1. Fire delayed arrow during current attack interval
        if (!_hasFiredInCurrentCycle && _attackTimer >= scaledFireTime)
        {
            if (ArrowPoolManager.Instance != null)
            {
                // The arrow reaches max range exactly at the end of _attackInterval
                float flightTimeAvailable = _attackInterval - scaledFireTime;
                float calculatedArrowSpeed = flightTimeAvailable > 0.01f ? _lockOnRadius / flightTimeAvailable : _lockOnRadius;
                Transform fp = _firePoint != null ? _firePoint : transform;

                Vector3? tgtPos = null;
                if (_currentTarget != null)
                {
                    // Enforce completely horizontal trajectory relative to firepoint elevation
                    tgtPos = new Vector3(_currentTarget.transform.position.x, fp.position.y, _currentTarget.transform.position.z);
                }

                // ArrowPoolManager.Instance.FireArrow(fp.position, fp.rotation, calculatedArrowSpeed, _loseTargetRadius, tgtPos, false);
                ArrowPoolManager.Instance.FireArrow(fp.position, fp.rotation, calculatedArrowSpeed, _loseTargetRadius, null, false);
            }
            _hasFiredInCurrentCycle = true;
        }

        // 2. Start new attack cycle after full (interval + cooldown) elapsed
        if (_currentTarget != null && _attackTimer >= totalCycleTime)
        {
            float sqrDist = (transform.position - _currentTarget.transform.position).sqrMagnitude;

            if (sqrDist <= _meleeRadius * _meleeRadius)
            {
                PlayAttack("MeleeAttack");
                _attackTimer = 0f;
                _hasFiredInCurrentCycle = true;
            }
            else if (sqrDist <= _lockOnRadius * _lockOnRadius)
            {
                PlayAttack("Attack");
                _attackTimer = 0f;
                _hasFiredInCurrentCycle = false;
            }
        }
    }

    private void UpdateTargeting()
    {
        // 1. Drop out-of-bounds target (with explicit lose radius)
        if (_currentTarget != null)
        {
            float sqrDist = (transform.position - _currentTarget.transform.position).sqrMagnitude;
            if (sqrDist > _loseTargetRadius * _loseTargetRadius)
            {
                _currentTarget = null;
            }

            // Revert behavior: if disabled, act like old Zelda lock-on. Once locked, stay locked until dropped!
            if (!_targetByMoveDirection && _currentTarget != null)
            {
                return;
            }
        }

        Vector3 moveDir = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;
        bool hasMoveInput = moveDir.sqrMagnitude > 0.01f;

        // CRITICAL FIX: If TargetByMoveDirection is ON, and we let go of joystick, do not arbitrary search.
        if (_targetByMoveDirection && !hasMoveInput && _currentTarget != null)
        {
            return;
        }

        Enemy best = null;
        float bestScore = float.MinValue;
        float closestDistanceSqr = _lockOnRadius * _lockOnRadius;

        if (BattleManager.Instance == null) return;

        foreach (var enemy in BattleManager.Instance.ActiveEnemies)
        {
            if (enemy == null) continue;

            float distanceSqr = (transform.position - enemy.transform.position).sqrMagnitude;

            // Only evaluate if it's within Lock-On radius OR if it literally is our current Target holding hysteresis
            if (distanceSqr <= _lockOnRadius * _lockOnRadius || enemy == _currentTarget)
            {
                if (_targetByMoveDirection)
                {
                    if (hasMoveInput)
                    {
                        // Score based on how closely enemy aligns with user's inputted move direction
                        Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
                        float score = Vector3.Dot(moveDir, dirToEnemy);

                        // Give a gentle boundary bias to current target to prevent rapid flickering between two inline enemies
                        if (enemy == _currentTarget) score += 0.2f;

                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = enemy;
                        }
                    }
                    else
                    {
                        // Default to finding nearest one if no target exists.
                        if (distanceSqr <= closestDistanceSqr)
                        {
                            closestDistanceSqr = distanceSqr;
                            best = enemy;
                        }
                    }
                }
                else
                {
                    // Original strict distance-based search behavior
                    if (distanceSqr <= closestDistanceSqr)
                    {
                        closestDistanceSqr = distanceSqr;
                        best = enemy;
                    }
                }
            }
        }

        // If we found a valid lock inside standard radius, update. Otherwise gracefully keep hysteresis target
        if (best != null)
        {
            _currentTarget = best;
        }
        else
        {
            _currentTarget = null;
        }

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
        _controller.Move(move * Time.deltaTime * _moveSpeed);

        float animSpeed = _moveInput.magnitude;

        if (_currentTarget != null)
        {
            // Face the locked enemy smoothly
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
            // Normal rotation according to movement input
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _rotationSpeed);
        }

        // Handle backwards movement relative to facing direction with anti-jitter hysteresis
        float targetDirection = 1f;
        if (_currentTarget != null && move.sqrMagnitude > 0.01f)
        {
            float dotProduct = Vector3.Dot(transform.forward, move.normalized);
            // Require a strictly stronger threshold to switch to backwards logic
            if (_directionParam == 1f && dotProduct < -0.2f) targetDirection = -1f;
            // Prevent it from immediately flipping back unless they turn heavily forward again!
            else if (_directionParam == -1f && dotProduct < 0f) targetDirection = -1f;
        }
        _directionParam = targetDirection;
        if (animSpeed == 0)
        {
            _directionParam = 0;
        }
        _playerAnim.UpdateLocomotion(animSpeed, _directionParam);
    }

    private void PlayAttack(string triggerName)
    {
        _playerAnim.PlayAttack(triggerName);
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        Debug.Log("Player took " + amount + " damage natively! Health: " + _currentHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;
        _moveInput = Vector2.zero;
        _playerAnim.UpdateLocomotion(0, 0);
        // Play death animation if available
        if (TryGetComponent<Animator>(out var anim)) anim.SetTrigger("Death");
        
        GameEvents.TriggerPlayerDied();
    }

    private void OnDrawGizmos()
    {
        // Draw Lock On Radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _lockOnRadius);

        // Draw Melee Radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _meleeRadius);

        // Draw Line of Sight to Locked Enemy
        if (_currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
        }
        else
        {
            // Draw normal looking direction
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * _lockOnRadius);
        }
    }
}
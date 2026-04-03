using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputSystem_Actions _actions;
    private Vector2 _moveInput;
    private Animator _animator;
    private CharacterController _controller;
    private ArrowPoolManager _arrowPoolManager;

    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 15f;

    [Header("Targeting")]
    [SerializeField] private float _lockOnRadius = 10f;
    [SerializeField] private float _meleeRadius = 2f;
    [SerializeField, Range(0.1f, 2f)] private float _attackInterval = 1f;
    [SerializeField, Range(0f, 1f)] private float _cooldown = 0.2f; // Time spent recovering mathematically after animation
    [SerializeField, Range(0f, 1f)] private float _rangedFireTime = 0.4f; // NATIVE time in seconds when arrow leaves bow string
    [SerializeField] private float _rangedAnimClipDuration = 1f; // Length of the shooting anim
    [SerializeField] private float _meleeAnimClipDuration = 1f; // Length of the melee anim
    private float _attackTimer;
    private float _directionParam = 1f;
    private bool _hasFiredInCurrentCycle = true;
    private List<Enemy> _enemiesInScene = new List<Enemy>();
    private Enemy _currentTarget;

    private void Awake()
    {
        if(_actions== null)
            _actions = new InputSystem_Actions();
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _arrowPoolManager = GetComponent<ArrowPoolManager>();
    }

    private void OnEnable() 
    {
        _actions.Enable();
        GameEvents.EnemySpawned += AddEnemy;
        GameEvents.EnemyDestroyed += RemoveEnemy;
    }

    private void OnDisable() 
    {
        _actions.Disable();
        GameEvents.EnemySpawned -= AddEnemy;
        GameEvents.EnemyDestroyed -= RemoveEnemy;
    }

    private void AddEnemy(Enemy enemy)
    {
        if (!_enemiesInScene.Contains(enemy))
            _enemiesInScene.Add(enemy);
    }

    private void RemoveEnemy(Enemy enemy)
    {
        if (_enemiesInScene.Contains(enemy))
            _enemiesInScene.Remove(enemy);
    }

    private void Update()
    {
        _moveInput = _actions.Player.Move.ReadValue<Vector2>();
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

        _animator.SetFloat("RangedAnimSpeed", rangedSpeedMult);
        _animator.SetFloat("MeleeAnimSpeed", meleeSpeedMult);

        float scaledFireTime = _rangedFireTime / rangedSpeedMult;
        float totalCycleTime = _attackInterval + _cooldown;

        // 1. Fire delayed arrow during current attack interval
        if (!_hasFiredInCurrentCycle && _attackTimer >= scaledFireTime)
        {
            if (_arrowPoolManager != null)
            {
                // The arrow reaches max range exactly at the end of _attackInterval
                float flightTimeAvailable = _attackInterval - scaledFireTime;
                float calculatedArrowSpeed = flightTimeAvailable > 0.01f ? _lockOnRadius / flightTimeAvailable : _lockOnRadius;
                Vector3? tgtPos = _currentTarget != null ? _currentTarget.transform.position : (Vector3?)null;
                _arrowPoolManager.FireArrow(calculatedArrowSpeed, _lockOnRadius, tgtPos);
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
        // If we already have a target, keep it until it’s well outside the radius
        if (_currentTarget != null)
        {
            float sqrDist = (transform.position - _currentTarget.transform.position).sqrMagnitude;
            float hystRadius = _lockOnRadius * 1.2f;
            if (sqrDist > hystRadius * hystRadius) // hysteresis buffer
            {
                _currentTarget = null;
            }
            return;
        }

        // Otherwise, find the closest enemy within radius using squared distance
        float closestDistanceSqr = _lockOnRadius * _lockOnRadius;
        Enemy best = null;

        foreach (var enemy in _enemiesInScene)
        {
            if (enemy == null) continue;

            float sqrDist = (transform.position - enemy.transform.position).sqrMagnitude;
            if (sqrDist <= closestDistanceSqr)
            {
                closestDistanceSqr = sqrDist;
                best = enemy;
            }
        }

        _currentTarget = best;
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
        
        _animator.SetFloat("Speed", animSpeed);
        _animator.SetFloat("Direction", _directionParam);
    }

    private void PlayAttack(string triggerName)
    {
        _animator.SetTrigger(triggerName);
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
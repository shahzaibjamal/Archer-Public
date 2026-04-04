using UnityEngine;

public class HealerEnemy : Enemy
{
    [SerializeField] private float healAmount = 10f;
    [SerializeField] private float healInterval = 2f;
    [SerializeField] private float healHitDelay = 0.5f;
    private float _healTimer;
    private Enemy _currentAllyToHeal;

    protected override bool CheckAggro()
    {
        FindAllyTarget();
        if (_currentAllyToHeal != null)
        {
            ChangeState(EnemyState.Combat); // Actually initiates their "heal" tracking mechanics
            return true;
        }
        return false;
    }

    protected override void BehaviorUpdate()
    {
        if (_currentAllyToHeal == null || _currentAllyToHeal.gameObject == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, _currentAllyToHeal.transform.position);
        
        if (dist > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, _currentAllyToHeal.transform.position, moveSpeed * Time.deltaTime);
            transform.LookAt(new Vector3(_currentAllyToHeal.transform.position.x, transform.position.y, _currentAllyToHeal.transform.position.z));
        }
        
        _healTimer -= Time.deltaTime;
        if (_healTimer <= 0)
        {
            if (animator != null) animator.SetTrigger("Attack01");
            _healTimer = healInterval;
            LockAttackState(1f, healHitDelay, () => {
                if (_currentAllyToHeal != null) _currentAllyToHeal.Heal(healAmount);
            }); // Fully shields iteration checks securely during cast cycle 
        }
    }

    private void FindAllyTarget()
    {
        if (_currentAllyToHeal != null && _currentAllyToHeal.gameObject != null) return;
        
        if (BattleManager.Instance != null)
        {
            _currentAllyToHeal = BattleManager.Instance.GetHealTarget(this, aggroRange);
        }
    }
}

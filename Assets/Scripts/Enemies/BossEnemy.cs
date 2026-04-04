using UnityEngine;

public class BossEnemy : Enemy
{
    [Header("Boss Mechanics")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballCooldown = 3f;
    private float _fireballTimer;

    protected override void BehaviorUpdate()
    {
        if (playerTarget == null) return;

        // Boss strictly tracks the player visually
        transform.LookAt(new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z));
        
        _fireballTimer -= Time.deltaTime;
        if (_fireballTimer <= 0 && Vector3.Distance(transform.position, playerTarget.position) <= attackRange)
        {
            ShootFireball();
            _fireballTimer = fireballCooldown;
        }
    }

    private void ShootFireball()
    {
        if (fireballPrefab != null)
        {
            // Usually spawned from a hand/mouth transform slot
            Instantiate(fireballPrefab, transform.position + transform.forward * 2f + Vector3.up * 2f, transform.rotation);
            Debug.Log($"{name} fired a devastating Fireball!");
        }
    }
}

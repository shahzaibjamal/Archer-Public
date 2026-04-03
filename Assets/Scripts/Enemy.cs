using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float health = 30f;

    private void Start()
    {
        GameEvents.TriggerEnemySpawned(this);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        GameEvents.TriggerEnemyDestroyed(this);
    }
}

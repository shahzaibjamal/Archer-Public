[System.Serializable]
public class EnemyData
{
    public EnemyType EnemyType;
    public string PrefabName;
    public float MaxHealth;
    public float MoveSpeed;
    public float AttackRange;
    public float AggroRange;
    public float Damage;
    public float BaseIdleTime;
    public float PatrolRange;
    public float AbilityCheckInterval;
    public int HitsToStun;

    // NavMeshAgent Settings
    public float AngularSpeed;
    public float Acceleration;
    public float StoppingDistance;

    // Block/Detection Settings
    public float BlockDetectionRadius;
    public float BlockAngleThreshold;
}

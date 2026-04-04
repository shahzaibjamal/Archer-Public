using UnityEngine;

public abstract class EnemyAbility : MonoBehaviour
{
    public float cooldown = 5f;
    [Range(0, 1)] public float chanceToTrigger = 0.5f;
    public float executionDuration = 1f;

    private float timer;

    public void TickCooldown(float dt)
    {
        if (timer > 0) timer -= dt;
    }

    public bool IsReady()
    {
        return timer <= 0f;
    }

    public void ResetCooldown()
    {
        timer = cooldown;
    }

    public void ExecuteOnStart(Enemy user)
    {
        ResetCooldown();
        Execute(user);
    }

    protected abstract void Execute(Enemy user);

    // Optional override for abilities that actually need to do processing/moving over time!
    public virtual void UpdateAbility(Enemy user) {}
}

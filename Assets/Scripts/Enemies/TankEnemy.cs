using UnityEngine;

public class TankEnemy : MeleeEnemy
{
    // Technically acts identical to the base Melee enemy pathing, but we enforce
    // massive HP and sluggishness directly in code just in case it's misconfigured in Inspector
    protected override void Start()
    {
        base.Start();
        
        moveSpeed *= 0.4f; // Extremely sluggish
        currentHealth *= 5f; // Extremely beefy
        maxHealth *= 5f;
    }
}

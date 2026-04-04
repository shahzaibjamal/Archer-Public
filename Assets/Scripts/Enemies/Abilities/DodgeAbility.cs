using UnityEngine;

public class DodgeAbility : EnemyAbility
{
    [SerializeField] private float dodgeDistance = 3f;

    protected override void Execute(Enemy user)
    {
        // Simple MVP sidestep dodge
        Vector3 rightOffset = user.transform.right * dodgeDistance;
        if (Random.value > 0.5f) rightOffset = -rightOffset; // Pick left or right

        user.transform.position += rightOffset;
        Debug.Log($"{user.name} Dodged!");
    }
}

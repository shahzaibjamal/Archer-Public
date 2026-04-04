using UnityEngine;

public class BuffAbility : EnemyAbility
{
    [SerializeField] private float speedBuffMultiplier = 1.5f;
    [SerializeField] private float damageBuffMultiplier = 1.3f;
    [SerializeField] private float buffDuration = 5f;
    
    // In a fully integrated system you would inject these directly into the 'damage'
    // and 'moveSpeed' fields of the user enemy during a Coroutine!
    protected override void Execute(Enemy user)
    {
        Debug.Log($"{user.name} applied offensive Buffs! Speed increased by {speedBuffMultiplier}, Damage multiplied by {damageBuffMultiplier}");
        // e.g. StartCoroutine(user.ApplyBuffs(speedBuffMultiplier, buffDuration));
    }
}

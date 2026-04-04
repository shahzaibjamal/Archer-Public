using UnityEngine;

public class SummonAbility : EnemyAbility
{
    [SerializeField] private GameObject minionPrefab;

    protected override void Execute(Enemy user)
    {
        if (minionPrefab != null)
        {
            Vector3 spawnPos = user.transform.position + user.transform.forward * 2f;
            Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"{user.name} Summoned a minion!");
        }
    }
}

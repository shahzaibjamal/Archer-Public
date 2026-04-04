using UnityEngine;

public class SummonAbility : EnemyAbility
{
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = 2; // Controls number!
    [SerializeField] private float sideOffsetSpacing = 1.5f;

    protected override void Execute(Enemy user)
    {
        if (minionPrefab != null)
        {
            if (user.EnemyAnimator != null) user.EnemyAnimator.SetTrigger("Attack02"); // Generic summon anim or explicit "Summon" string

            for (int i = 0; i < minionCount; i++)
            {
                // Alternates left and right precisely like the Clash Royale Witch!
                float sign = (i % 2 == 0) ? 1f : -1f;
                // Steps outwards iteratively if minionCount > 2
                float currentOffset = sideOffsetSpacing * ((i / 2) + 1);
                
                Vector3 spawnPos = user.transform.position + (user.transform.right * sign * currentOffset);
                Instantiate(minionPrefab, spawnPos, user.transform.rotation);
                Debug.Log($"{user.name} elegantly Summoned a minion at offset {currentOffset}!");
            }
        }
    }
}

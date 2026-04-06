using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ThrowAbility : EnemyAbility
{
    [SerializeField] private float throwRange = 12f;
    [SerializeField] private float throwHitDelay = 0.5f;

    protected override void Execute(Enemy user)
    {
        if (DataManager.Instance == null || DataManager.Instance.Metadata == null) return;
        if (user.GetPlayerTarget() == null) return;

        // Filter available throwables based on enemy level
        List<ThrowableData> available = DataManager.Instance.Metadata.Throwables.FindAll(t => t.MinLevel <= user.GetEnemyLevel());

        if (available.Count == 0) return;

        ThrowableData selected = available[Random.Range(0, available.Count)];
        Vector3 target = user.GetPlayerTarget().position;

        if (user.EnemyAnimator != null) user.EnemyAnimator.SetTrigger("Throw");

        // Small delay to match animation before spawning actual projectile
        StartCoroutine(ExecuteThrow(user, selected, target));
    }

    private IEnumerator ExecuteThrow(Enemy user, ThrowableData data, Vector3 target)
    {
        yield return new WaitForSeconds(throwHitDelay);

        GameObject projectileObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObj.GetComponent<Renderer>().material.color = Color.red; // Indicator
        projectileObj.transform.position = user.transform.position + Vector3.up * 1f;

        var proj = projectileObj.AddComponent<ThrowableProjectile>();
        proj.Launch(data, target);
    }
}

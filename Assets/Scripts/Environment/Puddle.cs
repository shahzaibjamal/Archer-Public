using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;

public enum PuddleType { Poison, Frozen, Block }

public class Puddle : MonoBehaviour
{
    private PuddleType type;
    private float duration;
    private float effectRadius;
    private float damagePerSecond;
    private float slowMultiplier;
    private bool blocksEnemies;

    private float timer;
    private HashSet<IDamageable> targetsInRange = new HashSet<IDamageable>();
    private HashSet<Collider> currentColliders = new HashSet<Collider>();
    private float damageTimer;

    public void Initialize(ThrowableData data)
    {
        if (System.Enum.TryParse(data.Type, out PuddleType pType))
            this.type = pType;

        this.duration = data.Duration;
        this.effectRadius = data.EffectRadius;
        this.damagePerSecond = data.DamagePerSecond;
        this.slowMultiplier = data.SlowMultiplier;
        this.blocksEnemies = data.BlocksEnemies;

        // Visuals using primitive
        transform.localScale = new Vector3(effectRadius * 2, 0.1f, effectRadius * 2);
        
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            switch (type)
            {
                case PuddleType.Poison: renderer.material.color = new Color(0.5f, 0, 0.5f, 0.5f); break;
                case PuddleType.Frozen: renderer.material.color = new Color(0, 0.5f, 1f, 0.5f); break;
                case PuddleType.Block: renderer.material.color = new Color(0.3f, 0.3f, 0.3f, 1f); break;
            }
        }

        // Trigger collider for area effects (slows, damage, detection)
        var trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.5f;

        // NavMesh blocking: Use "Not Walkable" (area 1) to REMOVE the mesh
        // This is the only way to block NavMeshAgent.Move() — the triangles must not exist.
        if (type == PuddleType.Block)
        {
            // Child object to avoid parent's flat scale squishing the volume
            GameObject modifierObj = new GameObject("NavBlockVolume");
            modifierObj.transform.SetParent(transform);
            modifierObj.transform.localPosition = Vector3.zero;
            modifierObj.transform.localRotation = Quaternion.identity;
            modifierObj.transform.localScale = Vector3.one;

            var modifier = modifierObj.AddComponent<NavMeshModifierVolume>();
            // Area 1 = "Not Walkable" — this REMOVES the NavMesh at this spot
            modifier.area = 1;
            modifier.size = new Vector3(effectRadius * 2.1f, 3, effectRadius * 2.1f);

            // Tell NavMeshManager to rebake all relevant surfaces
            if (NavMeshManager.Instance != null)
                NavMeshManager.Instance.RequestNavMeshUpdate();
        }

        timer = duration;
    }

    private void OnDestroy()
    {
        // Reset speed on any entities still inside
        foreach (var col in currentColliders)
        {
            if (col != null) ApplySlow(col, false);
        }

        // Rebake NavMesh to restore the walkable area
        if (type == PuddleType.Block && NavMeshManager.Instance != null)
            NavMeshManager.Instance.RequestNavMeshUpdate();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (damagePerSecond > 0)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= 1f)
            {
                damageTimer = 0f;
                foreach (var target in targetsInRange)
                {
                    if (target != null) target.TakeDamage(damagePerSecond);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentColliders.Contains(other)) return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null && !targetsInRange.Contains(damageable))
        {
            targetsInRange.Add(damageable);
            currentColliders.Add(other);
            ApplySlow(other, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!currentColliders.Contains(other)) return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null && targetsInRange.Contains(damageable))
        {
            targetsInRange.Remove(damageable);
            currentColliders.Remove(other);
            ApplySlow(other, false);
        }
    }

    private void ApplySlow(Collider other, bool apply)
    {
        // Block puddles don't apply slow — they physically remove the NavMesh
        if (type == PuddleType.Block) return;

        var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (pc != null || other.CompareTag("Player"))
        {
            if (pc == null) pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) pc.SetSpeedMultiplier(slowMultiplier, apply);
        }
        else if (other.GetComponentInParent<Enemy>())
        {
            other.GetComponentInParent<Enemy>().SetSpeedMultiplier(slowMultiplier, apply);
        }
    }
}

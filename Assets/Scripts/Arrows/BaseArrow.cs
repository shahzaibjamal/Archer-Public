using System;
using UnityEngine;

public abstract class BaseArrow : MonoBehaviour
{
    public Action<BaseArrow> OnDespawn;

    [SerializeField] protected float damage = 10f;
    [Header("VFX/SFX")]
    [SerializeField] protected GameObject hitVfxPrefab;
    [SerializeField] protected AudioClip hitSfx;

    protected float speed;
    protected float rangeSqr;
    protected Vector3 startPos;
    protected Vector3 moveDir;
    public bool IsEnemyProjectile { get; protected set; }

    public virtual void Launch(float speed, float range, Vector3? targetPos = null, bool isEnemyProjectile = false)
    {
        this.speed = speed;
        this.rangeSqr = range * range;
        IsEnemyProjectile = isEnemyProjectile;
        startPos = transform.position;

        if (targetPos.HasValue)
        {
            moveDir = (targetPos.Value - transform.position).normalized;
            if (moveDir != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(moveDir);
        }
        else
        {
            moveDir = transform.forward; // use current facing
        }
    }

    protected bool IsValidHit(Collider other, out IDamageable damageable)
    {
        damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            MonoBehaviour targetMb = (MonoBehaviour)damageable;
            
            // Safer: Check if the IDamageable belongs to the Player by checking component presence on itself or parent
            bool isPlayerHit = targetMb.GetComponentInParent<PlayerController>() != null || targetMb.CompareTag("Player");
            
            if (isPlayerHit && IsEnemyProjectile) return true;
            if (!isPlayerHit && !IsEnemyProjectile) return true;
        }
        return false;
    }

    protected void CheckRange()
    {
        if ((transform.position - startPos).sqrMagnitude >= rangeSqr)
        {
            OnMaxRangeReached();
        }
    }

    protected abstract void OnMaxRangeReached();

    protected void Poof()
    {
        if (hitVfxPrefab != null) Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
        if (hitSfx != null) AudioSource.PlayClipAtPoint(hitSfx, transform.position);
        
        if (OnDespawn != null)
            OnDespawn.Invoke(this);
        else 
            Destroy(gameObject);
    }
}

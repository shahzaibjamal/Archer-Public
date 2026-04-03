using System;
using UnityEngine;

public abstract class BaseArrow : MonoBehaviour
{
    public Action<BaseArrow> OnDespawn;

    [SerializeField] protected float damage = 10f;
    protected float speed;
    protected float rangeSqr;
    protected Vector3 startPos;

    public virtual void Launch(float speed, float range)
    {
        this.speed = speed;
        this.rangeSqr = range * range;
        startPos = transform.position;
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
        OnDespawn?.Invoke(this);
    }
}

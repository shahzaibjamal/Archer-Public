using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void UpdateLocomotion(float speed, float direction)
    {
        if (_animator == null) return;
        _animator.SetFloat("Speed", speed);
        _animator.SetFloat("Direction", direction);
    }

    public void UpdateAttackRates(float rangedSpeedMult, float meleeSpeedMult)
    {
        if (_animator == null) return;
        _animator.SetFloat("RangedAnimSpeed", rangedSpeedMult);
        _animator.SetFloat("MeleeAnimSpeed", meleeSpeedMult);
    }

    public void PlayAttack(string triggerName)
    {
        if (_animator != null) _animator.SetTrigger(triggerName);
    }

    public void PlayStun()
    {
        if (_animator != null) _animator.SetTrigger("Stun");
    }

    public void PlayTakeDamage()
    {
        if (_animator != null) _animator.SetTrigger("TakeDamage");
    }
}

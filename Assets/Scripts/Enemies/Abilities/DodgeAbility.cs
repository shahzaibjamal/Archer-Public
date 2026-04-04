using UnityEngine;

public class DodgeAbility : EnemyAbility
{
    [SerializeField] private float dodgeDistance = 3f;
    [SerializeField] private float dodgeSpeed = 10f;

    private Vector3 _targetDodgePos;

    protected override void Execute(Enemy user)
    {
        Vector3 rightOffset = user.transform.right * dodgeDistance;
        bool isRight = Random.value > 0.5f;
        if (isRight) rightOffset = -rightOffset; // Pick randomly left or right

        if (user.EnemyAnimator != null) user.EnemyAnimator.SetTrigger("Dodge" + (isRight ? "Right" : "Left")); // Triggers animation natively!

        _targetDodgePos = user.transform.position + rightOffset;
    }

    public override void UpdateAbility(Enemy user)
    {
        // Physically smoothly slides the enemy character over time across the FSM at the speed defined in the inspector natively!
        user.transform.position = Vector3.MoveTowards(user.transform.position, _targetDodgePos, dodgeSpeed * Time.deltaTime);
    }
}

using UnityEngine;

public class MeleeEnemy : Enemy
{
    protected override void Start()
    {
        base.Start();
    }

    protected override void BehaviorUpdate()
    {
        if (playerTarget == null) return;
        
        // FLATTEN THE TARGET Y COORDINATE TO ZERO OUT GROUND-CLIPPING
        Vector3 flatTargetPos = new Vector3(playerTarget.position.x, transform.position.y, playerTarget.position.z);
        float dist = Vector3.Distance(transform.position, flatTargetPos);

        if (dist <= aggroRange && dist > attackRange)
        {
            transform.position = Vector3.MoveTowards(transform.position, flatTargetPos, moveSpeed * Time.deltaTime);
            transform.LookAt(flatTargetPos);
        }
        else if (dist <= attackRange)
        {
            // Within strike range! Turn to face but do not MoveTowards physically into them!
            transform.LookAt(flatTargetPos);
        }
    }
}

using UnityEngine;

public static class TrajectoryPredictor
{
    /// <summary>
    /// Calculates where a target will be based on its velocity and the projectile's flight time.
    /// </summary>
    public static Vector3 GetPredictedTarget(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVelocity, float projectileSpeed, float skill)
    {
        if (targetVelocity.sqrMagnitude < 0.1f || skill <= 0f) return targetPos;

        // Pass 1: Initial estimate
        float dist1 = Vector3.Distance(shooterPos, targetPos);
        float time1 = dist1 / projectileSpeed;
        Vector3 guess1 = targetPos + (targetVelocity * time1 * skill);

        // Pass 2: Refined estimate (Iterative)
        float dist2 = Vector3.Distance(shooterPos, guess1);
        float time2 = dist2 / projectileSpeed;

        return targetPos + (targetVelocity * time2 * skill);
    }

    /// <summary>
    /// Specific version for Ballistic/Arc projectiles where time is NOT based on speed.
    /// </summary>
    public static Vector3 GetBallisticPredictedTarget(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVelocity, float arcHeight, float gravityMult, float skill)
    {
        if (targetVelocity.sqrMagnitude < 0.1f || skill <= 0f) return targetPos;

        float time = CalculateBallisticTime(shooterPos, targetPos, arcHeight, gravityMult);
        return targetPos + (targetVelocity * time * skill);
    }

    private static float CalculateBallisticTime(Vector3 start, Vector3 end, float arcHeight, float gMult)
    {
        float g = Mathf.Abs(Physics.gravity.y) * gMult;
        float h = Mathf.Max(start.y, end.y) + arcHeight;
        float hStart = Mathf.Max(0.1f, h - start.y);
        float hTarget = Mathf.Max(0.1f, h - end.y);

        return (Mathf.Sqrt(2 * g * hStart) / g) + (Mathf.Sqrt(2 * g * hTarget) / g);
    }
}
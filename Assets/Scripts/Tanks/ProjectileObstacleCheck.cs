using UnityEngine;

/// <summary>
/// Stops shells at authored foliage cover, including when they start inside it.
/// Cover is query-only so large tree canopies do not obstruct hull navigation.
/// </summary>
public static class ProjectileObstacleCheck
{
    private static readonly int CoverMask = LayerMask.GetMask("VegetationCover");
    private static readonly int ObstacleMask = LayerMask.GetMask("Obstacle", "VegetationCover");

    public static bool IsObstacle(Collider collider)
    {
        return collider != null && (ObstacleMask & (1 << collider.gameObject.layer)) != 0;
    }

    public static bool IsTrajectoryClear(Vector3 origin, Vector3 velocity, float duration, float radius)
    {
        if (Physics.CheckSphere(origin, radius, ObstacleMask, QueryTriggerInteraction.Collide)) return false;
        int steps = Mathf.Clamp(Mathf.CeilToInt(velocity.magnitude * duration), 4, 48);
        Vector3 previous = origin;
        for (int i = 1; i <= steps; i++)
        {
            float time = duration * i / steps;
            Vector3 next = origin + velocity * time + Physics.gravity * (0.5f * time * time);
            Vector3 segment = next - previous;
            float distance = segment.magnitude;
            if (distance > 0.0001f && Physics.SphereCast(previous, radius, segment / distance,
                    out _, distance, ObstacleMask, QueryTriggerInteraction.Collide)) return false;
            previous = next;
        }
        return true;
    }

    public static bool TryHitCover(Collider shell, Vector3 velocity, out Vector3 impactPosition)
    {
        impactPosition = shell.transform.position;
        Vector3 extents = shell.bounds.extents;
        float radius = Mathf.Max(0.02f, Mathf.Min(extents.x, extents.y, extents.z));
        if (Physics.CheckSphere(impactPosition, radius, CoverMask, QueryTriggerInteraction.Collide))
            return true;

        float distance = velocity.magnitude * Time.fixedDeltaTime;
        if (distance <= 0.0001f) return false;
        if (!Physics.SphereCast(impactPosition, radius, velocity.normalized, out RaycastHit hit,
                distance, CoverMask, QueryTriggerInteraction.Collide)) return false;
        impactPosition = hit.point;
        return true;
    }
}

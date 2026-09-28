using UnityEngine;

/// <summary>
/// Checks whether a target is inside the tank's field of view and unobstructed.
/// Navigation decisions remain in the AI controller.
/// </summary>
public sealed class EnemyTankPerception : MonoBehaviour
{
    [Header("Vision")]
    [SerializeField, Min(0.1f)] private float viewDistance = 30f;
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 120f;
    [SerializeField, Min(0f)] private float sensorHeight = 1f;
    [SerializeField] private LayerMask obstacleMask = 1 << 6;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 18f;

    public bool CanSeeTarget(Transform target, Collider targetCollider)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 origin = GetSensorPosition();
        Vector3 toTarget = GetTargetPosition(target, targetCollider) - origin;
        float distance = toTarget.magnitude;

        if (distance < 0.01f || distance > viewDistance)
        {
            return false;
        }

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 horizontalDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f || horizontalDirection.sqrMagnitude < 0.0001f ||
            Vector3.Angle(forward, horizontalDirection) > fieldOfView * 0.5f)
        {
            return false;
        }

        return !Physics.Raycast(
            origin,
            toTarget / distance,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
    }

    public bool IsWithinAttackRange(Transform target, Collider targetCollider)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 toTarget = GetTargetPosition(target, targetCollider) - GetSensorPosition();
        return toTarget.sqrMagnitude <= attackRange * attackRange;
    }

    private Vector3 GetSensorPosition()
    {
        return transform.position + Vector3.up * sensorHeight;
    }

    private static Vector3 GetTargetPosition(Transform target, Collider targetCollider)
    {
        return targetCollider != null && targetCollider.enabled
            ? targetCollider.bounds.center
            : target.position + Vector3.up;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Checks a proposed tank pose before kinematic movement or player input can overlap another hull.
/// </summary>
public static class TankCollisionGuard
{
    private const float PenetrationTolerance = 0.001f;

    private struct ReservedPose
    {
        public BoxCollider Hull;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private static readonly List<ReservedPose> reservedPoses = new();
    private static double reservationTime = double.NaN;

    public static bool IsBlocking(Collider other, Transform tank, LayerMask obstacleMask)
    {
        if (other == null || other.transform.IsChildOf(tank))
        {
            return false;
        }

        bool isObstacle = (obstacleMask.value & (1 << other.gameObject.layer)) != 0;
        bool isTank = other.GetComponentInParent<TankHealth>() != null;
        return isObstacle || isTank;
    }

    public static bool CanOccupy(
        BoxCollider hull, Vector3 position, Quaternion rotation, LayerMask obstacleMask)
    {
        RefreshReservations();

        Vector3 scale = hull.transform.lossyScale;
        Vector3 scaledCenter = Vector3.Scale(hull.center, scale);
        Vector3 scaledSize = Vector3.Scale(hull.size, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        Vector3 center = position + rotation * scaledCenter;
        float searchRadius = scaledSize.magnitude * 0.5f;

        foreach (Collider other in Physics.OverlapSphere(
                     center, searchRadius, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            if (!IsBlocking(other, hull.transform, obstacleMask) ||
                !Physics.ComputePenetration(
                    hull, position, rotation,
                    other, other.transform.position, other.transform.rotation,
                    out _, out float proposedDepth) ||
                proposedDepth <= PenetrationTolerance)
            {
                continue;
            }

            // Allow a tank that is already touching another collider to move away from it.
            bool currentlyOverlapping = Physics.ComputePenetration(
                hull, hull.transform.position, hull.transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out _, out float currentDepth);
            if (!currentlyOverlapping || proposedDepth > currentDepth + PenetrationTolerance)
            {
                return false;
            }
        }

        foreach (ReservedPose reserved in reservedPoses)
        {
            if (reserved.Hull == null || reserved.Hull == hull)
            {
                continue;
            }

            if (Physics.ComputePenetration(
                    hull, position, rotation,
                    reserved.Hull, reserved.Position, reserved.Rotation,
                    out _, out float overlap) && overlap > PenetrationTolerance)
            {
                return false;
            }
        }

        return true;
    }

    public static void ReservePose(BoxCollider hull, Vector3 position, Quaternion rotation)
    {
        RefreshReservations();
        for (int i = 0; i < reservedPoses.Count; i++)
        {
            if (reservedPoses[i].Hull == hull)
            {
                reservedPoses.RemoveAt(i);
                break;
            }
        }

        reservedPoses.Add(new ReservedPose
        {
            Hull = hull,
            Position = position,
            Rotation = rotation
        });
    }

    private static void RefreshReservations()
    {
        double fixedTime = Time.fixedTimeAsDouble;
        if (fixedTime == reservationTime)
        {
            return;
        }

        reservedPoses.Clear();
        reservationTime = fixedTime;
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies one radial damage pulse to each tank except the tank that fired it.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public sealed class ExplosionDamage : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(1)] private int maximumDamage = 40;
    [SerializeField, Min(0.01f)] private float activeDuration = 0.15f;
    [SerializeField, Min(0.1f)] private float visualDuration = 2f;

    private readonly HashSet<TankHealth> damagedTanks = new();
    private SphereCollider areaCollider;
    private GameObject source;
    private TankHealth sourceTank;

    private void Awake()
    {
        areaCollider = GetComponent<SphereCollider>();
        areaCollider.isTrigger = true;
        areaCollider.radius = radius;
    }

    private void Start()
    {
        // A newly created trigger can already overlap a tank before OnTriggerEnter runs.
        foreach (Collider other in Physics.OverlapSphere(
                     transform.position, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            TryDamage(other);
        }

        Invoke(nameof(DisableAreaCollider), activeDuration);
        Destroy(gameObject, visualDuration);
    }

    private void DisableAreaCollider()
    {
        areaCollider.enabled = false;
    }

    public void SetSource(GameObject damageSource)
    {
        source = damageSource;
        sourceTank = source != null ? source.GetComponentInParent<TankHealth>() : null;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        if (!areaCollider.enabled)
        {
            return;
        }

        TankHealth health = other.GetComponentInParent<TankHealth>();
        // Compare individual tanks, not teams: another enemy's shot still deals damage.
        if (health == null || health == sourceTank || health.IsDead || damagedTanks.Contains(health))
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, other.ClosestPoint(transform.position));
        if (distance >= radius)
        {
            return;
        }

        int damage = Mathf.CeilToInt(maximumDamage * (1f - distance / radius));
        if (damage > 0)
        {
            damagedTanks.Add(health);
            health.ApplyDamage(damage, source);
        }
    }
}

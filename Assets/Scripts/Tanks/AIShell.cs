using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class AIShell : MonoBehaviour
{
    public GameObject explosion;

    private Rigidbody body;
    private Collider shellCollider;
    private GameObject owner;
    private int ownerGeneration;
    private Vector3 velocityBeforePause;
    private bool wasPaused;
    private bool exploded;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        shellCollider = GetComponent<Collider>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void SetOwner(GameObject tank)
    {
        owner = tank;
        ownerGeneration = owner != null && owner.TryGetComponent(out AITank attacker)
            ? attacker.WeaponGeneration : 0;
        if (owner == null || shellCollider == null)
        {
            return;
        }

        foreach (Collider tankCollider in owner.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(shellCollider, tankCollider);
        }
    }

    private void FixedUpdate()
    {
        if (exploded) return;
        bool isPaused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        if (isPaused)
        {
            if (!wasPaused)
            {
                velocityBeforePause = body.linearVelocity;
                body.useGravity = false;
                wasPaused = true;
            }

            body.linearVelocity = Vector3.zero;
            return;
        }

        if (wasPaused)
        {
            body.useGravity = true;
            body.linearVelocity = velocityBeforePause;
            wasPaused = false;
        }

        if (body.linearVelocity.sqrMagnitude > 0.001f)
        {
            if (ProjectileObstacleCheck.TryHitCover(shellCollider, body.linearVelocity, out Vector3 impact))
            {
                ExplodeAt(impact, true);
                return;
            }
            body.MoveRotation(Quaternion.LookRotation(body.linearVelocity));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded || (PauseManager.Instance != null && PauseManager.Instance.IsPaused) ||
            owner != null && collision.transform.IsChildOf(owner.transform))
        {
            return;
        }

        ExplodeAt(transform.position, ProjectileObstacleCheck.IsObstacle(collision.collider));
    }

    private void ExplodeAt(Vector3 position, bool blockedByObstacle = false)
    {
        if (exploded) return;
        exploded = true;
        shellCollider.enabled = false;
        body.linearVelocity = Vector3.zero;
        if (blockedByObstacle && owner != null && owner.TryGetComponent(out AITank attacker))
            attacker.NotifyBlockedShot(ownerGeneration);
        if (explosion != null)
        {
            GameObject effect = Instantiate(explosion, position, Quaternion.identity);
            if (effect.TryGetComponent(out ExplosionDamage damageArea))
            {
                damageArea.SetSource(owner);
            }
        }

        Destroy(gameObject);
    }
}

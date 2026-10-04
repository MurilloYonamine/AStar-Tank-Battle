using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class Shell : MonoBehaviour
{
    public GameObject explosion;

    [SerializeField, Min(0.1f)] private float initialSpeed = 30f;
    [SerializeField, Min(0f)] private float drag = 1f;
    [SerializeField, Min(0f)] private float downwardAcceleration = 9.8f;

    private Rigidbody body;
    private Collider shellCollider;
    private GameObject owner;
    private float currentSpeed;
    private float verticalSpeed;
    private bool exploded;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        shellCollider = GetComponent<Collider>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        currentSpeed = initialSpeed;
    }

    private void FixedUpdate()
    {
        if (exploded) return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            body.linearVelocity = Vector3.zero;
            return;
        }

        currentSpeed *= Mathf.Max(0f, 1f - Time.fixedDeltaTime * drag);
        verticalSpeed -= downwardAcceleration * Time.fixedDeltaTime;
        body.linearVelocity = transform.forward * currentSpeed + Vector3.up * verticalSpeed;
        if (ProjectileObstacleCheck.TryHitCover(shellCollider, body.linearVelocity, out Vector3 impact))
            ExplodeAt(impact);
    }

    public void SetOwner(GameObject tank)
    {
        owner = tank;
        if (owner == null || shellCollider == null)
        {
            return;
        }

        foreach (Collider tankCollider in owner.GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(shellCollider, tankCollider);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded || (PauseManager.Instance != null && PauseManager.Instance.IsPaused) ||
            owner != null && collision.transform.IsChildOf(owner.transform))
        {
            return;
        }

        ExplodeAt(transform.position);
    }

    private void ExplodeAt(Vector3 position)
    {
        if (exploded) return;
        exploded = true;
        shellCollider.enabled = false;
        body.linearVelocity = Vector3.zero;
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

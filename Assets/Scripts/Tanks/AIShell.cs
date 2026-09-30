using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class AIShell : MonoBehaviour
{
    public GameObject explosion;

    private Rigidbody body;
    private Collider shellCollider;
    private GameObject owner;
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
            body.MoveRotation(Quaternion.LookRotation(body.linearVelocity));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (exploded || owner != null && collision.transform.IsChildOf(owner.transform))
        {
            return;
        }

        exploded = true;
        if (explosion != null)
        {
            GameObject effect = Instantiate(explosion, transform.position, Quaternion.identity);
            if (effect.TryGetComponent(out ExplosionDamage damageArea))
            {
                damageArea.SetSource(owner);
            }
        }

        Destroy(gameObject);
    }
}

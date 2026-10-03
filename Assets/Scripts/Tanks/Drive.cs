using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Drive : MonoBehaviour
{
    public float speed = 5.0f;           // World units per second.
    public float rotationSpeed = 100.0f; // Degrees per second.
    public bool invertRotationWhenBackwards = true;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference rotateUp;
    [SerializeField] private InputActionReference rotateDown;

    [Header("Combat")]
    public Transform cannon;
    public Transform bulletSpawn;
    public GameObject bulletPrefab;
    [Tooltip("Starting upward pitch in degrees; manual elevation remains available.")]
    [SerializeField, Range(-10f, 30f)] private float initialElevation = 5f;
    [SerializeField, Range(-20f, 0f)] private float minimumElevation = -5f;
    [SerializeField, Range(0f, 60f)] private float maximumElevation = 30f;

    private InputAction fireAction;
    private Rigidbody body;
    private BoxCollider hull;
    private Vector2 moveInput;
    private float elevation;
    private Quaternion cannonRestRotation;
    private readonly LayerMask obstacleMask = 1 << 6;
    private const float CollisionSkin = 0.05f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hull = GetComponent<BoxCollider>();
        body.isKinematic = false;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (cannon != null)
        {
            cannonRestRotation = cannon.localRotation;
            ResetAim();
        }

        if (moveAction != null && moveAction.action != null)
        {
            fireAction = moveAction.action.actionMap.FindAction("Attack");
        }
    }

    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(rotateUp);
        EnableAction(rotateDown);
        fireAction?.Enable();
    }

    private void OnDisable()
    {
        DisableAction(moveAction);
        DisableAction(rotateUp);
        DisableAction(rotateDown);
        fireAction?.Disable();
        moveInput = Vector2.zero;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (moveAction == null || moveAction.action == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = moveAction.action.ReadValue<Vector2>();

        if (IsPressed(rotateUp))
        {
            elevation += 30f * Time.deltaTime;
        }
        else if (IsPressed(rotateDown))
        {
            elevation -= 30f * Time.deltaTime;
        }

        elevation = Mathf.Clamp(elevation, minimumElevation, maximumElevation);
        if (cannon != null)
        {
            cannon.localRotation = cannonRestRotation * Quaternion.Euler(-elevation, 0f, 0f);
        }

        if (fireAction != null && fireAction.WasPressedThisFrame())
        {
            GameObject projectile = Instantiate(bulletPrefab, bulletSpawn.position,
                Quaternion.LookRotation(cannon.forward));
            if (projectile.TryGetComponent(out Shell shell))
            {
                shell.SetOwner(gameObject);
            }
        }
    }

    public void ResetAim()
    {
        elevation = Mathf.Clamp(initialElevation, minimumElevation, maximumElevation);
        if (cannon != null)
        {
            cannon.localRotation = cannonRestRotation * Quaternion.Euler(-elevation, 0f, 0f);
        }
    }

    public void SetWeaponTransforms(Transform turret, Transform muzzle)
    {
        cannon = turret;
        bulletSpawn = muzzle;
        cannonRestRotation = cannon.localRotation;
        ResetAim();
    }

    private void FixedUpdate()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            return;
        }

        float turnInput = invertRotationWhenBackwards && moveInput.y < 0f
            ? -moveInput.x
            : moveInput.x;
        Quaternion nextRotation = body.rotation *
            Quaternion.Euler(0f, turnInput * rotationSpeed * Time.fixedDeltaTime, 0f);

        if (TankCollisionGuard.CanOccupy(hull, body.position, nextRotation, obstacleMask))
        {
            body.MoveRotation(nextRotation);
        }
        else
        {
            nextRotation = body.rotation;
        }

        float requestedSpeed = moveInput.y * speed;
        if (Mathf.Abs(requestedSpeed) < 0.001f)
        {
            body.linearVelocity = Vector3.zero;
            TankCollisionGuard.ReservePose(hull, body.position, nextRotation);
            return;
        }

        Vector3 direction = nextRotation * Vector3.forward * Mathf.Sign(requestedSpeed);
        float allowedStep = Mathf.Abs(requestedSpeed) * Time.fixedDeltaTime;
        foreach (RaycastHit hit in body.SweepTestAll(
                     direction, allowedStep + CollisionSkin, QueryTriggerInteraction.Ignore))
        {
            if (TankCollisionGuard.IsBlocking(hit.collider, transform, obstacleMask))
            {
                allowedStep = Mathf.Min(allowedStep, Mathf.Max(0f, hit.distance - CollisionSkin));
            }
        }

        Vector3 nextPosition = body.position + direction * allowedStep;
        if (!TankCollisionGuard.CanOccupy(hull, nextPosition, nextRotation, obstacleMask))
        {
            allowedStep = 0f;
            nextPosition = body.position;
        }

        body.linearVelocity = direction * (allowedStep / Time.fixedDeltaTime);
        TankCollisionGuard.ReservePose(hull, nextPosition, nextRotation);
    }

    private static bool IsPressed(InputActionReference actionReference)
    {
        return actionReference != null &&
               actionReference.action != null &&
               actionReference.action.IsPressed();
    }

    private static void EnableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Enable();
        }
    }

    private static void DisableAction(InputActionReference actionReference)
    {
        if (actionReference != null && actionReference.action != null)
        {
            actionReference.action.Disable();
        }
    }
}

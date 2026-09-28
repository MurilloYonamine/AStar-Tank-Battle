using UnityEngine;
using UnityEngine.InputSystem;

public class Drive : MonoBehaviour
{
    public float speed = 5.0f;           // 5 metros por segundo
    public float rotationSpeed = 100.0f; // 100 graus por segundo
    public bool invertRotationWhenBackwards = true;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference rotateUp;
    [SerializeField] private InputActionReference rotateDown;

    [Header("Combat")]
    public Transform cannon;
    public Transform bulletSpawn;
    public GameObject bulletPrefab;

    private InputAction fireAction;

    private void Awake()
    {
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
    }

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        if (moveAction == null || moveAction.action == null)
        {
            return;
        }

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        if (invertRotationWhenBackwards)
        {
            moveInput.x = moveInput.y < 0 ? -moveInput.x : moveInput.x;
        }

        Vector3 newDirection = new Vector3(0f, 0f, moveInput.y).normalized;
        Vector3 newRotation = new Vector3(0f, moveInput.x, 0f).normalized;

        transform.Translate(newDirection * speed * Time.deltaTime);
        transform.Rotate(newRotation * rotationSpeed * Time.deltaTime);

        if (IsPressed(rotateUp))
        {
            cannon.RotateAround(cannon.position, cannon.right, -30 * Time.deltaTime);
        }
        else if (IsPressed(rotateDown))
        {
            cannon.RotateAround(cannon.position, cannon.right, 30 * Time.deltaTime);
        }

        if (fireAction != null && fireAction.WasPressedThisFrame())
        {
            Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);
        }
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

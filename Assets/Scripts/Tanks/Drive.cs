using UnityEngine;
using UnityEngine.InputSystem;

public class Drive : MonoBehaviour {
    public float speed = 5.0f;           // 5 metros por segundo
    public float rotationSpeed = 100.0f; // 100 graus por segundo
    public bool invertRotationWhenBackwards = true;
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference rotateUp;
    [SerializeField] InputActionReference rotateDown;

    public Transform cannon;
    public Transform bulletSpawn;
    public GameObject bulletPrefab;

    private void OnEnable() { 
        if (moveAction != null) moveAction.action.Enable(); 
        if (rotateUp != null) rotateUp.action.Enable();
        if (rotateDown != null) rotateDown.action.Enable();
    }
    private void OnDisable() { 
        if (moveAction != null) moveAction.action.Disable();
        if (rotateUp != null) rotateUp.action.Disable();
        if (rotateDown != null) rotateDown.action.Disable();
    }

    void Update() {
        if (moveAction == null) return;

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        if (invertRotationWhenBackwards)
            moveInput.x = moveInput.y < 0 ? -moveInput.x : moveInput.x;

        Vector3 newDirection = new Vector3(0f, 0f, moveInput.y).normalized;
        Vector3 newRotation = new Vector3(0f, moveInput.x, 0f).normalized;

        transform.Translate(newDirection * speed * Time.deltaTime);
        transform.Rotate(newRotation * rotationSpeed * Time.deltaTime);

        if (rotateUp.action.IsPressed())
        {
            cannon.RotateAround(cannon.position, cannon.right, -30 * Time.deltaTime);
        }
        else if (rotateDown.action.IsPressed()) 
        { 
            cannon.RotateAround(cannon.position, cannon.right, 30 * Time.deltaTime);
        }

        if(Mouse.current.leftButton.wasPressedThisFrame)
        {
            Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);
        }
    }
}
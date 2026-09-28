using UnityEngine;
using UnityEngine.InputSystem;

public class FireShell : MonoBehaviour
{
    public GameObject bullet;
    public GameObject turret;
    public GameObject enemy;

    [SerializeField] private InputActionReference fireAction;

    private void OnEnable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Disable();
        }
    }

    private void CreateBullet()
    {
        Instantiate(bullet, turret.transform.position, turret.transform.rotation);
    }

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
        {
            return;
        }

        if (fireAction == null ||
            fireAction.action == null ||
            !fireAction.action.WasPressedThisFrame())
        {
            return;
        }

        Vector3 aimAt = CalculateTrajectory();

        if (aimAt != Vector3.zero)
        {
            transform.forward = aimAt;
            CreateBullet();
        }
    }

    private Vector3 CalculateTrajectory()
    {
        Vector3 p = enemy.transform.position - transform.position;
        Vector3 v = enemy.transform.forward * enemy.GetComponent<Drive>().speed;
        float s = bullet.GetComponent<MoveShell>().speed;

        float a = Vector3.Dot(v, v) - s * s;
        float b = Vector3.Dot(p, v);
        float c = Vector3.Dot(p, p);
        float d = b * b - a * c;

        if (d < 0.1f)
        {
            return Vector3.zero;
        }

        float sqrt = Mathf.Sqrt(d);
        float t1 = (-b - sqrt) / c;
        float t2 = (-b + sqrt) / c;

        if (t1 < 0.0f && t2 < 0.0f)
        {
            return Vector3.zero;
        }

        float t;

        if (t1 < 0.0f)
        {
            t = t2;
        }
        else if (t2 < 0.0f)
        {
            t = t1;
        }
        else
        {
            t = Mathf.Max(t1, t2);
        }

        return t * p + v;
    }
}

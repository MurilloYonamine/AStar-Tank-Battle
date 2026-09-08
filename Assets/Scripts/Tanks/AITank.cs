using UnityEngine;
using UnityEngine.InputSystem;

public class AITank : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject bulletSpawn;
    public GameObject enemy;
    public Transform cannon;
    public float rotationSpeed = 2.0f;
    float speed = 15.0f;
    float moveSpeed = 3.0f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = (enemy.transform.position - transform.position).normalized;        
        direction.y = 0f; // para não rotacionar em x
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            CreateBullet();
        }
        float? angle = RotateCannon();

        if(angle != null)
        {
            CreateBullet();
        }
        else
        {
            transform.Translate(0, 0, Time.deltaTime * moveSpeed);
        }
    }

    void CreateBullet()
    {
        GameObject shell = Instantiate(bulletPrefab, bulletSpawn.transform.position, bulletSpawn.transform.rotation);
        shell.GetComponent<Rigidbody>().linearVelocity = speed * cannon.forward;
    }

    float? RotateCannon()
    {
        float? angle = CalculateAngle(true);
        if(angle != null)
        {
            cannon.localEulerAngles = new Vector3(360f - (float)angle, 0f, 0f);
        }
        return angle;
    }

    float? CalculateAngle(bool low)
    {
        float angle = 0.0f;
        Vector3 targetDir = enemy.transform.position - transform.position;
        float y = targetDir.y;
        targetDir.y = 0;
        float x = targetDir.magnitude - 1;
        float gravity = 9.81f;
        float sSqr = speed * speed;
        float underTheRoot = sSqr * sSqr - gravity * (gravity * x * x + 2 * y * sSqr);

        if(underTheRoot >= 0)
        {
            float root = Mathf.Sqrt(underTheRoot);
            float highAngle = sSqr + root;
            float lowAngle = sSqr - root;
            if (low)
            {
                angle = Mathf.Atan2(lowAngle, gravity * x) * Mathf.Rad2Deg;
                return angle;
            }
            else
            {
                angle = Mathf.Atan2(highAngle, gravity * x) * Mathf.Rad2Deg;
                return angle;
            }
        }
        else
            return null;
    }
}

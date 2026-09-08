using UnityEngine;

public class AIShell : MonoBehaviour
{
    public GameObject explosion;
    Rigidbody body;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "tank")
        {
            GameObject exp = Instantiate(explosion, transform.position, Quaternion.identity);
            Destroy(exp, 0.5f);
            Destroy(gameObject);
        }
    }
    void Start()
    {
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.forward = body.linearVelocity;
    }
}

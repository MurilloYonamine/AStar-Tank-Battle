using UnityEngine;

public class Shell : MonoBehaviour {

    public GameObject explosion;
    float speed = 0.0f;
    float mass = 1.0f;
    float force = 30.0f;
    float drag = 1.0f;
    float acceleration;
    float ySpeed = 0.0f; 
    float gravity = -9.8f;
    float gravityAcceleration = 0.0f;

    void OnCollisionEnter(Collision col) {

        if (col.gameObject.tag == "tank") {
            GameObject exp = Instantiate(explosion, this.transform.position, Quaternion.identity);
            Destroy(exp, 0.5f);
            Destroy(this.gameObject);
        }
    }

    private void Start() 
    {
        acceleration = force / mass;
        speed += acceleration;
        gravityAcceleration = gravity / mass;
    }

    void Update() 
    {
        speed *= (1 - Time.deltaTime * drag);
        ySpeed += gravityAcceleration * Time.deltaTime * 0.01f;
        transform.Translate(0, ySpeed, speed * Time.deltaTime);
    }
}

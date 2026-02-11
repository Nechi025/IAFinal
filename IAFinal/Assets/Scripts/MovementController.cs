using UnityEngine;

public class MovementController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 8f;
    public float arriveRadius = 0.5f;

    [Header("Obstacle Avoidance")]
    public float avoidDistance = 2f;
    public float avoidForce = 15f;
    public LayerMask obstacleMask;

    private Vector3 velocity;
    private Rigidbody rb;

    [Header("Target")]
    public Transform target;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (target == null)
            return;

        Vector3 steering = Vector3.zero;

        steering += Seek(target.position);
        steering += ObstacleAvoidance();

        ApplyMovement(steering);
    }

    //Steering Seek
    Vector3 Seek(Vector3 targetPos)
    {
        Vector3 desired = targetPos - rb.position;
        float distance = desired.magnitude;

        if (distance < arriveRadius)
            return Vector3.zero;

        desired.Normalize();
        desired *= maxSpeed;

        Vector3 steering = desired - velocity;
        return Vector3.ClampMagnitude(steering, maxForce);
    }

    void ApplyMovement(Vector3 steering)
    {
        velocity += steering * Time.deltaTime;
        velocity.y = 0f;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        rb.MovePosition(rb.position + velocity * Time.deltaTime);

        if (velocity.sqrMagnitude > 0.001f)
        {
            rb.MoveRotation(Quaternion.LookRotation(velocity));
        }
    }

    Vector3 ObstacleAvoidance()
    {
        Vector3 avoidance = Vector3.zero;
        RaycastHit hit;

        Vector3 origin = rb.position;
        origin.y = 0.5f;

        if (Physics.Raycast(origin, transform.forward, out hit, avoidDistance, obstacleMask))
        {
            //Esto para que no tome el eje Y
            Vector3 normal = hit.normal;
            normal.y = 0f;
            normal.Normalize();

            Vector3 away = Vector3.Reflect(transform.forward, normal);
            away.y = 0f;

            avoidance += away.normalized * avoidForce;
        }

        Debug.DrawRay(origin, transform.forward * avoidDistance, Color.red);

        return avoidance;
    }

}

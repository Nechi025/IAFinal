using UnityEngine;

public class Leader : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 8f;
    public float arriveRadius = 0.5f;
    public float drag = 4f; //Para no patinar

    [Header("Vision")]
    public float viewDistance = 8f;
    public float viewAngle = 120f;
    public LayerMask obstacleMask;
    public LayerMask enemyMask;

    [Header("Obstacle Avoidance")]
    public float avoidDistance = 2f;
    public float avoidForce = 15f;

    public Vector3 velocity;
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

        Transform enemy = GetVisibleEnemy();

        if (enemy != null)
        {
            Debug.Log(name + " ve a " + enemy.name);
        }

        Vector3 steering = Vector3.zero;

        steering += Seek(target.position);
        steering += ObstacleAvoidance();

        steering = Vector3.ClampMagnitude(steering, maxForce);

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

        velocity = Vector3.Lerp(velocity, Vector3.zero, drag * Time.deltaTime);
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

    Transform GetVisibleEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(rb.position, viewDistance, enemyMask);

        foreach (var hit in hits)
        {
            Vector3 dirToTarget = hit.transform.position - rb.position;
            dirToTarget.y = 0f;

            float angle = Vector3.Angle(transform.forward, dirToTarget);

            //Revisa angulo
            if (angle < viewAngle / 2f)
            {
                //Tira raycast
                RaycastHit rayHit;
                Vector3 origin = rb.position + Vector3.up * 0.5f;

                if (Physics.Raycast(origin, dirToTarget.normalized, out rayHit, viewDistance))
                {
                    //Verifica que lo primero que toca es el enemigo
                    if (rayHit.collider == hit)
                    {
                        return hit.transform;
                    }
                }
            }
        }

        return null;
    }

    void OnDrawGizmosSelected()
    {
        //Linea de vision
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, left * viewDistance);
        Gizmos.DrawRay(transform.position, right * viewDistance);
    }
}

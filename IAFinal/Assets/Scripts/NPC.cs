using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 8f;
    public float arriveRadius = 0.5f;

    [Header("Obstacle Avoidance")]
    public float avoidDistance = 2f;
    public float avoidForce = 15f;
    public LayerMask obstacleMask;

    public Vector3 velocity;
    private Rigidbody rb;

    [Header("Leader")]
    public Transform leader;

    [Header("Flocking")]
    public float neighborRadius = 3f;
    public float separationRadius = 1.2f;

    public float cohesionWeight = 1f;
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;

    public LayerMask npcMask;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (leader == null)
            return;

        Vector3 steering = Vector3.zero;

        Collider[] neighbors = GetNeighbors();

        steering += FollowLeader();
        steering += Cohesion(neighbors) * cohesionWeight;
        steering += Separation(neighbors) * separationWeight;
        steering += Alignment(neighbors) * alignmentWeight;
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

    Vector3 FollowLeader()
    {
        Vector3 toLeader = leader.position - rb.position;
        toLeader.y = 0f;

        float distance = toLeader.magnitude;

        if (distance < 1.5f) // distancia mínima
            return Vector3.zero;

        return Seek(leader.position);
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

    //Lo mantiene cerca del grupo
    Vector3 Cohesion(Collider[] neighbors)
    {
        if (neighbors.Length == 0)
            return Vector3.zero;

        Vector3 center = Vector3.zero;
        int count = 0;

        //Calcula el centro promedio de todos los vecinos
        foreach (var n in neighbors)
        {
            if (n.gameObject == gameObject) continue;

            center += n.transform.position;
            count++;
        }

        if (count == 0) return Vector3.zero;

        center /= count;
        Vector3 desired = center - rb.position;
        desired.y = 0f;

        return desired.normalized * maxSpeed - velocity;
    }

    //Para no chocar entre ellos
    Vector3 Separation(Collider[] neighbors)
    {
        Vector3 force = Vector3.zero;

        foreach (var n in neighbors)
        {
            if (n.gameObject == gameObject) continue;

            Vector3 diff = rb.position - n.transform.position;
            diff.y = 0f;

            float dist = diff.magnitude;

            //Movimiento mas fluido
            if (dist < 0.01f) continue;

            if (dist < separationRadius)
            {
                //Mientras mas cerca, mas fuerte la separacion
                force += diff.normalized / dist;
            }
        }

        return force * maxSpeed - velocity;
    }

    //Direccion promedio
    Vector3 Alignment(Collider[] neighbors)
    {
        if (neighbors.Length == 0)
            return Vector3.zero;

        Vector3 avgVelocity = Vector3.zero;
        int count = 0;

        foreach (var n in neighbors)
        {
            if (n.gameObject == gameObject) continue;

            NPC mc = n.GetComponent<NPC>();
            if (mc == null) continue;

            avgVelocity += mc.velocity;
            count++;
        }

        if (count == 0) return Vector3.zero;

        avgVelocity /= count;
        avgVelocity.y = 0f;

        return avgVelocity.normalized * maxSpeed - velocity;
    }

    //Obtiene los vecinos que esten dentro del radio
    Collider[] GetNeighbors()
    {
        return Physics.OverlapSphere(rb.position, neighborRadius, npcMask);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, neighborRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}

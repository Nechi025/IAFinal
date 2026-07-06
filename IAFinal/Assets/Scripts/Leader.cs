using Unity.VisualScripting;
using UnityEngine;

public class Leader : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 8f;
    public float arriveRadius = 0.5f;
    public float drag = 4f; //Para no patinar
    public float panicDistance = 6f;

    [Header("Vision")]
    public float viewDistance = 8f;
    public float viewAngle = 120f;
    public LayerMask obstacleMask;
    public LayerMask allyMask;
    public LayerMask enemyMask;

    public float awarenessRadius = 10f;
    private Animator anim;

    [Header("Obstacle Avoidance")]
    public float avoidDistance = 2f;
    public float avoidForce = 15f;

    public Vector3 velocity;
    private Rigidbody rb;

    [Header("Target")]
    public Transform target;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float currentHealth;

    public float fleeThreshold = 30f;

    //private Transform currentEnemy;
    //private Vector3 lastKnownEnemyPosition;

    [Header("Leader AI")]
    public float decisionInterval = 2f;
    private float decisionTimer;
    public Transform safePoint;

    public float retreatHealthThreshold = 40f;
    public enum LeaderState
    {
        Advance,
        Attack,
        Regroup,
        Defensive,
        Retreat
    }

    public LeaderState currentLeaderState;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0f)
        {
            DecideNextState();
            decisionTimer = decisionInterval;
        }

        Vector3 steering = Vector3.zero;

        switch (currentLeaderState)
        {
            case LeaderState.Advance:
                steering = AdvanceState();
                break;

            case LeaderState.Attack:
                steering = AttackState();
                break;

            case LeaderState.Regroup:
                steering = RegroupState();
                break;

            case LeaderState.Defensive:
                steering = DefensiveState();
                break;

            case LeaderState.Retreat:
                steering = RetreatState();
                break;
        }

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

    Transform GetClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(rb.position, viewDistance, enemyMask);

        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(rb.position, hit.transform.position);

            if (dist < minDist && dist < panicDistance)
            {
                minDist = dist;
                closest = hit.transform;
            }
        }

        return closest;
    }

    Vector3 AdvanceState()
    {
        return Seek(target.position);
    }
    Vector3 AttackState()
    {
        Transform enemy = GetVisibleEnemy();

        if (enemy != null)
            return Seek(enemy.position);

        return AdvanceState();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    Vector3 RegroupState()
    {
        // quedarse quieto o moverse lento
        return Vector3.zero;
    }
    Vector3 DefensiveState()
    {
        Transform enemy = GetClosestEnemy();

        if (enemy == null)
            return AdvanceState();

        Vector3 dir = transform.position - enemy.position;
        dir.y = 0f;

        return Seek(transform.position + dir.normalized * 3f);
    }
    Vector3 RetreatState()
    {
        return Seek(safePoint.transform.position);
    }

    //Deciciones en base a la vida restante
    void DecideNextState()
    {
        float healthFactor = currentHealth / maxHealth;

        int allies = CountNearbyAllies();
        int enemies = CountNearbyEnemies();

        //Pesos
        float weightAttack = 0f;
        float weightDefensive = 0f;
        float weightRegroup = 0f;
        float weightRetreat = 0f;
        float weightAdvance = 0f;

        if (healthFactor > 0.6f && allies >= enemies)
            weightAttack = 3f;

        if (healthFactor > 0.4f)
            weightAdvance = 2f;

        if (allies < enemies)
            weightRegroup = 3f;

        if (healthFactor < 0.5f)
            weightDefensive = 2f;

        if (healthFactor < 0.3f)
            weightRetreat = 5f;

        //Roulette wheel
        float total = weightAttack + weightDefensive + weightRegroup + weightRetreat + weightAdvance;

        float rand = Random.Range(0f, total);

        if ((rand -= weightAttack) <= 0f)
            currentLeaderState = LeaderState.Attack;
        else if ((rand -= weightAdvance) <= 0f)
            currentLeaderState = LeaderState.Advance;
        else if ((rand -= weightRegroup) <= 0f)
            currentLeaderState = LeaderState.Regroup;
        else if ((rand -= weightDefensive) <= 0f)
            currentLeaderState = LeaderState.Defensive;
        else
            currentLeaderState = LeaderState.Retreat;
    }

    int CountNearbyAllies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, awarenessRadius, allyMask);

        int count = 0;

        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject) //No se cuenta a si mismo
            {
                count++;
            }
        }

        return count;
    }

    int CountNearbyEnemies()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, awarenessRadius, enemyMask);

        return hits.Length;
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

    private void OnGUI()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y - 15, 120, 20), currentLeaderState.ToString());
    }
}

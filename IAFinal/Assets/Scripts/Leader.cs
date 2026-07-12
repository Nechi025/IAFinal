using System.Collections.Generic;
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

    [Header("Obstacle Avoidance")]
    public float avoidDistance = 2f;
    public float avoidForce = 15f;

    public Vector3 velocity;
    private Rigidbody rb;
    private Animator anim;

    [Header("Target")]
    public Transform target;

    [Header("Combat")]
    public float maxHealth = 100f;
    public float currentHealth;

    public float fleeThreshold = 30f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;

    [Header("Pathfinding")]
    public float directMoveDistance = 4f;

    private List<Node> currentPath;
    private int pathIndex;

    private float pathTimer;
    public float pathRefreshRate = 1f;

    private Pathfinding pathfinding;

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
        anim = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        pathfinding = FindObjectOfType<Pathfinding>();
    }

    void Update()
    {
        decisionTimer -= Time.deltaTime;

        UpdateAnimations();

        if (decisionTimer <= 0f)
        {
            DecideNextState();
            decisionTimer = decisionInterval;
        }

        attackTimer -= Time.deltaTime;
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


    void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetBool("isWalking", false);
        anim.SetBool("isAttacking", false);
        anim.SetBool("isFleeing", false);

        switch (currentLeaderState)
        {
            case LeaderState.Advance:
                if (velocity.magnitude > 0.1f)
                    anim.SetBool("isWalking", true);
                break;

            case LeaderState.Attack:
                Transform enemy = GetVisibleEnemy();

                if (enemy != null)
                {
                    float distance = Vector3.Distance(transform.position, enemy.position);

                    if (distance <= attackRange)
                    {
                        Debug.Log(gameObject.name + " ATTACKING");
                        anim.SetBool("isAttacking", true);
                    }
                    else if (velocity.magnitude > 0.1f)
                    {
                        anim.SetBool("isWalking", true);
                    }
                }
                else if (velocity.magnitude > 0.1f)
                {
                    anim.SetBool("isWalking", true);
                }
                break;

            case LeaderState.Defensive:
            case LeaderState.Regroup:
                if (velocity.magnitude > 0.1f)
                    anim.SetBool("isWalking", true);
                break;

            case LeaderState.Retreat:
                anim.SetBool("isFleeing", true);
                break;
        }
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
        if (target == null)
            return Vector3.zero;

        return MoveTowards(target.position);
    }

    Vector3 AttackState()
    {
        Transform enemy = GetVisibleEnemy();

        if (enemy == null)
            return AdvanceState();

        float distance = Vector3.Distance(transform.position, enemy.position);

        if (distance <= attackRange)
        {
            Attack(enemy);
            return Vector3.zero;
        }

        return MoveTowards(enemy.position);
    }
    void Attack(Transform enemy)
    {
        //CD
        if (attackTimer > 0f)
            return;


        NPC enemyMC = enemy.GetComponent<NPC>();
        Leader enemyL = enemy.GetComponent<Leader>();

        if (enemyMC != null)
        {
            enemyMC.TakeDamage(attackDamage);
        }
        else
        {
            enemyL.TakeDamage(attackDamage);
        }
        attackTimer = attackCooldown;
    }


    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (anim != null)
        {
            anim.ResetTrigger("Damaged");
            anim.SetTrigger("Damaged");
        }

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
        return Vector3.zero;
    }
    Vector3 DefensiveState()
    {
        Transform enemy = GetClosestEnemy();

        if (enemy == null)
            return AdvanceState();

        Vector3 dir = transform.position - enemy.position;
        dir.y = 0f;

        return MoveTowards(transform.position + dir.normalized * 3f);
    }
    Vector3 RetreatState()
    {
        if (safePoint == null)
            return Vector3.zero;

        return MoveTowards(safePoint.position);
    }

    Vector3 FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
            return Vector3.zero;

        if (pathIndex >= currentPath.Count)
            return Vector3.zero;

        Vector3 waypoint = currentPath[pathIndex].worldPosition;

        if (Vector3.Distance(rb.position, waypoint) < 0.5f)
        {
            pathIndex++;
        }

        return Seek(waypoint);
    }

    void UpdatePath(Vector3 targetPos)
    {
        pathTimer -= Time.deltaTime;

        if (pathTimer > 0f)
            return;

        currentPath = pathfinding.FindPath(rb.position, targetPos);

        pathIndex = 0;

        pathTimer = pathRefreshRate;
    }

    bool NeedsPathfinding(Vector3 targetPos)
    {
        Vector3 dir = targetPos - rb.position;
        dir.y = 0f;

        //Si esta cerca, steering normal
        if (dir.magnitude < directMoveDistance)
            return false;

        //Raycast directo
        if (!Physics.Raycast(
            rb.position + Vector3.up * 0.5f,
            dir.normalized,
            dir.magnitude,
            obstacleMask))
        {
            return false;
        }

        return true;
    }

    //Funcion para verificar si hace falta o no usar A*
    Vector3 MoveTowards(Vector3 targetPosition)
    {
        if (NeedsPathfinding(targetPosition))
        {
            UpdatePath(targetPosition);
            return FollowPath();
        }



        return Seek(targetPosition);
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

        Gizmos.color = Color.green;

        for (int i = 0; i < currentPath.Count; i++)
        {
            Gizmos.DrawSphere(currentPath[i].worldPosition, 0.15f);

            if (i < currentPath.Count - 1)
            {
                Gizmos.DrawLine(
                    currentPath[i].worldPosition,
                    currentPath[i + 1].worldPosition
                );
            }
        }
    }

    private void OnGUI()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        GUI.Label(new Rect(screenPos.x, Screen.height - screenPos.y - 15, 120, 20), currentLeaderState.ToString());
    }
}

using UnityEngine;
using UnityEngine.AI;

public class TankMovement : MonoBehaviour
{
    public int m_PlayerNumber = 1;         
    public float m_Speed = 13.8f;            
    public float m_TurnSpeed = 180f;       
    public Joystick joystick;
    public float m_SpeedMultiplier = 1f;

    private Rigidbody m_Rigidbody;         
    private Vector3 m_Movement;
    private NavMeshAgent agent;
    private float baseAgentSpeed;
    private Transform playerTarget;
    
    private enum AIState { Patrol, Chase, Flee }
    private AIState currentState;
    private float stateTimer;
    private float speedBoostUntil;
    
    public float sightRange = 60f;
    public float patrolRadius = 35f;
    public float targetRefreshInterval = 1f;
    private float targetRefreshTimer;
    private TankHealth tankHealth; 
    
    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        if (m_PlayerNumber == 1 && m_Speed <= 12f)
            m_Speed = 12f * 1.15f;

        if (agent != null)
        {
            baseAgentSpeed = agent.speed * 1.15f;
            agent.speed = baseAgentSpeed;
        }

        tankHealth = GetComponent<TankHealth>(); 
    }

    private void OnEnable ()
    {
        // ĐÃ SỬA: Áp dụng cho TẤT CẢ các Bot (2, 3, 4...)
        if (m_PlayerNumber >= 2 && agent != null)
        {
            m_Rigidbody.isKinematic = true;
            agent.enabled = true;
            currentState = AIState.Patrol;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            
            if (agent.isOnNavMesh) agent.isStopped = false;
        }
        else if (m_PlayerNumber == 1) // Chỉ xe của người chơi mới dùng vật lý cơ bản
        {
            m_Rigidbody.isKinematic = false;
        }
    }

    private void Start()
    {
        // ĐÃ SỬA: TẤT CẢ các Bot đều phải đi tìm mục tiêu
        if (m_PlayerNumber >= 2)
        {
            FindPlayerTarget();
            targetRefreshTimer = targetRefreshInterval;
        }
        else // Người chơi
        {
            if (agent != null) agent.enabled = false;
            joystick = FindObjectOfType<FloatingJoystick>();
        }
    }

    private void FindPlayerTarget()
    {
        TankMovement[] tanks = FindObjectsOfType<TankMovement>();
        foreach (var tank in tanks)
        {
            if (tank.m_PlayerNumber == 1)
            {
                playerTarget = tank.transform;
                break;
            }
        }
    }

    private void Update()
    {
        if (m_SpeedMultiplier != 1f && Time.time >= speedBoostUntil)
        {
            m_SpeedMultiplier = 1f;
            if (agent != null)
                agent.speed = baseAgentSpeed;
        }

        if (m_PlayerNumber == 1)
        {
            float h = 0f;
            float v = 0f;

            if (joystick != null)
            {
                h = joystick.Horizontal;
                v = joystick.Vertical;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v = -1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

            m_Movement = GetCameraRelativeMovement(h, v);
        }
        // ĐÃ SỬA: TẤT CẢ Bot đều được chạy vòng lặp suy nghĩ AI
        else if (m_PlayerNumber >= 2 && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            targetRefreshTimer -= Time.deltaTime;
            if (targetRefreshTimer <= 0f)
            {
                FindPlayerTarget();
                targetRefreshTimer = targetRefreshInterval;
            }

            ProcessAIBrain();
        }
    }

    private Vector3 GetCameraRelativeMovement(float horizontal, float vertical)
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        Camera cam = Camera.main;
        if (cam != null)
        {
            forward = cam.transform.forward;
            forward.y = 0f;
            right = cam.transform.right;
            right.y = 0f;
        }
        forward.Normalize();
        right.Normalize();
        return forward * vertical + right * horizontal;
    }

    private void FixedUpdate()
    {
        if (m_PlayerNumber != 1) return;

        m_Rigidbody.angularVelocity = Vector3.zero;

        if (m_Movement.magnitude > 0.1f)
        {
            Turn();
            Move(m_Movement);
        }
        else
        {
            Move(Vector3.zero);
        }
    }

    private void Move(Vector3 moveDirection)
    {
        Vector3 targetVelocity = moveDirection.normalized * m_Speed * m_SpeedMultiplier;
        targetVelocity.y = m_Rigidbody.velocity.y; 
        m_Rigidbody.velocity = targetVelocity;
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        m_SpeedMultiplier = Mathf.Max(1f, multiplier);
        speedBoostUntil = Mathf.Max(speedBoostUntil, Time.time + Mathf.Max(0f, duration));
        if (agent != null)
            agent.speed = baseAgentSpeed * m_SpeedMultiplier;
    }

    private void Turn()
    {
        m_Rigidbody.MoveRotation(Quaternion.LookRotation(m_Movement));
    }

    private void ProcessAIBrain()
    {
        if (playerTarget == null) FindPlayerTarget();
        if (playerTarget == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        if (tankHealth != null && tankHealth.m_CurrentHealth <= 20f)
        {
            currentState = AIState.Flee;
        }
        else if (distanceToPlayer <= sightRange)
        {
            currentState = AIState.Chase;
        }
        else
        {
            currentState = AIState.Patrol;
        }

        switch (currentState)
        {
            case AIState.Patrol:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
                    randomDirection += transform.position;
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(randomDirection, out navHit, patrolRadius, NavMesh.AllAreas))
                    {
                        agent.SetDestination(navHit.position);
                    }
                    stateTimer = Random.Range(3f, 6f);
                }
                break;

            case AIState.Chase:
                agent.SetDestination(playerTarget.position);
                break;

            case AIState.Flee:
                Vector3 fleeDirection = transform.position - playerTarget.position;
                Vector3 fleePosition = transform.position + fleeDirection.normalized * 10f;
                NavMeshHit fleeHit;
                if (NavMesh.SamplePosition(fleePosition, out fleeHit, 10f, 1))
                {
                    agent.SetDestination(fleeHit.position);
                }
                break;
        }
    }
}
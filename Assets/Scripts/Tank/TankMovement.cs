using UnityEngine;
using UnityEngine.AI;

public class TankMovement : MonoBehaviour
{
    public int m_PlayerNumber = 1;         
    public float m_Speed = 12f;            
    public float m_TurnSpeed = 180f;       
    public Joystick joystick;

    private Rigidbody m_Rigidbody;         
    private Vector3 m_Movement;
    private NavMeshAgent agent;
    private Transform playerTarget;
    
    private enum AIState { Patrol, Chase, Flee }
    private AIState currentState;
    private float stateTimer;
    
    public float sightRange = 25f;
    private TankHealth tankHealth; 
    
    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        tankHealth = GetComponent<TankHealth>(); 
    }

    private void OnEnable ()
    {
        if (m_PlayerNumber == 2 && agent != null)
        {
            m_Rigidbody.isKinematic = true;
            agent.enabled = true;
            currentState = AIState.Patrol;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                agent.Warp(hit.position);
            
            if (agent.isOnNavMesh) agent.isStopped = false;
        }
        else
        {
            m_Rigidbody.isKinematic = false;
        }
    }

    private void Start()
    {
        if (m_PlayerNumber == 2)
        {
            FindPlayerTarget();
        }
        else
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
        if (m_PlayerNumber == 1)
        {
            float h = 0f;
            float v = 0f;

            // 1. Đọc dữ liệu từ Joystick (như bản gốc của bạn)
            if (joystick != null)
            {
                h = joystick.Horizontal;
                v = joystick.Vertical;
            }

            // 2. Nếu bấm phím, hệ thống sẽ ưu tiên dùng phím
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v = -1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h = 1f;

            // 3. Đưa tín hiệu vào hàm di chuyển gốc của bạn
            m_Movement = GetCameraRelativeMovement(h, v);
        }
        else if (m_PlayerNumber == 2 && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
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

        if (m_Movement.magnitude > 0.2f)
        {
            Turn();
            Move();
        }
    }

    private void Move()
    {
        Vector3 targetVelocity = m_Movement.normalized * m_Speed;
        targetVelocity.y = m_Rigidbody.velocity.y;
        m_Rigidbody.velocity = targetVelocity;
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
                    Vector3 randomDirection = Random.insideUnitSphere * 15f;
                    randomDirection += transform.position;
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(randomDirection, out navHit, 15f, 1))
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
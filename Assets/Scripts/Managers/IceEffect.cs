using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class IceEffect : MonoBehaviourPun
{
    private TankMovement movement;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private TankHealth health;
    private bool wasAgentEnabled = false;

    void Start()
    {
        movement = GetComponentInParent<TankMovement>();
        rb = GetComponentInParent<Rigidbody>();
        agent = GetComponentInParent<NavMeshAgent>();
        health = GetComponentInParent<TankHealth>();

        bool isBot = (movement != null && movement.IsBot());

        // Lưu trạng thái agent: nếu là Bot hoặc agent đang bật
        if (agent != null)
        {
            wasAgentEnabled = agent.enabled || isBot;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            agent.enabled = false;
        }

        // 1. Tắt điều khiển di chuyển
        if (movement != null) movement.enabled = false; 

        // 2. KHÓA CỨNG VẬT LÝ: Triệt tiêu mọi lực di chuyển và biến xe thành cục đá
        if (rb != null)
        {
            rb.velocity = Vector3.zero;        // Ép vận tốc về 0 (không trôi)
            rb.angularVelocity = Vector3.zero; // Ép tốc độ xoay về 0
            rb.isKinematic = true;             // Miễn nhiễm với mọi lực đẩy
        }

        // 3. Nhuốm màu xe thành Xanh Băng đồng bộ
        if (health != null)
        {
            health.SetEffectColor(Color.cyan);
        }

        Invoke("RemoveIce", 3f); 
    }

    public void RefreshIce()
    {
        CancelInvoke("RemoveIce");
        Invoke("RemoveIce", 3f);
    }

    void RemoveIce()
    {
        bool isBot = wasAgentEnabled || (movement != null && movement.IsBot());

        // 1. Phục hồi NavMeshAgent: Chỉ bật nếu là Bot
        if (agent != null)
        {
            agent.enabled = isBot;
            if (isBot)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                else if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }

                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                }
            }
        }

        // 2. Phục hồi Rigidbody vật lý:
        // Xe người chơi cần isKinematic = false để di chuyển bằng vật lý bình thường
        // Xe Bot dùng NavMesh thì cần isKinematic = true
        if (rb != null)
        {
            rb.isKinematic = isBot;
        }

        // 3. Mở lại điều khiển di chuyển
        if (movement != null)
        {
            bool shouldEnable = movement.CanControl() || isBot;
            movement.enabled = shouldEnable;
            if (shouldEnable)
            {
                movement.ResumeFromFreeze();
            }
        }

        // 4. Trả lại màu sơn gốc (hoặc chuyển sang Toxic nếu vẫn đang dính độc)
        enabled = false;
        if (health != null)
        {
            health.RestoreOriginalColor();
        }

        Destroy(this); 
    }

    private void OnDestroy()
    {
        CancelInvoke("RemoveIce");
        if (enabled)
        {
            bool isBot = wasAgentEnabled || (movement != null && movement.IsBot());
            if (agent != null)
            {
                agent.enabled = isBot;
                if (isBot && agent.isOnNavMesh) agent.isStopped = false;
            }
            if (rb != null) rb.isKinematic = isBot;
            if (movement != null)
            {
                bool shouldEnable = movement.CanControl() || isBot;
                movement.enabled = shouldEnable;
                if (shouldEnable) movement.ResumeFromFreeze();
            }
        }
        enabled = false;
        if (health != null)
        {
            health.RestoreOriginalColor();
        }
    }
}
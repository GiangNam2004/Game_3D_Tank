using UnityEngine;
using UnityEngine.AI; // Thêm dòng này để xử lý cả Bot AI

public class IceEffect : MonoBehaviour
{
    private TankMovement movement;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private MeshRenderer[] renderers;
    private Color[] originalColors;

    void Start()
    {
        // Dùng GetComponentInParent để đề phòng trường hợp đạn trúng vào bánh xích hay nòng súng
        movement = GetComponentInParent<TankMovement>();
        rb = GetComponentInParent<Rigidbody>();
        agent = GetComponentInParent<NavMeshAgent>();

        // 1. Tắt điều khiển của Người chơi
        if (movement != null) movement.enabled = false; 

        // 2. Tắt AI tìm đường của Bot (Nếu có)
        if (agent != null) agent.enabled = false;

        // 3. KHÓA CỨNG VẬT LÝ: Triệt tiêu mọi lực di chuyển và biến xe thành cục đá
        if (rb != null)
        {
            rb.velocity = Vector3.zero;        // Ép vận tốc về 0 (không trôi)
            rb.angularVelocity = Vector3.zero; // Ép tốc độ xoay về 0
            rb.isKinematic = true;             // Miễn nhiễm với mọi lực đẩy
        }

        // 4. Nhuốm màu xe thành Xanh Băng
        renderers = GetComponentsInChildren<MeshRenderer>();
        originalColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
            renderers[i].material.color = Color.cyan;
        }

        Invoke("RemoveIce", 3f); 
    }

    void RemoveIce()
    {
        // Mở khóa lại toàn bộ các chức năng
        if (movement != null) movement.enabled = true;
        if (agent != null) agent.enabled = true;
        if (rb != null) rb.isKinematic = false; 

        // Trả lại màu sơn gốc
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].material.color = originalColors[i];
        }

        Destroy(this); 
    }
}
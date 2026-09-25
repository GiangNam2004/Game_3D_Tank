using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    public float m_Speed = 20f;        // Tốc độ bay của tên lửa
    public float m_TurnSpeed = 5f;     // Tốc độ bẻ lái (càng cao cua càng gắt)
    public int m_ShooterPlayerNumber = 1; // Đánh dấu phe bắn để không tự đuổi mình

    private Transform m_Target;
    private Rigidbody m_Rigidbody;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        
        // Tắt trọng lực để đạn bay thẳng như tên lửa thay vì rơi vòng cung
        if (m_Rigidbody != null) 
            m_Rigidbody.useGravity = false;

        FindClosestEnemy();
    }

    private void FindClosestEnemy()
    {
        // Quét toàn bộ xe tăng trên bản đồ
        TankMovement[] allTanks = FindObjectsOfType<TankMovement>();
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < allTanks.Length; i++)
        {
            // Bỏ qua xe của phe mình và xe đã chết
            if (allTanks[i].m_PlayerNumber == m_ShooterPlayerNumber || !allTanks[i].gameObject.activeSelf)
                continue;

            // Tìm xe gần nhất
            float distance = Vector3.Distance(transform.position, allTanks[i].transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                m_Target = allTanks[i].transform;
            }
        }
    }

    private void FixedUpdate()
    {
        // Nếu có mục tiêu và mục tiêu còn sống thì bẻ lái đuổi theo
        if (m_Target != null && m_Target.gameObject.activeSelf)
        {
            // Tính toán hướng bay về phía mục tiêu (ngắm vào thân xe thay vì dưới chân)
            Vector3 targetPos = m_Target.position;
            targetPos.y += 1f; // Nâng tâm ngắm lên giữa thân xe tăng
            Vector3 direction = (targetPos - transform.position).normalized;
            
            // Xoay đầu đạn từ từ hướng về mục tiêu
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            m_Rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, m_TurnSpeed * Time.fixedDeltaTime));
            
            // Ép viên đạn bay thẳng theo hướng đầu nòng hiện tại
            m_Rigidbody.velocity = transform.forward * m_Speed;
        }
        else
        {
            // Nếu mất dấu mục tiêu (xe địch đã nổ), cứ bay thẳng tiếp cho đến khi chạm tường
            if (m_Rigidbody != null)
                m_Rigidbody.velocity = transform.forward * m_Speed;
        }
    }
}
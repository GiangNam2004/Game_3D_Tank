using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    public float m_Speed = 20f;        
    public float m_TurnSpeed = 5f;     
    public int m_ShooterPlayerNumber = 1; // Đánh dấu phe bắn

    private Transform m_Target;
    private Rigidbody m_Rigidbody;

    private void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        
        if (m_Rigidbody != null) 
            m_Rigidbody.useGravity = false;

        FindClosestEnemy();
    }

    private void FindClosestEnemy()
    {
        TankMovement[] allTanks = FindObjectsOfType<TankMovement>();
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < allTanks.Length; i++)
        {
            // 1. Bỏ qua nếu xe đã chết
            if (!allTanks[i].gameObject.activeSelf)
                continue;

            // 2. NHẬN DIỆN ĐỊCH/TA (Hỗ trợ cả Online và Offline)
            bool isMyTank = false;
            if (Photon.Pun.PhotonNetwork.IsConnected)
            {
                Photon.Pun.PhotonView targetView = allTanks[i].GetComponent<Photon.Pun.PhotonView>();
                if (targetView != null && targetView.IsMine) 
                    isMyTank = true;
            }
            else
            {
                // Chơi Offline: So sánh số hiệu phe (Player 1 hoặc Bot 2)
                if (allTanks[i].m_PlayerNumber == m_ShooterPlayerNumber) 
                    isMyTank = true;
            }

            if (isMyTank) continue;

            // 3. TÍNH KHOẢNG CÁCH VÀ CHỐT CHẶN AN TOÀN
            float distance = Vector3.Distance(transform.position, allTanks[i].transform.position);
            
            // Bỏ qua xe ở cự ly < 3m (ngay sát nòng súng) để tên lửa không quay đầu tự sát
            if (distance < 3f) continue;

            // 4. Khóa mục tiêu gần nhất
            if (distance < closestDistance)
            {
                closestDistance = distance;
                m_Target = allTanks[i].transform;
            }
        }
    }

    private void FixedUpdate()
    {
        if (m_Target != null && m_Target.gameObject.activeSelf)
        {
            Vector3 targetPos = m_Target.position;
            targetPos.y += 1f; // Nâng tâm ngắm lên giữa thân xe tăng
            Vector3 direction = (targetPos - transform.position).normalized;
            
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            m_Rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, m_TurnSpeed * Time.fixedDeltaTime));
            
            m_Rigidbody.velocity = transform.forward * m_Speed;
        }
        else
        {
            if (m_Rigidbody != null)
                m_Rigidbody.velocity = transform.forward * m_Speed;
        }
    }
}
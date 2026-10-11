using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraControl : MonoBehaviour
{
    public float m_DampTime = 0.2f;                 
    public float m_MinSize = 30f;                   
    [HideInInspector] public Transform[] m_Targets; 

    private Camera m_Camera;                        
    private Vector3 m_MoveVelocity;                 

    private void Awake()
    {
        m_Camera = GetComponentInChildren<Camera>();
        if (SceneManager.GetActiveScene().name == "Main 2")
        {
            MinimapController legacyMinimap = GetComponent<MinimapController>();
            if (legacyMinimap != null)
                Destroy(legacyMinimap);

            if (GetComponent<Main2MinimapController>() == null)
                gameObject.AddComponent<Main2MinimapController>();
        }
        else if (GetComponent<MinimapController>() == null)
        {
            gameObject.AddComponent<MinimapController>();
        }
    }

    private void FixedUpdate()
    {
        // 1. Kiểm tra xem GameManager đã giao mục tiêu cho Camera chưa
        if (m_Targets != null && m_Targets.Length > 0 && m_Targets[0] != null)
        {
            // 2. ÉP CAMERA CHỈ ĐUỔI THEO MỤC TIÊU SỐ 0 (CHÍNH LÀ XE CỦA BẠN)
            transform.position = Vector3.SmoothDamp(transform.position, m_Targets[0].position, ref m_MoveVelocity, m_DampTime);
            
            // 3. KHÓA CỨNG ĐỘ ZOOM, không cho tự động co giãn nữa
            m_Camera.orthographicSize = m_MinSize;
        }
    }

    public void SetTarget(Transform target)
    {
        m_Targets = target == null ? null : new[] { target };
        m_MoveVelocity = Vector3.zero;
        SetStartPositionAndSize();
    }

    // Giữ lại hàm này để lúc vừa load màn, Camera nhảy ngay đến vị trí xe bạn
    public void SetStartPositionAndSize()
    {
        if (m_Targets != null && m_Targets.Length > 0 && m_Targets[0] != null)
        {
            transform.position = m_Targets[0].position;
            if (m_Camera != null) m_Camera.orthographicSize = m_MinSize;
        }
    }
}
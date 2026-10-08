using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement; 

public class NetworkPlayerSetup : MonoBehaviourPun 
{
    private Transform mainCam;

    void Start()
    {
        if (photonView.IsMine)
        {
            // NẾU ĐANG Ở LOBBY: Lấy Main Camera để chuẩn bị đi theo sau lưng
            if (SceneManager.GetActiveScene().name == "Lobby")
            {
                if (Camera.main != null)
                {
                    mainCam = Camera.main.transform;
                    mainCam.rotation = Quaternion.Euler(45f, 0f, 0f);
                }
            }
        }
        else
        {
            // Tìm và khóa bánh xe đối thủ (Chỉ khóa nếu tìm thấy script)
            TankMovement moveScript = GetComponent<TankMovement>();
            if (moveScript != null) 
            {
                moveScript.enabled = false;
            }

            // Tìm và khóa súng đối thủ (Tránh lỗi văng game trên Phân thân)
            TankShooting shootScript = GetComponent<TankShooting>();
            if (shootScript != null) 
            {
                shootScript.enabled = false; 
            }
        }
    }

    void LateUpdate()
    {
        // NẾU ĐANG Ở LOBBY: Bắt Camera chạy theo sau lưng xe tăng
        if (photonView.IsMine && mainCam != null && SceneManager.GetActiveScene().name == "Lobby")
        {
            mainCam.position = transform.position + new Vector3(0f, 10f, -10f);
        }
    }
}
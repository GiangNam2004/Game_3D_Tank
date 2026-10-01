using UnityEngine;
using Photon.Pun;

public class GameManagerOnline : MonoBehaviour
{
    void Start()
    {
        // Kiểm tra xem máy chủ đã sẵn sàng và 2 người đã vào chung phòng chưa
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // Chọn ngẫu nhiên vị trí thả xe để 2 người không bị rơi đè lên nhau
            float randomX = Random.Range(-15f, 15f);
            float randomZ = Random.Range(-15f, 15f);
            Vector3 spawnPos = new Vector3(randomX, 0f, randomZ);

            // Lệnh thả xe tăng từ thư mục Resources xuống mặt đất (trục Y = 0)
            PhotonNetwork.Instantiate("Tank", spawnPos, Quaternion.identity);
        }
    }
}
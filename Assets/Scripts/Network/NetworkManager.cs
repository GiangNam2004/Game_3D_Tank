using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Chú ý: Kế thừa MonoBehaviourPunCallbacks thay vì MonoBehaviour bình thường
public class NetworkManager : MonoBehaviourPunCallbacks
{
   void Start()
    {
        Debug.Log("1. Đang kết nối đến máy chủ Photon...");
        
        // Thêm dòng này vào (BẮT BUỘC):
        PhotonNetwork.AutomaticallySyncScene = true; 
        
        PhotonNetwork.ConnectUsingSettings(); 
    }

    // Hàm này tự động chạy khi đã kết nối máy chủ thành công
    public override void OnConnectedToMaster()
    {
        Debug.Log("2. Đã kết nối máy chủ! Đang vào sảnh...");
        PhotonNetwork.JoinLobby();
    }

    // Hàm này tự động chạy khi vào sảnh thành công
    public override void OnJoinedLobby()
    {
        Debug.Log("3. Đã vào sảnh! Đang tìm phòng chung...");
        // Tự động tạo hoặc chui vào một phòng chung tên là "LobbyChung"
        PhotonNetwork.JoinOrCreateRoom("LobbyChung", new RoomOptions { MaxPlayers = 20 }, TypedLobby.Default);
    }

    // Hàm này tự động chạy khi bạn đã ngồi yên vị trong phòng
    public override void OnJoinedRoom()
    {
        Debug.Log("4. ĐÃ VÀO PHÒNG LOBBY CHUNG! Hiện có " + PhotonNetwork.CurrentRoom.PlayerCount + " người ở đây.");
        
        // Chọn ngẫu nhiên tọa độ X và Z trong khoảng từ -5 đến 5
        float randomX = Random.Range(-5f, 5f);
        float randomZ = Random.Range(-5f, 5f);

        // Gán tọa độ ngẫu nhiên này cho xe tăng (Trục Y vẫn bằng 0 để bám đất)
        Vector3 spawnPosition = new Vector3(randomX, 0f, randomZ); 
        PhotonNetwork.Instantiate("Tank", spawnPosition, Quaternion.identity);
    }
}
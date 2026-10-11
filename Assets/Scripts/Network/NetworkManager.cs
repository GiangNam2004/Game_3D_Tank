using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Chú ý: Kế thừa MonoBehaviourPunCallbacks thay vì MonoBehaviour bình thường
public class NetworkManager : MonoBehaviourPunCallbacks
{
   private bool m_HasSpawnedTank;

   void Start()
   {
       // Thêm dòng này vào (BẮT BUỘC):
       PhotonNetwork.AutomaticallySyncScene = true;

       if (PhotonNetwork.IsConnected)
       {
           Debug.Log("Photon đã kết nối, tiếp tục luồng vào phòng.");
           ContinueConnectionFlow();
           return;
       }

       Debug.Log("1. Đang kết nối đến máy chủ Photon...");
        
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
        JoinSharedRoom();
    }

    // Hàm này tự động chạy khi bạn đã ngồi yên vị trong phòng
    public override void OnJoinedRoom()
    {
        SpawnNetworkTank();
    }

    private void ContinueConnectionFlow()
    {
        if (PhotonNetwork.InRoom)
        {
            SpawnNetworkTank();
        }
        else if (PhotonNetwork.NetworkClientState == ClientState.JoinedLobby)
        {
            JoinSharedRoom();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    private void JoinSharedRoom()
    {
        // Tự động tạo hoặc chui vào một phòng chung tên là "LobbyChung"
        PhotonNetwork.JoinOrCreateRoom("LobbyChung", new RoomOptions { MaxPlayers = 20 }, TypedLobby.Default);
    }

    private void SpawnNetworkTank()
    {
        if (m_HasSpawnedTank || !PhotonNetwork.InRoom)
            return;

        m_HasSpawnedTank = true;
        Debug.Log("4. ĐÃ VÀO PHÒNG LOBBY CHUNG! Hiện có " + PhotonNetwork.CurrentRoom.PlayerCount + " người ở đây.");
        
        Vector3 spawnPosition = GetLobbySpawnPosition();
        PhotonNetwork.Instantiate("Tank", spawnPosition, Quaternion.identity);
    }

    private Vector3 GetLobbySpawnPosition()
    {
        GameObject[] existingTanks = GameObject.FindGameObjectsWithTag("Player");
        const float minimumDistance = 8f;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 candidate = new Vector3(Random.Range(-15f, 15f), 0f, Random.Range(-15f, 15f));
            bool isTooClose = false;

            for (int i = 0; i < existingTanks.Length; i++)
            {
                if (Vector3.Distance(candidate, existingTanks[i].transform.position) < minimumDistance)
                {
                    isTooClose = true;
                    break;
                }
            }

            if (!isTooClose)
                return candidate;
        }

        return new Vector3(Random.Range(-15f, 15f), 0f, Random.Range(-15f, 15f));
    }
}
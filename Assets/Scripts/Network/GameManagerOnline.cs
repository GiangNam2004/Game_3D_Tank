using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManagerOnline : MonoBehaviourPunCallbacks
{
    private const float MinimumPlayerSpawnDistance = 15f;
    [SerializeField] private Vector2 spawnArea = new Vector2(28f, 28f);
    [SerializeField] private float groundHeight = 0.5f;
    [SerializeField] private float navMeshSampleDistance = 2f;
    [SerializeField] private int maxSpawnPositionAttempts = 20;
    [SerializeField] private float resultDisplayDuration = 3f;

    private Bounds mapBounds;
    private bool hasMapBounds;
    private bool matchEnded;
    private bool returningToLobby;
    private string resultMessage;

    void Start()
    {
        // TỐI ƯU MẠNG: Tăng gấp đôi tần suất gửi/nhận gói tin để game không bị giật
        PhotonNetwork.SendRate = 40;
        PhotonNetwork.SerializationRate = 40;

        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            hasMapBounds = TryGetMapBounds(out mapBounds);
            Vector3 spawnPosition = GetRandomSpawnPosition();

            // Đọc tên Tank người chơi đã chọn từ CustomProperties hoặc PlayerPrefs
            string selectedTankPrefab = PlayerPrefs.GetString("SelectedTank", "Tank");
            if (PhotonNetwork.LocalPlayer != null && 
                PhotonNetwork.LocalPlayer.CustomProperties != null && 
                PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("SelectedTank"))
            {
                selectedTankPrefab = (string)PhotonNetwork.LocalPlayer.CustomProperties["SelectedTank"];
            }

            if (Resources.Load<GameObject>(selectedTankPrefab) == null)
            {
                Debug.LogWarning("Không tìm thấy Prefab '" + selectedTankPrefab + "' trong Resources, fallback về 'Tank'");
                selectedTankPrefab = "Tank";
            }
            
            // Khởi tạo đúng chiếc Tank đó qua mạng
            GameObject playerTank = PhotonNetwork.Instantiate(selectedTankPrefab, spawnPosition, Quaternion.identity);
            PhotonView playerView = playerTank.GetComponent<PhotonView>();
            CameraControl cameraControl = FindObjectOfType<CameraControl>();
            if (playerView != null && playerView.IsMine && cameraControl != null)
                cameraControl.SetTarget(playerTank.transform);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        for (int attempt = 0; attempt < maxSpawnPositionAttempts; attempt++)
        {
            Vector3 position = GetSpawnPosition();
            TankHealth[] tanks = FindObjectsOfType<TankHealth>();
            bool isFarEnough = true;

            for (int i = 0; i < tanks.Length; i++)
            {
                if (Vector3.Distance(position, tanks[i].transform.position) < MinimumPlayerSpawnDistance)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
                return position;
        }

        return GetSpawnPosition();
    }

    private Vector3 GetSpawnPosition()
    {
        if (hasMapBounds)
        {
            return new Vector3(
                Random.Range(mapBounds.min.x, mapBounds.max.x),
                mapBounds.max.y,
                Random.Range(mapBounds.min.z, mapBounds.max.z));
        }

        if (NavMesh.SamplePosition(
            new Vector3(
                Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
                groundHeight,
                Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f)),
            out NavMeshHit hit,
            navMeshSampleDistance,
            NavMesh.AllAreas))
        {
            return hit.position;
        }

        return new Vector3(
            Random.Range(-spawnArea.x * 0.5f, spawnArea.x * 0.5f),
            groundHeight,
            Random.Range(-spawnArea.y * 0.5f, spawnArea.y * 0.5f));
    }

    private bool TryGetMapBounds(out Bounds bounds)
    {
        Collider[] colliders = FindObjectsOfType<Collider>();
        float largestArea = 0f;
        bounds = new Bounds();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
                continue;

            Bounds candidateBounds = collider.bounds;
            float area = candidateBounds.size.x * candidateBounds.size.z;
            if (area > largestArea)
            {
                largestArea = area;
                bounds = candidateBounds;
            }
        }

        return largestArea > 0f;
    }

    public void ShowMatchResult(int winnerActorNumber, int loserActorNumber)
    {
        if (matchEnded)
            return;

        matchEnded = true;
        int localActorNumber = PhotonNetwork.LocalPlayer != null
            ? PhotonNetwork.LocalPlayer.ActorNumber
            : 0;
        resultMessage = localActorNumber == winnerActorNumber
            ? "YOU WIN!"
            : localActorNumber == loserActorNumber
                ? "YOU LOSE!"
                : "PLAYER " + winnerActorNumber + " WIN!";

        TankMovement[] movements = FindObjectsOfType<TankMovement>();
        for (int i = 0; i < movements.Length; i++)
            movements[i].enabled = false;

        TankShooting[] shootings = FindObjectsOfType<TankShooting>();
        for (int i = 0; i < shootings.Length; i++)
            shootings[i].enabled = false;

        StartCoroutine(ReturnToLobby());
    }

    private IEnumerator ReturnToLobby()
    {
        yield return new WaitForSeconds(resultDisplayDuration);

        if (PhotonNetwork.InRoom)
        {
            returningToLobby = true;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            LoadLobby();
        }
    }

    public override void OnLeftRoom()
    {
        if (returningToLobby)
            LoadLobby();
    }

    private void LoadLobby()
    {
        SceneManager.LoadScene("Lobby");
    }

    private void OnGUI()
    {
        if (!matchEnded)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Max(24, Screen.height / 12),
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(0f, Screen.height * 0.35f, Screen.width, Screen.height * 0.2f), resultMessage, style);
    }
}
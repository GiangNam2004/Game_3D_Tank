using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;

public class TankSelection : MonoBehaviour
{
    public Button btnTank1;
    public Button btnTank; // Xe gốc
    public Button btnTank2;

    private void Start()
    {
        SelectTank("Tank"); // Mặc định

        // Đổi "Tank 1" thành "Tank1", "Tank 2" thành "Tank2"
        if (btnTank1 != null) btnTank1.onClick.AddListener(() => SelectTank("Tank1"));
        if (btnTank != null) btnTank.onClick.AddListener(() => SelectTank("Tank"));
        if (btnTank2 != null) btnTank2.onClick.AddListener(() => SelectTank("Tank2"));
    }

    public void SelectTank(string tankPrefabName)
    {
        // 1. LƯU CHO OFFLINE (Lưu thẳng vào bộ nhớ máy)
        PlayerPrefs.SetString("SelectedTank", tankPrefabName);
        PlayerPrefs.Save();

        // 2. LƯU CHO ONLINE (Lưu vào hồ sơ Photon nếu đang có mạng)
        if (PhotonNetwork.IsConnected)
        {
            Hashtable customProps = new Hashtable();
            customProps["SelectedTank"] = tankPrefabName;
            PhotonNetwork.LocalPlayer.SetCustomProperties(customProps);
        }

        Debug.Log("Đã lưu xe: " + tankPrefabName + " cho cả Offline và Online!");
    }
}
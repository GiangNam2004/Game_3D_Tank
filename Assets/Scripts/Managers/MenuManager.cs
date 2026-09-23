using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // Nhấn nút sẽ nhảy sang sảnh chờ
        SceneManager.LoadScene("Lobby"); 
    }
}
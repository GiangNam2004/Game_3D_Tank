using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCInteract : MonoBehaviour
{
    public GameObject dialoguePanel; 

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Báo ra Console xem có nhận diện được xe chạm vào không
        Debug.Log("Có vật thể chạm vào vùng NPC: " + other.gameObject.name);

        TankMovement tank = other.GetComponentInParent<TankMovement>();
        if (tank != null && tank.m_PlayerNumber == 1)
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true); // Bật khung chat
            }
            else
            {
                // Báo lỗi đỏ nếu quên kéo giao diện vào
                Debug.LogError("LỖI: Bạn chưa kéo DialoguePanel vào ô của NPC!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TankMovement tank = other.GetComponentInParent<TankMovement>();
        if (tank != null && dialoguePanel != null)
        {
            dialoguePanel.SetActive(false); // Tắt khung chat khi đi xa
        }
    }

    public void GoToBattle()
    {
        SceneManager.LoadScene("Main");
    }
}
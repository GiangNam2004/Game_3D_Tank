using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using TMPro; 

public class BossTrigger : MonoBehaviour
{
    [Header("1. Khung UI chung (Kéo UI từ Hierarchy vào)")]
    public GameObject dialoguePanel;
    public Image avatarUI;                 
    public TextMeshProUGUI dialogueTextUI; 
    public TextMeshProUGUI buttonTextUI;   
    public Button nextButton; // MỚI: Kéo bản thân cái Nút bấm vào đây

    [Header("2. Cốt truyện của NPC (Điền nhiều trang)")]
    public Sprite myAvatar;                
    
    [TextArea(2, 5)]
    public string[] myStory;               
    
    public string mySceneToLoad = "Main";  

    private int currentLine = 0; 

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        TankMovement tank = other.GetComponentInParent<TankMovement>();
        if (tank != null && tank.m_PlayerNumber == 1) 
        {
            if (dialoguePanel != null)
            {
                // --- BÍ QUYẾT LÀ ĐÂY: TỰ ĐỘNG GÁN LỆNH CHO NÚT BẤM ---
                if (nextButton != null)
                {
                    nextButton.onClick.RemoveAllListeners(); // Xóa sạch lệnh của xe khác
                    nextButton.onClick.AddListener(OnNextButtonClicked); // Nạp lệnh của xe này vào
                }

                currentLine = 0; 
                UpdateDialogueUI(); 
                dialoguePanel.SetActive(true); 
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TankMovement tank = other.GetComponentInParent<TankMovement>();
        if (tank != null && dialoguePanel != null)
        {
            dialoguePanel.SetActive(false); 
        }
    }

    public void OnNextButtonClicked()
    {
        currentLine++; 

        if (currentLine < myStory.Length)
        {
            UpdateDialogueUI();
        }
        else
        {
            SceneManager.LoadScene(mySceneToLoad);
        }
    }

    private void UpdateDialogueUI()
    {
        if (avatarUI != null && myAvatar != null)
            avatarUI.sprite = myAvatar;

        if (dialogueTextUI != null && myStory.Length > 0)
            dialogueTextUI.text = myStory[currentLine];

        if (buttonTextUI != null)
        {
            if (currentLine == myStory.Length - 1)
                buttonTextUI.text = "Vào Ải"; 
            else
                buttonTextUI.text = "Tiếp tục (>>)"; 
        }
    }
}
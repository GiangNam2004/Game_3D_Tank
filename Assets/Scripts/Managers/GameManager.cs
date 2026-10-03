using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Thêm 3 biến này vào đầu class GameManager
public SkillButtonUI m_SpreadSkillUI;
public SkillButtonUI m_MineSkillUI;
public SkillButtonUI m_HomingSkillUI;
    public int m_MaxWavesPerLevel = 3;
    public float m_StartDelay = 3f;
    public float m_EndDelay = 3f;
    public CameraControl m_CameraControl;
    public Text m_MessageText;
    public GameObject m_TankPrefab;
    public TankManager[] m_Tanks;

    private int m_RoundNumber;
    private WaitForSeconds m_StartWait;
    private WaitForSeconds m_EndWait;
    private bool m_PlayerDefeated;

    private void Start()
    {
        m_StartWait = new WaitForSeconds (m_StartDelay);
        m_EndWait = new WaitForSeconds (m_EndDelay);
        m_RoundNumber = 0;

        SpawnAllTanks();
        SetCameraTargets();
        StartCoroutine (GameLoop ());
    }

   private void SpawnAllTanks()
    {
        // 1. Đọc tên xe người chơi đã chọn
        string selectedTankName = PlayerPrefs.GetString("SelectedTank", "Tank");
        GameObject myTankPrefab = Resources.Load<GameObject>(selectedTankName);

        // 2. Chọn xe cho Bot sao cho KHÁC với xe của người chơi
        string botTankName = (selectedTankName == "Tank2") ? "Tank1" : "Tank2";
        GameObject botTankPrefab = Resources.Load<GameObject>(botTankName);

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            GameObject prefabToSpawn;

            // ĐÃ SỬA LỖI Ở ĐÂY: Dùng i == 0 để nhận diện Người chơi 1 thay vì m_PlayerNumber
            if (i == 0)
            {
                prefabToSpawn = myTankPrefab != null ? myTankPrefab : m_TankPrefab;
                
                if (myTankPrefab == null) 
                    Debug.LogError("LỖI: Không tìm thấy file '" + selectedTankName + "' trong thư mục Resources!");
            }
            else // i >= 1 LÀ BOT
            {
                prefabToSpawn = botTankPrefab != null ? botTankPrefab : m_TankPrefab;
            }

            // Sinh xe ra bản đồ
            m_Tanks[i].m_Instance = Instantiate(prefabToSpawn, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
            
            // Gán số hiệu cho xe (1 là Player, 2 là Bot)
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].Setup();
            // --- THÊM ĐOẠN NÀY ĐỂ ẨN/HIỆN NÚT OFFLINE ---
            if (i == 0) // Chỉ xét UI cho xe của người chơi
            {
                TankShooting pShoot = m_Tanks[i].m_Instance.GetComponent<TankShooting>();
                if (m_SpreadSkillUI != null) m_SpreadSkillUI.gameObject.SetActive(pShoot.m_HasSpreadShot);
                if (m_MineSkillUI != null) m_MineSkillUI.gameObject.SetActive(pShoot.m_HasMine);
                if (m_HomingSkillUI != null) m_HomingSkillUI.gameObject.SetActive(pShoot.m_HasHomingMissile);
            }
        }
    }

    private void SetCameraTargets()
    {
        Transform[] targets = new Transform[m_Tanks.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = m_Tanks[i].m_Instance.transform;
        }
        m_CameraControl.m_Targets = targets;
    }

    private IEnumerator GameLoop ()
    {
        yield return StartCoroutine (RoundStarting ());
        yield return StartCoroutine (RoundPlaying());
        yield return StartCoroutine (RoundEnding());

        if (m_PlayerDefeated)
        {
            SceneManager.LoadScene("Lobby");
        }
        else
        {
            if (m_RoundNumber >= m_MaxWavesPerLevel)
            {
                SceneManager.LoadScene("Lobby");
            }
            else
            {
                StartCoroutine (GameLoop ());
            }
        }
    }

    private IEnumerator RoundStarting ()
    {
        m_RoundNumber++;
        ResetTanksForWave();
        DisableTankControl ();

        m_CameraControl.SetStartPositionAndSize ();
        m_MessageText.text = "WAVE " + m_RoundNumber;

        yield return m_StartWait;
    }

    private IEnumerator RoundPlaying ()
    {
        EnableTankControl ();
        m_MessageText.text = string.Empty;

        while (!IsRoundOver())
        {
            yield return null;
        }
    }

    private IEnumerator RoundEnding ()
    {
        DisableTankControl ();

        if (m_PlayerDefeated)
            m_MessageText.text = "GAME OVER!";
        else if (m_RoundNumber >= m_MaxWavesPerLevel)
            m_MessageText.text = "LEVEL CLEARED!";
        else
            m_MessageText.text = "WAVE CLEARED!";

        yield return m_EndWait;
    }

    private bool IsRoundOver()
    {
        if (!m_Tanks[0].m_Instance.activeSelf)
        {
            m_PlayerDefeated = true;
            return true; 
        }

        int botsToSpawn = Mathf.Min(m_RoundNumber, m_Tanks.Length - 1);
        for (int i = 1; i <= botsToSpawn; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
            {
                return false;
            }
        }

        m_PlayerDefeated = false;
        return true;
    }

    private void ResetTanksForWave()
    {
        m_Tanks[0].Reset();
        m_Tanks[0].m_Instance.SetActive(true);

        int botsToSpawn = Mathf.Min(m_RoundNumber, m_Tanks.Length - 1);

        for (int i = 1; i < m_Tanks.Length; i++)
        {
            if (i <= botsToSpawn)
            {
                m_Tanks[i].Reset();
                m_Tanks[i].m_Instance.SetActive(true);
            }
            else
            {
                m_Tanks[i].m_Instance.SetActive(false);
            }
        }
    }

    private void EnableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                m_Tanks[i].EnableControl();
        }
    }

    private void DisableTankControl()
    {
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            if (m_Tanks[i].m_Instance.activeSelf)
                m_Tanks[i].DisableControl();
        }
    }

    // ==========================================
    // CẦU NỐI UI: GỌI KỸ NĂNG ĐẠN CHÙM CHO NGƯỜI CHƠI
    // ==========================================
    public void OnClick_SpreadShot()
    {
        if (m_Tanks != null && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && m_Tanks[0].m_Instance.activeSelf)
        {
            TankShooting shootingScript = m_Tanks[0].m_Instance.GetComponent<TankShooting>();
            if (shootingScript != null)
            {
                shootingScript.FireSpreadShot();
                // Tự động lấy biến m_SkillCooldown (5s) từ TankShooting
                m_SpreadSkillUI.StartCooldown(shootingScript.m_SkillCooldown);
            }
        }
    }

    public void OnClick_DropMine()
    {
        if (m_Tanks != null && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && m_Tanks[0].m_Instance.activeSelf)
        {
            TankShooting shootingScript = m_Tanks[0].m_Instance.GetComponent<TankShooting>();
            if (shootingScript != null)
            {
                shootingScript.DropMine();
                // Tự động lấy biến m_MineCooldown (8s) từ TankShooting
                m_MineSkillUI.StartCooldown(shootingScript.m_MineCooldown);
            }
        }
    }

    public void OnClick_HomingMissile()
    {
        if (m_Tanks != null && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && m_Tanks[0].m_Instance.activeSelf)
        {
            TankShooting shootingScript = m_Tanks[0].m_Instance.GetComponent<TankShooting>();
            if (shootingScript != null)
            {
                shootingScript.FireHomingMissile();
                // Tự động lấy biến m_HomingCooldown (10s) từ TankShooting
                m_HomingSkillUI.StartCooldown(shootingScript.m_HomingCooldown);
            }
        }    
    }
}
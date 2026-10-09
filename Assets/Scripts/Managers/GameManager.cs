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

        // 2. Tải Prefab "Enemy" cho lính thường
        GameObject botTankPrefab = Resources.Load<GameObject>("Enemy");

        for (int i = 0; i < m_Tanks.Length; i++)
        {
            GameObject finalPrefabToSpawn = null; 

            // --- BƯỚC XÁC ĐỊNH LOẠI XE ---
            if (i == 0) 
            {
                finalPrefabToSpawn = myTankPrefab != null ? myTankPrefab : m_TankPrefab;
                if (myTankPrefab == null) 
                    Debug.LogError("LỖI: Không tìm thấy file '" + selectedTankName + "' trong thư mục Resources!");
            }
            else 
            {
                if (m_Tanks[i].m_CustomPrefab != null)
                {
                    finalPrefabToSpawn = m_Tanks[i].m_CustomPrefab; 
                }
                else 
                {
                    finalPrefabToSpawn = botTankPrefab != null ? botTankPrefab : m_TankPrefab;
                }
            }

            // --- BƯỚC ĐẺ XE RA SÂN ---
            m_Tanks[i].m_Instance = Instantiate(finalPrefabToSpawn, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;            
            m_Tanks[i].m_PlayerNumber = i + 1;
            m_Tanks[i].Setup();
            
            // --- CÀI ĐẶT BẬT/TẮT UI KỸ NĂNG (ĐÃ CẬP NHẬT TANK 3) ---
            if (i == 0) 
            {
                TankShooting pShoot = m_Tanks[i].m_Instance.GetComponent<TankShooting>();
                
                // Nút 1
                if (m_SpreadSkillUI != null) 
                    m_SpreadSkillUI.gameObject.SetActive(pShoot.m_HasSpreadShot || pShoot.m_HasGiantShell || pShoot.m_HasElemental || pShoot.m_HasDeathSpin);
                
                // Nút 2
                if (m_MineSkillUI != null) 
                    m_MineSkillUI.gameObject.SetActive(pShoot.m_HasMine || pShoot.m_HasTurret || pShoot.m_HasSmoke || pShoot.m_HasMagneticPull);
                
                // Nút 3
                if (m_HomingSkillUI != null) 
                    m_HomingSkillUI.gameObject.SetActive(pShoot.m_HasHomingMissile || pShoot.m_HasClone || pShoot.m_HasShield || pShoot.m_HasDash);
            }
        }
    }

    private void SetCameraTargets()
    {
        // Chỉ tạo một mảng chứa đúng 1 mục tiêu duy nhất
        Transform[] playerTarget = new Transform[1];
        
        // Gán chiếc xe đầu tiên (m_Tanks[0] - chính là xe của người chơi) vào mảng này
        playerTarget[0] = m_Tanks[0].m_Instance.transform;
        
        // Giao nhiệm vụ theo dõi cho Camera
        m_CameraControl.m_Targets = playerTarget;
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
    // CẦU NỐI UI: ĐÃ BỔ SUNG ĐẦY ĐỦ KỸ NĂNG TANK 2
    // ==========================================
    public void OnClick_SpreadShot()
    {
        if (m_Tanks != null && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && m_Tanks[0].m_Instance.activeSelf)
        {
            TankShooting shootingScript = m_Tanks[0].m_Instance.GetComponent<TankShooting>();
            if (shootingScript != null)
            {
                if (shootingScript.m_HasSpreadShot) shootingScript.FireSpreadShot();
                else if (shootingScript.m_HasGiantShell) shootingScript.FireGiantShell();
                // Bổ sung gọi kỹ năng Bắn đạn nguyên tố của Tank 2
                else if (shootingScript.m_HasElemental) shootingScript.FireElementalShot();
                
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
                if (shootingScript.m_HasMine) shootingScript.DropMine();
                else if (shootingScript.m_HasTurret) shootingScript.PlaceTurret();
                // Bổ sung gọi kỹ năng Ném lựu đạn khói của Tank 2
                else if (shootingScript.m_HasSmoke) shootingScript.DropSmoke();
                
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
                if (shootingScript.m_HasHomingMissile) shootingScript.FireHomingMissile();
                else if (shootingScript.m_HasClone) shootingScript.CreateClone();
                // Bổ sung gọi kỹ năng Bật khiên của Tank 2
                else if (shootingScript.m_HasShield) shootingScript.ActivateShield();
                
                m_HomingSkillUI.StartCooldown(shootingScript.m_HomingCooldown);
            }
        }    
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
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
        for (int i = 0; i < m_Tanks.Length; i++)
        {
            m_Tanks[i].m_Instance = Instantiate(m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
            m_Tanks[i].m_PlayerNumber = (i == 0) ? 1 : 2; 
            m_Tanks[i].Setup();
            m_Tanks[i].m_Instance.SetActive(false); 
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
        // Đảm bảo danh sách xe tăng tồn tại, và xe của người chơi (phần tử 0) đang sống
        if (m_Tanks != null && m_Tanks.Length > 0 && m_Tanks[0].m_Instance != null && m_Tanks[0].m_Instance.activeSelf)
        {
            // Tìm component bắn súng trên xe tăng đó và gọi lệnh bắn chùm
            TankShooting shootingScript = m_Tanks[0].m_Instance.GetComponent<TankShooting>();
            if (shootingScript != null)
            {
                shootingScript.FireSpreadShot();
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
            }
        }
    }
}
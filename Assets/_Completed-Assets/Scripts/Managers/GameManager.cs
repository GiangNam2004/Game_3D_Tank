using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        public int m_MaxWavesPerLevel = 3;              // Số Wave cần vượt qua để chuyển sang Map mới
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
                
                // Cài đặt Element 0 luôn là Người chơi (Phe 1), từ Element 1 trở đi là Bot (Phe 2)
                m_Tanks[i].m_PlayerNumber = (i == 0) ? 1 : 2; 
                m_Tanks[i].Setup();
                
                // Ẩn tất cả xe tăng lúc mới sinh ra để tự động bật theo từng Wave
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
                // Nếu người chơi chết -> Trở về sảnh chờ
                SceneManager.LoadScene("Lobby");
            }
            else
            {
                if (m_RoundNumber >= m_MaxWavesPerLevel)
                {
                    // Đã vượt qua đủ số Wave -> Chiến thắng và trở về sảnh
                    SceneManager.LoadScene("Lobby");
                }
                else
                {
                    // Chưa đủ Wave -> Tiếp tục đợt quái tiếp theo
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

            // Chờ đến khi Người chơi chết hoặc tất cả Bot trong Wave bị tiêu diệt
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
            // 1. Kiểm tra Người chơi
            if (!m_Tanks[0].m_Instance.activeSelf)
            {
                m_PlayerDefeated = true;
                return true; 
            }

            // 2. Kiểm tra số lượng Bot tham gia trong Wave này
            int botsToSpawn = Mathf.Min(m_RoundNumber, m_Tanks.Length - 1);
            for (int i = 1; i <= botsToSpawn; i++)
            {
                if (m_Tanks[i].m_Instance.activeSelf)
                {
                    return false; // Vẫn còn Bot sống
                }
            }

            // 3. Người chơi sống và sạch bóng địch
            m_PlayerDefeated = false;
            return true;
        }

        private void ResetTanksForWave()
        {
            // Reset và bật người chơi
            m_Tanks[0].Reset();
            m_Tanks[0].m_Instance.SetActive(true);

            // Xác định số lượng Bot được phép xuất hiện
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
    }
}
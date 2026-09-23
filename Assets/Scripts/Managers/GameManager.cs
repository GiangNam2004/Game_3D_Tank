private IEnumerator GameLoop ()
    {
        yield return StartCoroutine (RoundStarting ());
        yield return StartCoroutine (RoundPlaying());
        yield return StartCoroutine (RoundEnding());

        if (m_PlayerDefeated)
        {
            // Sửa: Người chơi chết sẽ bị đưa về sảnh Lobby
            SceneManager.LoadScene("Lobby");
        }
        else
        {
            // Kiểm tra điều kiện chuyển Map
            if (m_RoundNumber >= m_MaxWavesPerLevel)
            {
                // Sửa dứt điểm: Thắng cuộc sẽ tải thẳng về sảnh Lobby
                SceneManager.LoadScene("Lobby");
            }
            else
            {
                // Chưa đủ Wave -> Chơi đợt tiếp theo
                StartCoroutine (GameLoop ());
            }
        }
    }
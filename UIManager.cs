using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Image[] healthIcons;

    [Header("Game Over Screen")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverTimeText;

    [Header("Level Complete Screen")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI levelCompleteScoreText;
    [SerializeField] private TextMeshProUGUI levelCompleteTimeText;

    private void Start()
    {
        HideAllPanels();

        // Double-check panels are inactive
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            Debug.Log("Game Over Panel hidden on start");
        }
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
            Debug.Log("Level Complete Panel hidden on start");
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score:N0}";
        }
    }

    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            int milliseconds = Mathf.FloorToInt((time * 100) % 100);
            timerText.text = $"Time: {minutes:00}:{seconds:00}:{milliseconds:00}";
        }
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthIcons == null || healthIcons.Length == 0) return;

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (i < maxHealth)
            {
                healthIcons[i].gameObject.SetActive(true);
                healthIcons[i].enabled = i < currentHealth;
            }
            else
            {
                healthIcons[i].gameObject.SetActive(false);
            }
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = $"Final Score: {GameManager.Instance.CurrentScore:N0}";
            }

            if (gameOverTimeText != null)
            {
                float time = GameManager.Instance.LevelTimer;
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);
                gameOverTimeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }

    public void ShowLevelComplete(int finalScore, float finalTime)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);

            if (levelCompleteScoreText != null)
            {
                levelCompleteScoreText.text = $"Final Score: {finalScore:N0}";
            }

            if (levelCompleteTimeText != null)
            {
                int minutes = Mathf.FloorToInt(finalTime / 60);
                int seconds = Mathf.FloorToInt(finalTime % 60);
                levelCompleteTimeText.text = $"Time: {minutes:00}:{seconds:00}";
            }
        }
    }

    private void HideAllPanels()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
    }

    // Button callbacks
    public void OnRestartButton()
    {
        Debug.Log("OnRestartButton called");
        GameManager.Instance?.RestartLevel();
    }

    public void OnMainMenuButton()
    {
        Debug.Log("OnMainMenuButton called");
        GameManager.Instance?.LoadMainMenu();
    }
}
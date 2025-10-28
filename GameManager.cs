using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private bool useHealthSystem = false;
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float iFrameDuration = 1f;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float gameStartDelay = 0.3f; // Delay before player can be hit after scene loads

    [Header("UI References")]
    [SerializeField] private UIManager uiManager;

    [Header("Player Reference")]
    [SerializeField] private Rigidbody2D playerRigidbody;

    private PlayerController playerController;

    // Game state
    private int currentScore;
    private float levelTimer;
    private int currentHealth;
    private bool isInvulnerable;
    private float iFrameTimer;
    private bool levelComplete;
    private float gameStartTimer;

    public int CurrentScore => currentScore;
    public float LevelTimer => levelTimer;
    public int CurrentHealth => currentHealth;
    public bool IsInvulnerable => isInvulnerable;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeGame();
    }

    private void Update()
    {
        // Update game start timer
        if (gameStartTimer < gameStartDelay)
        {
            gameStartTimer += Time.deltaTime;
        }

        if (!levelComplete)
        {
            levelTimer += Time.deltaTime;
        }

        HandleIFrames();
        UpdateUI();
    }

    private void InitializeGame()
    {
        currentScore = 0;
        levelTimer = 0f;
        currentHealth = useHealthSystem ? maxHealth : 1;
        levelComplete = false;
        gameStartTimer = 0f; // Reset start timer
        isInvulnerable = false; // Reset invulnerability

        // Auto-find player if not assigned
        if (playerRigidbody == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerRigidbody = player.GetComponent<Rigidbody2D>();
            }
        }

        Debug.Log("Game Initialized - Start delay active for " + gameStartDelay + " seconds");
    }

    public void AddScore(int points)
    {
        currentScore += points;

        // Apply time bonus multiplier (example: faster = more points)
        float timeMultiplier = Mathf.Max(1f, 5f - (levelTimer / 10f));
        currentScore = Mathf.RoundToInt(currentScore * timeMultiplier);
    }

    public void PlayerHit()
    {
        // Ignore hits during startup delay or invulnerability
        if (isInvulnerable || gameStartTimer < gameStartDelay)
        {
            Debug.Log("Player hit ignored - invulnerable or startup delay active");
            return;
        }

        Debug.Log("Player Hit! - Health: " + currentHealth);

        if (useHealthSystem)
        {
            currentHealth--;
            ActivateIFrames();
            ApplyKnockback();

            if (currentHealth <= 0)
            {
                GameOver();
            }
        }
        else
        {
            // Flawless mode - instant game over
            GameOver();
        }
    }

    private void ApplyKnockback()
    {
        if (playerRigidbody != null)
        {
            // Apply upward and backward knockback
            playerRigidbody.linearVelocity = new Vector2(-knockbackForce, knockbackForce);
        }
    }

    private void ActivateIFrames()
    {
        isInvulnerable = true;
        iFrameTimer = iFrameDuration;
    }

    private void HandleIFrames()
    {
        if (isInvulnerable)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0)
            {
                isInvulnerable = false;
            }
        }
    }

    public void CompleteLevel()
    {
        if (levelComplete) return;

        levelComplete = true;

        // Calculate final score with time bonus
        float timeBonus = Mathf.Max(0, 1000 - (levelTimer * 10));
        currentScore += Mathf.RoundToInt(timeBonus);

        if (uiManager != null)
        {
            uiManager.ShowLevelComplete(currentScore, levelTimer);
        }

        // Pause game like Game Over does
        Time.timeScale = 0f;
        Debug.Log("Level Complete - Time.timeScale set to 0");
    }

    private void GameOver()
    {
        if (levelComplete) return; // Prevent game over if already complete

        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }

        // Pause game
        Time.timeScale = 0f;
        Debug.Log("Game Over - Time.timeScale set to 0");
    }

    public void RestartLevel()
    {
        Debug.Log("RestartLevel called - Resetting time scale");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0); // Assumes main menu is scene 0
    }

    private void UpdateUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateScore(currentScore);
            uiManager.UpdateTimer(levelTimer);
            if (useHealthSystem)
            {
                uiManager.UpdateHealth(currentHealth, maxHealth);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
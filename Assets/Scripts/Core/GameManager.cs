using UnityEngine;

/// <summary>
/// Controlador principal del flujo del juego
/// Gestiona: Menu Inicio -> Modo de Juego -> Game Over -> Menu Inicio
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias de Paneles")]
    public GameObject gameplayPanel; // Panel que contiene toda la UI de juego
    public GameObject gameOverPanel;

    [Header("Referencias de Managers")]
    public BossRushManager bossRushManager;
    public CombatManager combatManager;
    public RunSaveManager runSaveManager;
    public GameOverUI gameOverUI;

    [Header("Estado del Juego")]
    private bool gameInProgress = false;

    void Awake()
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
    }

    void Start()
    {
        // Suscribirse a eventos
        if (combatManager != null)
        {
            combatManager.GameOver += HandleGameOver;
        }

        if (bossRushManager != null)
        {
            bossRushManager.OnRunStarted += HandleRunStarted;
            bossRushManager.OnRunEnded += HandleRunEnded;
        }

        // Iniciar flujo según GlobalData
        if (Sveliaty.Core.GlobalData.ShouldLoadSave)
        {
            Debug.Log("GameManager: Restaurando partida guardada...");
            if (runSaveManager != null)
            {
                runSaveManager.LoadSavedRun();
            }
            else
            {
                Debug.LogError("RunSaveManager no asignado en GameManager");
            }
        }
        else
        {
            Debug.Log("GameManager: Iniciando nueva partida...");
            if (bossRushManager != null)
            {
                bossRushManager.StartNewRun();
            }
            else
            {
                Debug.LogError("BossRushManager no asignado en GameManager");
            }
        }
        
        if (gameplayPanel != null) gameplayPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.GameOver -= HandleGameOver;
        }

        if (bossRushManager != null)
        {
            bossRushManager.OnRunStarted -= HandleRunStarted;
            bossRushManager.OnRunEnded -= HandleRunEnded;
        }
    }


    void HandleRunStarted(CombatMode mode)
    {
        Debug.Log("Run iniciada");
    }

    void HandleRunEnded(int finalScore, int enemiesDefeated)
    {
        Debug.Log("Run terminada - Score: " + finalScore + ", Enemigos: " + enemiesDefeated);

        // Si se derrotaron todos los enemigos (Boss), es una victoria
        if (bossRushManager != null && enemiesDefeated >= bossRushManager.maxEnemiesPerRun)
        {
            gameInProgress = false;

            gameplayPanel.SetActive(false);
            gameOverPanel.SetActive(true);

            if (gameOverUI == null)
            {
                Debug.LogWarning("⚠️ GameManager: gameOverUI no está asignado en el Inspector. Intentando buscarlo...");
                if (gameOverPanel != null)
                    gameOverUI = gameOverPanel.GetComponentInChildren<GameOverUI>(true);
                
                if (gameOverUI == null)
                    gameOverUI = Object.FindObjectOfType<GameOverUI>(true);
            }

            if (gameOverUI != null && PlayerManager.Instance != null)
            {
                int fCards = PlayerManager.Instance.GetCards(AffinityType.Fuerza);
                int aCards = PlayerManager.Instance.GetCards(AffinityType.Agilidad);
                int dCards = PlayerManager.Instance.GetCards(AffinityType.Destreza);

                EnemyInstance lastBoss = combatManager != null ? combatManager.GetCurrentEnemy() : null;

                // Llamar con isVictory = true, y pasando al líder final
                gameOverUI.ShowGameOver(finalScore, fCards, aCards, dCards, lastBoss, true);
            }
        }
    }

    void HandleGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy)
    {
        Debug.Log("GAME OVER - Score: " + finalScore);
        
        gameInProgress = false;

        gameplayPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOver(finalScore, fuerzaCards, agilidadCards, destrezaCards, defeatedBy);
        }
    }

    public void RestartGame()
    {
        Debug.Log("Reiniciando juego...");
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene"); // Asumiendo que así se llama
    }

    // GETTERS
    public bool IsGameInProgress() => gameInProgress;
}
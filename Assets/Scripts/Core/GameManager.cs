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

        // Si se derrotaron todos los enemigos → Victoria final
        if (bossRushManager != null && enemiesDefeated >= bossRushManager.maxEnemiesPerRun)
        {
            gameInProgress = false;
            RunSaveManager.ClearSavedRun();

            // Empaquetar datos para la escena del personaje
            if (PlayerManager.Instance != null && AbilityManager.Instance != null)
            {
                Sveliaty.Core.GlobalData.PendingVictoryData = new Sveliaty.Core.VictoryData
                {
                    activeSkills  = AbilityManager.Instance.GetAllActiveSkillStates(),
                    cardsFuerza   = PlayerManager.Instance.GetCards(AffinityType.Fuerza),
                    cardsAgilidad = PlayerManager.Instance.GetCards(AffinityType.Agilidad),
                    cardsDestreza = PlayerManager.Instance.GetCards(AffinityType.Destreza),
                    finalScore    = finalScore
                };
            }
            else
            {
                Debug.LogWarning("[GameManager] PlayerManager o AbilityManager.Instance son null al preparar VictoryData.");
                Sveliaty.Core.GlobalData.PendingVictoryData = new Sveliaty.Core.VictoryData
                {
                    finalScore = finalScore
                };
            }

            // En vez de cargar la escena directamente, mostramos el panel de Game Over en modo Victoria
            // para que el jugador vea sus stats finales. El botón de ese panel cargará la escena del personaje.
            
            gameInProgress = false;
            if (gameplayPanel != null) gameplayPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            if (gameOverUI != null)
            {
                int f = PlayerManager.Instance != null ? PlayerManager.Instance.GetCards(AffinityType.Fuerza) : 0;
                int a = PlayerManager.Instance != null ? PlayerManager.Instance.GetCards(AffinityType.Agilidad) : 0;
                int d = PlayerManager.Instance != null ? PlayerManager.Instance.GetCards(AffinityType.Destreza) : 0;
                
                EnemyInstance currentEnemy = combatManager != null ? combatManager.GetCurrentEnemy() : null;
                
                gameOverUI.ShowGameOver(finalScore, f, a, d, currentEnemy, true);
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
    }

    // GETTERS
    public bool IsGameInProgress() => gameInProgress;
}
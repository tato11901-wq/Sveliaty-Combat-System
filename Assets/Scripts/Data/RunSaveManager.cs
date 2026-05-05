using UnityEngine;
using System;

/// <summary>
/// Gestor de guardado/carga automatico de runs
/// Guarda el estado completo del juego en PlayerPrefs usando JSON
/// Permite continuar una run despues de cerrar el juego
/// </summary>
public class RunSaveManager : MonoBehaviour
{
    public static RunSaveManager Instance { get; private set; }

    public const string SAVE_KEY = "CurrentRunSave";
    public const string HAS_SAVE_KEY = "HasSavedRun";

    [Header("References")]
    public PlayerManager playerManager;
    public BossRushManager bossRushManager;
    public CombatManager combatManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========== ESTRUCTURA DE DATOS ==========

    [System.Serializable]
    public class RunSaveData
    {
        // Info general
        public string timestamp;
        public string combatModeString; // Guardamos como string porque enum
        
        // Estado del jugador
        public int playerCurrentLife;
        public int playerMaxLife;
        public int playerScore;
        public int fuerzaCards;
        public int agilidadCards;
        public int destrezaCards;
        
        // Estado de la run
        public int enemiesDefeatedThisRun;
        
        // Estado del combate actual (si existe)
        public bool isInCombat;
        public string currentEnemyName; // Usamos name en lugar de id
        public string currentEnemyTierString;
        public int currentEnemyAttemptsRemaining;
        public int currentEnemyRPGHealth; // Para modo RPG
        public int turnsUsedThisCombat;
        
        // TODO: Maldiciones activas (para expansion futura)
        // public List<ActiveCurseData> activeCurses;
    }

    // ========== GUARDADO ==========

    /// <summary>
    /// Guarda el estado actual de la run
    /// Llamar despues de cada accion importante (ataque, fin de combate, etc.)
    /// </summary>
    public void SaveCurrentRun()
    {
        if (playerManager == null || bossRushManager == null)
        {
            Debug.LogWarning("No se puede guardar: Referencias no asignadas");
            return;
        }

        if (!bossRushManager.IsRunInProgress())
        {
            Debug.Log("No hay run en progreso para guardar");
            return;
        }

        RunSaveData saveData = new RunSaveData
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            combatModeString = bossRushManager.GetCurrentMode().ToString(),
            
            // Jugador
            playerCurrentLife = playerManager.GetCurrentLife(),
            playerMaxLife = playerManager.GetMaxLife(),
            playerScore = playerManager.GetScore(),
            fuerzaCards = playerManager.GetCards(AffinityType.Fuerza),
            agilidadCards = playerManager.GetCards(AffinityType.Agilidad),
            destrezaCards = playerManager.GetCards(AffinityType.Destreza),
            
            // Run
            enemiesDefeatedThisRun = bossRushManager.GetCurrentNodeIndex(),
            
            // Combate actual
            isInCombat = false
        };

        // Si hay combate activo, guardar su estado
        if (combatManager != null && combatManager.HasActiveEnemy() && !combatManager.IsCombatEnded())
        {
            EnemyInstance currentEnemy = combatManager.GetCurrentEnemy();
            
            saveData.isInCombat = true;
            saveData.currentEnemyName = currentEnemy.enemyData.name;
            saveData.currentEnemyTierString = currentEnemy.enemyTierData.enemyTier.ToString();
            saveData.currentEnemyAttemptsRemaining = currentEnemy.attemptsRemaining;
            saveData.currentEnemyRPGHealth = currentEnemy.currentRPGHealth;
            saveData.turnsUsedThisCombat = combatManager.GetTurnsUsedThisCombat();
        }

        // Serializar a JSON
        string json = JsonUtility.ToJson(saveData, true);
        
        // Guardar en PlayerPrefs
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.SetInt(HAS_SAVE_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("Run guardada: " + saveData.timestamp);
    }

    // ========== CARGA ==========

    /// <summary>
    /// Carga la run guardada
    /// </summary>
    public bool LoadSavedRun()
    {
        if (!HasSavedRun())
        {
            Debug.Log("No hay run guardada");
            return false;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("JSON de guardado vacio");
            return false;
        }

        try
        {
            RunSaveData saveData = JsonUtility.FromJson<RunSaveData>(json);
            
            if (saveData == null)
            {
                Debug.LogError("No se pudo deserializar el guardado");
                return false;
            }

            ApplySaveData(saveData);
            
            Debug.Log("Run cargada: " + saveData.timestamp);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al cargar run: " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// Aplica los datos cargados al estado del juego
    /// </summary>
    void ApplySaveData(RunSaveData saveData)
    {

        // Parsear modo de combate
        CombatMode mode;
        if (!Enum.TryParse(saveData.combatModeString, out mode))
        {
            mode = CombatMode.PlayerChooses;
            Debug.LogWarning("Modo de combate invalido, usando PlayerChooses");
        }

        // Restaurar estado del jugador
        playerManager.SetHealth(saveData.playerCurrentLife);
        playerManager.SetScore(saveData.playerScore);
        
        // Restaurar cartas (primero resetear a 0)
        int currentFuerza = playerManager.GetCards(AffinityType.Fuerza);
        int currentAgilidad = playerManager.GetCards(AffinityType.Agilidad);
        int currentDestreza = playerManager.GetCards(AffinityType.Destreza);
        
        playerManager.RemoveCards(AffinityType.Fuerza, currentFuerza);
        playerManager.RemoveCards(AffinityType.Agilidad, currentAgilidad);
        playerManager.RemoveCards(AffinityType.Destreza, currentDestreza);
        
        playerManager.AddCards(AffinityType.Fuerza, saveData.fuerzaCards);
        playerManager.AddCards(AffinityType.Agilidad, saveData.agilidadCards);
        playerManager.AddCards(AffinityType.Destreza, saveData.destrezaCards);

        // Marcar run como en progreso
        bossRushManager.RestoreRunState(saveData.enemiesDefeatedThisRun);

        // Si habia combate activo, restaurarlo
        if (saveData.isInCombat)
        {
            RestoreCombatState(saveData, mode);
        }
        else
        {
            // No habia combate, iniciar uno nuevo
            bossRushManager.StartNextCombat();
        }
    }

    /// <summary>
    /// Restaura el estado de un combate en progreso
    /// </summary>
    void RestoreCombatState(RunSaveData saveData, CombatMode mode)
    {
        // Buscar el enemigo en la base de datos
        EnemyDatabase enemyDB = bossRushManager.enemyDatabase;
        if (enemyDB == null)
        {
            Debug.LogError("EnemyDatabase no encontrado");
            bossRushManager.StartNextCombat();
            return;
        }

        EnemyData enemyData = enemyDB.GetEnemyByName(saveData.currentEnemyName);
        if (enemyData == null)
        {
            Debug.LogError("Enemigo con nombre " + saveData.currentEnemyName + " no encontrado");
            bossRushManager.StartNextCombat();
            return;
        }

        // Parsear tier
        EnemyTier tier;
        if (!Enum.TryParse(saveData.currentEnemyTierString, out tier))
        {
            tier = EnemyTier.Tier_1;
            Debug.LogWarning("Tier invalido, usando Tier_1");
        }

        // Iniciar combate
        combatManager.StartCombat(enemyData, tier, mode);

        // Restaurar estado del combate
        EnemyInstance enemy = combatManager.GetCurrentEnemy();
        if (enemy != null)
        {
            enemy.attemptsRemaining = saveData.currentEnemyAttemptsRemaining;
            enemy.currentRPGHealth = saveData.currentEnemyRPGHealth;
            
            // Restaurar turnos usados
            combatManager.SetTurnsUsedThisCombat(saveData.turnsUsedThisCombat);
        }
    }

    // ========== QUERIES ==========

    /// <summary>
    /// Verifica si existe una run guardada
    /// </summary>
    public static bool HasSavedRun()
    {
        return PlayerPrefs.GetInt(HAS_SAVE_KEY, 0) == 1;
    }

    /// <summary>
    /// Obtiene informacion de la run guardada sin cargarla
    /// </summary>
    public static RunSaveData GetSavedRunInfo()
    {
        if (!HasSavedRun()) return null;

        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonUtility.FromJson<RunSaveData>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Elimina la run guardada
    /// Llamar al terminar una run (victoria o derrota)
    /// </summary>
    public static void ClearSavedRun()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.SetInt(HAS_SAVE_KEY, 0);
        PlayerPrefs.Save();
        
        Debug.Log("Run guardada eliminada");
    }

    // ========== AUTO-SAVE HOOKS ==========

    /// <summary>
    /// Llama a esto despues de cada accion importante
    /// </summary>
    public void AutoSave()
    {
        if (bossRushManager != null && bossRushManager.IsRunInProgress())
        {
            SaveCurrentRun();
        }
    }
}
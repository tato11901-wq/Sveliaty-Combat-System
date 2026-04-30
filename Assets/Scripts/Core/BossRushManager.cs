using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gestor del modo Boss Rush con sistema de progresion
/// - 20 enemigos máximo por run
/// - Progresión de tiers: más tier 3 mientras más avanzas
/// - Enemigo 20 es un boss con stats x3
/// </summary>
public enum NodeType
{
    Combat,
    Shop,
    Boss
}

public class BossRushManager : MonoBehaviour
{
    [Header("References")]
    public CombatManager combatManager;
    public PlayerManager playerManager;
    public EnemyDatabase enemyDatabase;

    [Header("Boss Rush Settings")]
    public CombatMode defaultMode = CombatMode.PlayerChooses;
    
    [Header("Progression Settings")]
    public int maxEnemiesPerRun = 20;
    public float bossStatsMultiplier = 3f;
    [Tooltip("Cada cuántos combates ganados aparece una tienda")]
    public int enemiesPerShop = 3; 
    
    [Header("Run Statistics")]
    private int enemiesDefeatedThisRun = 0;
    private int totalTurnsUsed = 0;
    private bool runInProgress = false;
    
    // Eventos
    public event Action<CombatMode> OnRunStarted;
    public event Action<int, int> OnRunEnded; // (finalScore, enemiesDefeated)
    public event Action<int, int> OnProgressionUpdate; // (current, max) para UI
    public event Action OnShopReached; // Evento nuevo para la Tienda

    void Start()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatEnd += HandleCombatEnd;
            combatManager.GameOver += HandleGameOver;
        }
    }

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatEnd -= HandleCombatEnd;
            combatManager.GameOver -= HandleGameOver;
        }
    }

    public void StartNewRun(CombatMode mode)
    {
        Debug.Log("BossRushManager: Iniciando nueva run en modo " + mode);
        
        runInProgress = true;
        defaultMode = mode;
        
        enemiesDefeatedThisRun = 0;
        totalTurnsUsed = 0;
        
        if (playerManager != null)
        {
            playerManager.InitializeForNewRun(5);
        }
        else
        {
            Debug.LogError("PlayerManager no asignado en BossRushManager");
        }
        
        OnRunStarted?.Invoke(mode);
        OnProgressionUpdate?.Invoke(0, maxEnemiesPerRun);
        
        StartNextCombat();
    }

    public NodeType GetNodeType(int targetEnemyIndex)
    {
        if (targetEnemyIndex >= maxEnemiesPerRun - 1) return NodeType.Boss;
        
        // Si el jugador ha derrotado a la cantidad necesaria de enemigos para la tienda
        // Ejemplo: Si enemiesPerShop es 3, los nodos serán: 0(C), 1(C), 2(C), 3(S), 4(C), 5(C), 6(C), 7(S)...
        // Es decir, si el índice cae justo en el múltiplo (targetEnemyIndex % 4 == 3)
        if ((targetEnemyIndex + 1) % (enemiesPerShop + 1) == 0)
        {
            return NodeType.Shop;
        }
        return NodeType.Combat;
    }

    public List<NodeType> GetUpcomingNodes(int count)
    {
        List<NodeType> nodes = new List<NodeType>();
        for (int i = 0; i < count; i++)
        {
            int checkIndex = enemiesDefeatedThisRun + i;
            if (checkIndex >= maxEnemiesPerRun) break;
            nodes.Add(GetNodeType(checkIndex));
        }
        return nodes;
    }

    public void StartNextCombat()
    {
        if (!runInProgress)
        {
            Debug.LogWarning("No hay run en progreso");
            return;
        }

        NodeType nextNode = GetNodeType(enemiesDefeatedThisRun);

        if (nextNode == NodeType.Shop)
        {
            Debug.Log("¡TIENDA ENCONTRADA! Nodo: " + (enemiesDefeatedThisRun + 1));
            OnShopReached?.Invoke();
            return; // Detenemos el flujo aquí hasta que el jugador salga de la tienda
        }

        if (enemyDatabase == null)
        {
            Debug.LogError("EnemyDatabase no asignado en BossRushManager");
            return;
        }

        bool isFinalBoss = (nextNode == NodeType.Boss);
        
        EnemyData enemyData;
        EnemyTier tier;

        if (isFinalBoss)
        {
            // Boss final: Tier 3 garantizado
            (enemyData, tier) = GetRandomEnemyWithTier(EnemyTier.Tier_3);
            Debug.Log("BOSS FINAL! Enemigo " + maxEnemiesPerRun + " de " + maxEnemiesPerRun);
        }
        else
        {
            // Enemigo normal con progresión
            tier = GetTierBasedOnProgression();
            (enemyData, tier) = GetRandomEnemyWithTier(tier);
            Debug.Log("Enemigo " + (enemiesDefeatedThisRun + 1) + " de " + maxEnemiesPerRun + " (Tier: " + tier + ")");
        }

        if (enemyData == null)
        {
            Debug.LogError("No se pudo obtener enemigo");
            return;
        }

        float currentMultiplier = 1f;

        // Si es el boss final, aplicamos el multiplicador al iniciar el combate
        if (isFinalBoss)
        {
            currentMultiplier = bossStatsMultiplier;
            Debug.Log("Boss stats multiplicados x" + bossStatsMultiplier);
        }

        if (combatManager != null)
        {
            combatManager.StartCombat(enemyData, tier, defaultMode, currentMultiplier);
        }
        else
        {
            Debug.LogError("CombatManager no asignado en BossRushManager");
        }
    }

    /// <summary>
    /// Determina el tier según la progresión en la run
    /// </summary>
    EnemyTier GetTierBasedOnProgression()
    {
        int currentEnemy = enemiesDefeatedThisRun + 1;
        float progress = (float)currentEnemy / maxEnemiesPerRun;

        // Definir probabilidades según progresión
        float tier1Chance, tier2Chance, tier3Chance;

        if (progress <= 0.25f) // Rondas 1-5
        {
            tier1Chance = 0.70f;
            tier2Chance = 0.25f;
            tier3Chance = 0.05f;
        }
        else if (progress <= 0.50f) // Rondas 6-10
        {
            tier1Chance = 0.40f;
            tier2Chance = 0.40f;
            tier3Chance = 0.20f;
        }
        else if (progress <= 0.75f) // Rondas 11-15
        {
            tier1Chance = 0.20f;
            tier2Chance = 0.40f;
            tier3Chance = 0.40f;
        }
        else // Rondas 16-19
        {
            tier1Chance = 0.10f;
            tier2Chance = 0.30f;
            tier3Chance = 0.60f;
        }

        // Generar tier basado en probabilidades
        float roll = UnityEngine.Random.Range(0f, 1f);

        if (roll < tier1Chance)
            return EnemyTier.Tier_1;
        else if (roll < tier1Chance + tier2Chance)
            return EnemyTier.Tier_2;
        else
            return EnemyTier.Tier_3;
    }

    /// <summary>
    /// Obtiene un enemigo aleatorio con un tier específico
    /// </summary>
    (EnemyData, EnemyTier) GetRandomEnemyWithTier(EnemyTier desiredTier)
    {
        // Obtener enemigo aleatorio
        EnemyData randomEnemy = enemyDatabase.allEnemies[UnityEngine.Random.Range(0, enemyDatabase.allEnemies.Count)];

        // Verificar si tiene el tier deseado
        bool hasTier = false;
        foreach (var tierData in randomEnemy.enemyTierData)
        {
            if (tierData.enemyTier == desiredTier)
            {
                hasTier = true;
                break;
            }
        }

        // Si no tiene el tier deseado, buscar otro enemigo (máximo 10 intentos)
        int attempts = 0;
        while (!hasTier && attempts < 10)
        {
            randomEnemy = enemyDatabase.allEnemies[UnityEngine.Random.Range(0, enemyDatabase.allEnemies.Count)];
            
            foreach (var tierData in randomEnemy.enemyTierData)
            {
                if (tierData.enemyTier == desiredTier)
                {
                    hasTier = true;
                    break;
                }
            }
            
            attempts++;
        }

        // Si después de 10 intentos no encontró, usar cualquier tier disponible
        if (!hasTier && randomEnemy.enemyTierData.Length > 0)
        {
            Debug.LogWarning("No se encontró enemigo con tier " + desiredTier + ", usando tier disponible");
            desiredTier = randomEnemy.enemyTierData[0].enemyTier;
        }

        return (randomEnemy, desiredTier);
    }



    void HandleCombatEnd(bool victory, int currentScore, AffinityType rewardCard, int lifeLost)
    {
        if (!runInProgress) return;

        if (victory)
        {
            enemiesDefeatedThisRun++;
            Debug.Log("Enemigos derrotados en esta run: " + enemiesDefeatedThisRun);
            
            // Notificar progresión
            OnProgressionUpdate?.Invoke(enemiesDefeatedThisRun, maxEnemiesPerRun);
            
            // Verificar si completó la run (derrotó al boss final)
            if (enemiesDefeatedThisRun >= maxEnemiesPerRun)
            {
                Debug.Log("RUN COMPLETADA! Derrotaste al boss final!");
                EndRun(currentScore);
                
                // Aquí podrías mostrar una pantalla de victoria especial
            }
        }
    }

    public void ContinueToNextCombat()
    {
        if (!runInProgress)
        {
            Debug.LogWarning("No hay run en progreso");
            return;
        }

        if (!playerManager.IsAlive())
        {
            Debug.LogWarning("El jugador esta muerto, no se puede continuar");
            return;
        }

        Debug.Log("BossRushManager: Continuando al siguiente combate");
        StartNextCombat();
    }

    void HandleGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy)
    {
        Debug.Log("BossRushManager: Game Over - Score: " + finalScore);
        EndRun(finalScore);
        
        // Limpiar guardado
        if (RunSaveManager.Instance != null)
        {
            RunSaveManager.Instance.ClearSavedRun();
        }
    }

    public void EndRun(int finalScore)
    {
        if (!runInProgress)
        {
            Debug.LogWarning("No hay run en progreso para terminar");
            return;
        }

        Debug.Log("BossRushManager: Run terminada");
        Debug.Log("Enemigos derrotados: " + enemiesDefeatedThisRun);
        Debug.Log("Score final: " + finalScore);
        
        runInProgress = false;
        
        OnRunEnded?.Invoke(finalScore, enemiesDefeatedThisRun);
        
        // Limpiar guardado
        if (RunSaveManager.Instance != null)
        {
            RunSaveManager.Instance.ClearSavedRun();
        }
    }

    public void ForceEndRun()
    {
        if (runInProgress)
        {
            int currentScore = playerManager != null ? playerManager.GetScore() : 0;
            EndRun(currentScore);
        }
    }

    /// <summary>
    /// Restaura el estado de una run guardada
    /// </summary>
    public void RestoreRunState(CombatMode mode, int enemiesDefeated)
    {
        Debug.Log("BossRushManager: Restaurando run - Modo: " + mode + ", Enemigos derrotados: " + enemiesDefeated);
        
        runInProgress = true;
        defaultMode = mode;
        enemiesDefeatedThisRun = enemiesDefeated;
        
        OnRunStarted?.Invoke(mode);
        OnProgressionUpdate?.Invoke(enemiesDefeatedThisRun, maxEnemiesPerRun);
    }

    public void ClearCurrentRun()
    {
        runInProgress = false;
        enemiesDefeatedThisRun = 0;
        totalTurnsUsed = 0;
    }

    // GETTERS
    public bool IsRunInProgress() => runInProgress;
    public int GetEnemiesDefeatedThisRun() => enemiesDefeatedThisRun;
    public CombatMode GetCurrentMode() => defaultMode;
    public int GetMaxEnemies() => maxEnemiesPerRun;
    public float GetRunProgress() => (float)enemiesDefeatedThisRun / maxEnemiesPerRun;
}
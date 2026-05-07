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

    [Tooltip("Base de datos exclusiva de enemigos élite. Si se asigna, los nodos Boss usan élites en lugar de enemigos normales T3.")]
    public EliteEnemyDatabase eliteDatabase;

    [Header("Boss Rush Settings")]
    public CombatMode defaultMode = CombatMode.TraditionalRPG;
    
    [Header("Progression Settings")]
    public int maxEnemiesPerRun = 20;
    public float bossStatsMultiplier = 3f;
    [Tooltip("Cada cuántos combates (nodos) aparece una tienda")]
    public int enemiesPerShop = 3; 
    [Tooltip("Cada cuántos combates (nodos) aparece un Boss (Final de zona)")]
    public int enemiesPerBoss = 5; 
    
    [Header("Run Statistics (Debug)")]
    [SerializeField] private int currentNodeIndex = 0;
    [SerializeField] private int totalTurnsUsed = 0;
    [SerializeField] private bool runInProgress = false;

    [Header("Escalado (GDD: Curva Logística)")]
    [Tooltip("Número de Elites derrotados en la run actual (activa el escalado)")]
    [SerializeField] private int elitesDefeated = 0;
    [Tooltip("Límite máximo del multiplicador extra (GDD: L = 2.5 para mayor agresividad)")]
    public float scalingL = 2.5f;
    [Tooltip("Velocidad de la curva (GDD: k = 0.5)")]
    public float scalingK = 0.5f;
    [Tooltip("Punto medio de la curva, en número de elites (GDD: m = 2 = Elite 2, Ronda 10)")]
    public float scalingM = 2f;
    
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

    public void StartNewRun()
    {
        Debug.Log("BossRushManager: Iniciando nueva run");
        
        runInProgress = true;
        defaultMode = CombatMode.TraditionalRPG;
        
        currentNodeIndex = 0;
        totalTurnsUsed = 0;
        elitesDefeated = 0;
        
        if (playerManager != null)
        {
            playerManager.InitializeForNewRun(5);
        }
        else
        {
            Debug.LogError("PlayerManager no asignado en BossRushManager");
        }
        
        OnRunStarted?.Invoke(defaultMode);
        OnProgressionUpdate?.Invoke(0, maxEnemiesPerRun);
        
        StartNextCombat();
    }

    public NodeType GetNodeType(int targetEnemyIndex)
    {
        // Boss final siempre garantiza un jefe al final de la run
        if (targetEnemyIndex >= maxEnemiesPerRun - 1) return NodeType.Boss;
        
        // Bosses intermedios (ej: cada 5 enemigos)
        if (enemiesPerBoss > 0 && (targetEnemyIndex + 1) % enemiesPerBoss == 0)
        {
            return NodeType.Boss;
        }

        // Tiendas (ej: cada 3 enemigos. Si coincide con boss, tiene prioridad el boss)
        if (enemiesPerShop > 0 && (targetEnemyIndex + 1) % (enemiesPerShop + 1) == 0)
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
            int checkIndex = currentNodeIndex + i;
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

        NodeType nextNode = GetNodeType(currentNodeIndex);

        if (nextNode == NodeType.Shop)
        {
            Debug.Log("¡TIENDA ENCONTRADA! Nodo: " + (currentNodeIndex + 1));
            OnShopReached?.Invoke();
            return; // Detenemos el flujo aquí hasta que el jugador salga de la tienda
        }

        if (enemyDatabase == null)
        {
            Debug.LogError("EnemyDatabase no asignado en BossRushManager");
            return;
        }

        bool isBoss = (nextNode == NodeType.Boss);

        // ── Ruta élite ────────────────────────────────────────────────────────
        if (isBoss && eliteDatabase != null && eliteDatabase.allElites != null && eliteDatabase.allElites.Count > 0)
        {
            int eliteTier  = UnityEngine.Mathf.Clamp(elitesDefeated + 1, 1, 4);
            EliteEnemyData eliteData = eliteDatabase.GetRandom();

            int eliteExtraAttempts = 0;
            if (Sveliaty.Passives.PassiveManager.Instance != null)
                eliteExtraAttempts = Sveliaty.Passives.PassiveManager.Instance.GetModifiedAttempts(eliteExtraAttempts);

            if (currentNodeIndex >= maxEnemiesPerRun - 1)
            {
                Debug.Log($"BOSS FINAL (Élite Tier {eliteTier}): {eliteData.displayName}");
                combatManager.SetFinalBoss(true);
            }
            else
            {
                Debug.Log($"BOSS DE ZONA (Élite Tier {eliteTier}): {eliteData.displayName}");
            }

            combatManager.StartEliteCombat(eliteData, eliteTier, eliteExtraAttempts);
            combatManager.SetEliteRound(true);
            return;
        }


        // ── Ruta normal (fallback o sin eliteDatabase) ────────────────────────
        EnemyData enemyData;
        EnemyTier tier;

        if (isBoss)
        {
            // Todos los bosses (intermedios o final) son Tier 3 garantizado
            (enemyData, tier) = GetRandomEnemyWithTier(EnemyTier.Tier_3);
            
            if (currentNodeIndex >= maxEnemiesPerRun - 1)
                Debug.Log("BOSS FINAL! Enemigo " + maxEnemiesPerRun + " de " + maxEnemiesPerRun);
            else
                Debug.Log("BOSS DE ZONA! Enemigo " + (currentNodeIndex + 1) + " de " + maxEnemiesPerRun);
        }
        else
        {
            // Enemigo normal con progresión
            tier = GetTierBasedOnProgression();
            (enemyData, tier) = GetRandomEnemyWithTier(tier);
            Debug.Log("Enemigo " + (currentNodeIndex + 1) + " de " + maxEnemiesPerRun + " (Tier: " + tier + ")");
        }

        if (enemyData == null)
        {
            Debug.LogError("No se pudo obtener enemigo");
            return;
        }

        float currentMultiplier = 1f;
        int extraAttempts = 0;

        if (isBoss)
        {
            // Boss/Elite: usa el multiplicador fijo del inspector y +3 intentos
            currentMultiplier = bossStatsMultiplier;
            extraAttempts = 3;
            Debug.Log("Boss stats multiplicados x" + bossStatsMultiplier + " (+3 intentos)");
        }
        else
        {
            // Enemigo normal: aplica la curva logística SOLO si ya se derrotó al menos 1 élite
            if (elitesDefeated > 0)
            {
                currentMultiplier = CalculateScalingMultiplier(elitesDefeated);
                extraAttempts = elitesDefeated; // 1 por cada escalado (élite derrotado)
                Debug.Log($"Escalado logistico aplicado: x{currentMultiplier:F2} (Elites: {elitesDefeated}, +{extraAttempts} intentos)");
            }
        }

        if (Sveliaty.Passives.PassiveManager.Instance != null)
        {
            currentMultiplier = Sveliaty.Passives.PassiveManager.Instance.GetModifiedEnemyScaling(currentMultiplier);
            extraAttempts = Sveliaty.Passives.PassiveManager.Instance.GetModifiedAttempts(extraAttempts);
        }

        if (combatManager != null)
        {
            if (currentNodeIndex >= maxEnemiesPerRun - 1)
                combatManager.SetFinalBoss(true);
                
            combatManager.StartCombat(enemyData, tier, defaultMode, currentMultiplier, extraAttempts);
            if (isBoss) combatManager.SetEliteRound(true);
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
        int currentEnemy = currentNodeIndex + 1;
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

        // Si el nodo que acabó era un Boss/Elite, incrementar el contador de escalado
        // (el escalado se activa independientemente de si ganaó o perdió, según el GDD)
        NodeType completedNode = GetNodeType(currentNodeIndex);
        if (completedNode == NodeType.Boss)
        {
            elitesDefeated++;
            Debug.Log($"Elite derrotado. Total elites: {elitesDefeated}. Nuevo multiplicador: x{CalculateScalingMultiplier(elitesDefeated):F2}");
        }

        // Avanzamos el índice del nodo sin importar si ganamos o perdimos (siempre que sigamos vivos)
        currentNodeIndex++;
        Debug.Log("Nodos completados en esta run: " + currentNodeIndex);
        
        // Notificar progresión (usamos currentNodeIndex como el avance actual)
        OnProgressionUpdate?.Invoke(currentNodeIndex, maxEnemiesPerRun);
        
        // Verificar si completó la run (llegó al final del recorrido)
        if (currentNodeIndex >= maxEnemiesPerRun)
        {
            Debug.Log("RUN COMPLETADA! Llegaste al final del calabozo.");
            EndRun(currentScore);
            
            // Aquí podrías mostrar una pantalla de victoria especial
        }
    }

    public void CompleteShopNode()
    {
        if (!runInProgress) return;

        // Avanzamos el índice del nodo
        currentNodeIndex++;
        Debug.Log("Saliendo de la tienda. Nodos completados en esta run: " + currentNodeIndex);
        
        // Notificar progresión (usamos currentNodeIndex como el avance actual)
        OnProgressionUpdate?.Invoke(currentNodeIndex, maxEnemiesPerRun);
        
        // Verificar si completó la run (llegó al final del recorrido)
        if (currentNodeIndex >= maxEnemiesPerRun)
        {
            Debug.Log("RUN COMPLETADA! Llegaste al final del calabozo.");
            int currentScore = playerManager != null ? playerManager.GetScore() : 0;
            EndRun(currentScore);
        }
        else
        {
            ContinueToNextCombat();
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
        RunSaveManager.ClearSavedRun();
    }

    public void EndRun(int finalScore)
    {
        if (!runInProgress)
        {
            Debug.LogWarning("No hay run en progreso para terminar");
            return;
        }

        Debug.Log("BossRushManager: Run terminada");
        Debug.Log("Nodos completados: " + currentNodeIndex);
        Debug.Log("Score final: " + finalScore);
        
        runInProgress = false;
        
        OnRunEnded?.Invoke(finalScore, currentNodeIndex);
        
        // Limpiar guardado
        RunSaveManager.ClearSavedRun();
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
    public void RestoreRunState(int savedNodeIndex)
    {
        Debug.Log("BossRushManager: Restaurando run - Nodo: " + savedNodeIndex);
        
        runInProgress = true;
        defaultMode = CombatMode.TraditionalRPG;
        currentNodeIndex = savedNodeIndex;
        
        OnRunStarted?.Invoke(defaultMode);
        OnProgressionUpdate?.Invoke(currentNodeIndex, maxEnemiesPerRun);
    }

    public void ClearCurrentRun()
    {
        runInProgress = false;
        currentNodeIndex = 0;
        totalTurnsUsed = 0;
        elitesDefeated = 0;
    }

    /// <summary>
    /// Calcula el multiplicador de stats según la fórmula logística del GDD:
    /// S(n) = 1 + L / (1 + e^(-k * (n - m)))
    /// Donde n = elites derrotados, L=1.8, k=0.32, m=2
    /// </summary>
    public float CalculateScalingMultiplier(int eliteCount)
    {
        float exponent = -scalingK * (eliteCount - scalingM);
        float denominator = 1f + Mathf.Exp(exponent);
        return 1f + scalingL / denominator;
    }

    // GETTERS
    public bool IsRunInProgress() => runInProgress;
    public int GetCurrentNodeIndex() => currentNodeIndex;
    public CombatMode GetCurrentMode() => defaultMode;
    public int GetMaxEnemies() => maxEnemiesPerRun;
    public float GetRunProgress() => (float)currentNodeIndex / maxEnemiesPerRun;
    public int GetElitesDefeated() => elitesDefeated;
    public float GetCurrentScalingMultiplier() => CalculateScalingMultiplier(elitesDefeated);
}

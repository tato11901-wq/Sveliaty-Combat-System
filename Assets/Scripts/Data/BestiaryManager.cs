using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestor del Bestiario
/// Guarda progreso del jugador con cada enemigo usando PlayerPrefs
/// Registra: encuentros, derrotas, tiers descubiertos, afinidades probadas
/// </summary>
public class BestiaryManager : MonoBehaviour
{
    public static BestiaryManager Instance { get; private set; }

    [Header("References")]
    public EnemyDatabase enemyDatabase;
    public CombatManager combatManager;

    // Estructura de datos por enemigo
    [System.Serializable]
    public class EnemyBestiaryData
    {
        public int enemyId;
        public bool hasEncountered;
        public bool hasDefeated;
        
        // Flags binarios para tiers (bit 0 = Tier1, bit 1 = Tier2, bit 2 = Tier3)
        public int tiersDiscovered;
        public int tiersDefeated;
        
        // Flags binarios para afinidades (bit 0 = Fuerza, bit 1 = Agilidad, bit 2 = Destreza)
        public int affinitiesDiscovered;

        public EnemyBestiaryData(int id)
        {
            enemyId = id;
            hasEncountered = false;
            hasDefeated = false;
            tiersDiscovered = 0;
            tiersDefeated = 0;
            affinitiesDiscovered = 0;
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Suscribirse a eventos de combate
        if (combatManager != null)
        {
            combatManager.OnCombatStart += RegisterEnemyEncounter;
            combatManager.OnCombatEnd += RegisterCombatResult;
        }
    }

    // ========== REGISTRO DE PROGRESO ==========

    /// <summary>
    /// Registra que el jugador se encontro con un enemigo
    /// </summary>
    void RegisterEnemyEncounter(EnemyInstance enemy)
    {
        int enemyId = enemy.enemyData.id;
        EnemyTier tier = enemy.enemyTierData.enemyTier;

        // Marcar como encontrado
        SetEnemyEncountered(enemyId, true);
        
        // Registrar tier descubierto
        RegisterTierDiscovered(enemyId, tier);

        Debug.Log("Bestiario: Enemigo " + enemy.enemyData.displayName + " (" + tier + ") registrado");
    }

    /// <summary>
    /// Registra el resultado del combate
    /// </summary>
    void RegisterCombatResult(bool victory, int score, AffinityType cardReward, int lifeLost)
    {
        if (!victory) return;

        // Obtener enemigo actual del CombatManager
        CombatManager combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null || !combatManager.HasActiveEnemy()) return;

        EnemyInstance enemy = combatManager.GetCurrentEnemy();
        int enemyId = enemy.enemyData.id;
        EnemyTier tier = enemy.enemyTierData.enemyTier;

        // Marcar como derrotado
        SetEnemyDefeated(enemyId, true);
        
        // Registrar tier derrotado
        RegisterTierDefeated(enemyId, tier);

        Debug.Log("Bestiario: Enemigo " + enemy.enemyData.displayName + " (" + tier + ") derrotado");
    }

    /// <summary>
    /// Registra que el jugador probo una afinidad contra un enemigo
    /// Llamar desde AffinityDiscoveryTracker cuando registra un descubrimiento
    /// </summary>
    public void RegisterAffinityDiscovered(int enemyId, AffinityType affinity)
    {
        int currentFlags = GetAffinitiesDiscovered(enemyId);
        int affinityBit = AffinityToFlag(affinity);
        
        // Añadir flag si no existe
        if ((currentFlags & affinityBit) == 0)
        {
            currentFlags |= affinityBit;
            SaveAffinitiesDiscovered(enemyId, currentFlags);
            Debug.Log("Bestiario: Afinidad " + affinity + " descubierta para enemigo " + enemyId);
        }
    }

    // ========== PLAYERPREFS - GETTERS ==========

    public bool HasEncountered(int enemyId)
    {
        return PlayerPrefs.GetInt("Bestiary_Encountered_" + enemyId, 0) == 1;
    }

    public bool HasDefeated(int enemyId)
    {
        return PlayerPrefs.GetInt("Bestiary_Defeated_" + enemyId, 0) == 1;
    }

    public int GetTiersDiscovered(int enemyId)
    {
        return PlayerPrefs.GetInt("Bestiary_TiersDiscovered_" + enemyId, 0);
    }

    public int GetTiersDefeated(int enemyId)
    {
        return PlayerPrefs.GetInt("Bestiary_TiersDefeated_" + enemyId, 0);
    }

    public int GetAffinitiesDiscovered(int enemyId)
    {
        return PlayerPrefs.GetInt("Bestiary_Affinities_" + enemyId, 0);
    }

    // ========== PLAYERPREFS - SETTERS ==========

    void SetEnemyEncountered(int enemyId, bool value)
    {
        PlayerPrefs.SetInt("Bestiary_Encountered_" + enemyId, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    void SetEnemyDefeated(int enemyId, bool value)
    {
        PlayerPrefs.SetInt("Bestiary_Defeated_" + enemyId, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    void RegisterTierDiscovered(int enemyId, EnemyTier tier)
    {
        int currentFlags = GetTiersDiscovered(enemyId);
        int tierBit = TierToFlag(tier);
        currentFlags |= tierBit;
        
        PlayerPrefs.SetInt("Bestiary_TiersDiscovered_" + enemyId, currentFlags);
        PlayerPrefs.Save();
    }

    void RegisterTierDefeated(int enemyId, EnemyTier tier)
    {
        int currentFlags = GetTiersDefeated(enemyId);
        int tierBit = TierToFlag(tier);
        currentFlags |= tierBit;
        
        PlayerPrefs.SetInt("Bestiary_TiersDefeated_" + enemyId, currentFlags);
        PlayerPrefs.Save();
    }

    void SaveAffinitiesDiscovered(int enemyId, int flags)
    {
        PlayerPrefs.SetInt("Bestiary_Affinities_" + enemyId, flags);
        PlayerPrefs.Save();
    }

    // ========== QUERIES ESPECIFICAS ==========

    /// <summary>
    /// Verifica si un tier especifico fue descubierto
    /// </summary>
    public bool IsTierDiscovered(int enemyId, EnemyTier tier)
    {
        int flags = GetTiersDiscovered(enemyId);
        int tierBit = TierToFlag(tier);
        return (flags & tierBit) != 0;
    }

    /// <summary>
    /// Verifica si un tier especifico fue derrotado
    /// </summary>
    public bool IsTierDefeated(int enemyId, EnemyTier tier)
    {
        int flags = GetTiersDefeated(enemyId);
        int tierBit = TierToFlag(tier);
        return (flags & tierBit) != 0;
    }

    /// <summary>
    /// Verifica si una afinidad fue descubierta
    /// </summary>
    public bool IsAffinityDiscovered(int enemyId, AffinityType affinity)
    {
        int flags = GetAffinitiesDiscovered(enemyId);
        int affinityBit = AffinityToFlag(affinity);
        return (flags & affinityBit) != 0;
    }

    /// <summary>
    /// Obtiene el multiplicador de afinidad si fue descubierto
    /// </summary>
    public AffinityMultiplier GetDiscoveredAffinityMultiplier(int enemyId, AffinityType affinity)
    {
        if (!IsAffinityDiscovered(enemyId, affinity))
            return AffinityMultiplier.Neutral; // No descubierto = desconocido

        // Buscar en la base de datos
        EnemyData enemy = enemyDatabase.GetEnemyById(enemyId);
        if (enemy == null) return AffinityMultiplier.Neutral;

        foreach (var relation in enemy.affinityRelations)
        {
            if (relation.type == affinity)
                return relation.multiplier;
        }

        return AffinityMultiplier.Neutral;
    }

    // ========== UTILIDADES ==========

    int TierToFlag(EnemyTier tier)
    {
        return tier switch
        {
            EnemyTier.Tier_1 => 1 << 0, // bit 0
            EnemyTier.Tier_2 => 1 << 1, // bit 1
            EnemyTier.Tier_3 => 1 << 2, // bit 2
            _ => 0
        };
    }

    int AffinityToFlag(AffinityType affinity)
    {
        return affinity switch
        {
            AffinityType.Fuerza => 1 << 0,   // bit 0
            AffinityType.Agilidad => 1 << 1,  // bit 1
            AffinityType.Destreza => 1 << 2,  // bit 2
            _ => 0
        };
    }

    /// <summary>
    /// Resetea todo el progreso del bestiario (para testing)
    /// </summary>
    public void ResetBestiary()
    {
        if (enemyDatabase == null) return;

        foreach (var enemy in enemyDatabase.allEnemies)
        {
            PlayerPrefs.DeleteKey("Bestiary_Encountered_" + enemy.id);
            PlayerPrefs.DeleteKey("Bestiary_Defeated_" + enemy.id);
            PlayerPrefs.DeleteKey("Bestiary_TiersDiscovered_" + enemy.id);
            PlayerPrefs.DeleteKey("Bestiary_TiersDefeated_" + enemy.id);
            PlayerPrefs.DeleteKey("Bestiary_Affinities_" + enemy.id);
        }

        PlayerPrefs.Save();
        Debug.Log("Bestiario reseteado");
    }

    /// <summary>
    /// Debug: Imprime el estado del bestiario
    /// </summary>
    public void DebugPrintBestiary()
    {
        Debug.Log("===== BESTIARIO =====");
        
        if (enemyDatabase == null) return;

        foreach (var enemy in enemyDatabase.allEnemies)
        {
            Debug.Log("Enemigo: " + enemy.displayName + " (ID: " + enemy.id + ")");
            Debug.Log("  Encontrado: " + HasEncountered(enemy.id));
            Debug.Log("  Derrotado: " + HasDefeated(enemy.id));
            Debug.Log("  Tiers descubiertos: " + GetTiersDiscovered(enemy.id));
            Debug.Log("  Tiers derrotados: " + GetTiersDefeated(enemy.id));
            Debug.Log("  Afinidades descubiertas: " + GetAffinitiesDiscovered(enemy.id));
        }
        
        Debug.Log("=====================");
    }
}
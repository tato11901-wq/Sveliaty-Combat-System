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
        public string enemyName;
        public bool hasEncountered;
        public bool hasDefeated;
        
        // Flags binarios para tiers (bit 0 = Tier1, bit 1 = Tier2, bit 2 = Tier3)
        public int tiersDiscovered;
        public int tiersDefeated;
        
        // Flags binarios para afinidades (bit 0 = Fuerza, bit 1 = Agilidad, bit 2 = Destreza)
        public int affinitiesDiscovered;

        public EnemyBestiaryData(string name)
        {
            enemyName = name;
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
        string enemyName = enemy.enemyData.name;
        EnemyTier tier = enemy.enemyTierData.enemyTier;

        // Marcar como encontrado
        SetEnemyEncountered(enemyName, true);
        
        // Registrar tier descubierto
        RegisterTierDiscovered(enemyName, tier);

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
        string enemyName = enemy.enemyData.name;
        EnemyTier tier = enemy.enemyTierData.enemyTier;

        // Marcar como derrotado
        SetEnemyDefeated(enemyName, true);
        
        // Registrar tier derrotado
        RegisterTierDefeated(enemyName, tier);

        Debug.Log("Bestiario: Enemigo " + enemy.enemyData.displayName + " (" + tier + ") derrotado");
    }

    /// <summary>
    /// Registra que el jugador probo una afinidad contra un enemigo
    /// Llamar desde AffinityDiscoveryTracker cuando registra un descubrimiento
    /// </summary>
    public void RegisterAffinityDiscovered(string enemyName, AffinityType affinity)
    {
        int currentFlags = GetAffinitiesDiscovered(enemyName);
        int affinityBit = AffinityToFlag(affinity);
        
        // Añadir flag si no existe
        if ((currentFlags & affinityBit) == 0)
        {
            currentFlags |= affinityBit;
            SaveAffinitiesDiscovered(enemyName, currentFlags);
            Debug.Log("Bestiario: Afinidad " + affinity + " descubierta para enemigo " + enemyName);
        }
    }

    // ========== PLAYERPREFS - GETTERS ==========

    public bool HasEncountered(string enemyName)
    {
        return PlayerPrefs.GetInt("Bestiary_Encountered_" + enemyName, 0) == 1;
    }

    public bool HasDefeated(string enemyName)
    {
        return PlayerPrefs.GetInt("Bestiary_Defeated_" + enemyName, 0) == 1;
    }

    public int GetTiersDiscovered(string enemyName)
    {
        return PlayerPrefs.GetInt("Bestiary_TiersDiscovered_" + enemyName, 0);
    }

    public int GetTiersDefeated(string enemyName)
    {
        return PlayerPrefs.GetInt("Bestiary_TiersDefeated_" + enemyName, 0);
    }

    public int GetAffinitiesDiscovered(string enemyName)
    {
        return PlayerPrefs.GetInt("Bestiary_Affinities_" + enemyName, 0);
    }

    // ========== PLAYERPREFS - SETTERS ==========

    void SetEnemyEncountered(string enemyName, bool value)
    {
        PlayerPrefs.SetInt("Bestiary_Encountered_" + enemyName, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    void SetEnemyDefeated(string enemyName, bool value)
    {
        PlayerPrefs.SetInt("Bestiary_Defeated_" + enemyName, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    void RegisterTierDiscovered(string enemyName, EnemyTier tier)
    {
        int currentFlags = GetTiersDiscovered(enemyName);
        int tierBit = TierToFlag(tier);
        currentFlags |= tierBit;
        
        PlayerPrefs.SetInt("Bestiary_TiersDiscovered_" + enemyName, currentFlags);
        PlayerPrefs.Save();
    }

    void RegisterTierDefeated(string enemyName, EnemyTier tier)
    {
        int currentFlags = GetTiersDefeated(enemyName);
        int tierBit = TierToFlag(tier);
        currentFlags |= tierBit;
        
        PlayerPrefs.SetInt("Bestiary_TiersDefeated_" + enemyName, currentFlags);
        PlayerPrefs.Save();
    }

    void SaveAffinitiesDiscovered(string enemyName, int flags)
    {
        PlayerPrefs.SetInt("Bestiary_Affinities_" + enemyName, flags);
        PlayerPrefs.Save();
    }

    // ========== QUERIES ESPECIFICAS ==========

    /// <summary>
    /// Verifica si un tier especifico fue descubierto
    /// </summary>
    public bool IsTierDiscovered(string enemyName, EnemyTier tier)
    {
        int flags = GetTiersDiscovered(enemyName);
        int tierBit = TierToFlag(tier);
        return (flags & tierBit) != 0;
    }

    /// <summary>
    /// Verifica si un tier especifico fue derrotado
    /// </summary>
    public bool IsTierDefeated(string enemyName, EnemyTier tier)
    {
        int flags = GetTiersDefeated(enemyName);
        int tierBit = TierToFlag(tier);
        return (flags & tierBit) != 0;
    }

    /// <summary>
    /// Verifica si una afinidad fue descubierta
    /// </summary>
    public bool IsAffinityDiscovered(string enemyName, AffinityType affinity)
    {
        int flags = GetAffinitiesDiscovered(enemyName);
        int affinityBit = AffinityToFlag(affinity);
        return (flags & affinityBit) != 0;
    }

    /// <summary>
    /// Obtiene el multiplicador de afinidad si fue descubierto
    /// </summary>
    public AffinityMultiplier GetDiscoveredAffinityMultiplier(string enemyName, AffinityType affinity)
    {
        if (!IsAffinityDiscovered(enemyName, affinity))
            return AffinityMultiplier.Neutral; // No descubierto = desconocido

        // Buscar en la base de datos
        EnemyData enemy = enemyDatabase.GetEnemyByName(enemyName);
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
            if (enemy == null) continue;
            PlayerPrefs.DeleteKey("Bestiary_Encountered_" + enemy.name);
            PlayerPrefs.DeleteKey("Bestiary_Defeated_" + enemy.name);
            PlayerPrefs.DeleteKey("Bestiary_TiersDiscovered_" + enemy.name);
            PlayerPrefs.DeleteKey("Bestiary_TiersDefeated_" + enemy.name);
            PlayerPrefs.DeleteKey("Bestiary_Affinities_" + enemy.name);
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
            if (enemy == null) continue;
            Debug.Log("Enemigo: " + enemy.displayName + " (Name: " + enemy.name + ")");
            Debug.Log("  Encontrado: " + HasEncountered(enemy.name));
            Debug.Log("  Derrotado: " + HasDefeated(enemy.name));
            Debug.Log("  Tiers descubiertos: " + GetTiersDiscovered(enemy.name));
            Debug.Log("  Tiers derrotados: " + GetTiersDefeated(enemy.name));
            Debug.Log("  Afinidades descubiertas: " + GetAffinitiesDiscovered(enemy.name));
        }
        
        Debug.Log("=====================");
    }
}
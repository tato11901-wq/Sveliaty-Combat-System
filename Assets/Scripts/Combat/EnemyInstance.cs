/// <summary>
/// Instancia en tiempo de ejecución de un enemigo.
/// Almacena el estado mutable del combate sin modificar los ScriptableObjects originales.
///
/// Soporta dos orígenes:
///   1. Enemigo normal: constructor EnemyInstance(EnemyData, EnemyTierData)
///   2. Enemigo Élite:  constructor EnemyInstance(EliteEnemyData, int eliteTier)
///      En el caso élite se sintetizan EnemyData/EnemyTierData en runtime para
///      que el resto del codebase los consuma sin cambios.
/// </summary>
public class EnemyInstance
{
    public EnemyData     enemyData;
    public EnemyTierData enemyTierData;

    // ── Élite ────────────────────────────────────────────────────────────────
    /// <summary>Referencia al SO original de élite (null para enemigos normales).</summary>
    public EliteEnemyData eliteEnemyData;

    /// <summary>Tier del élite (1-4). Solo significativo si IsElite == true.</summary>
    public int eliteTierLevel;

    /// <summary>True si este enemigo fue creado desde un EliteEnemyData.</summary>
    public bool IsElite => eliteEnemyData != null;

    // ── Variables de instancia ───────────────────────────────────────────────

    public int failureDamage;
    public int attemptsRemaining;

    // Sistema RPG
    public int currentRPGHealth;
    public int maxRPGHealth;
    public int currentRPGDiceCount;

    // Buffs activos
    public int   activeArmor        = 0;
    public float activeThorns       = 0f;
    public float activeSpeedEvasion = 0f;

    /// <summary>True si hay probabilidad de evasión activa (retrocompatibilidad).</summary>
    public bool hasSpeedEvasion => activeSpeedEvasion > 0f;

    // ────────────────────────────────────────────────────────────────────────
    // Constructor — Enemigo Normal
    // ────────────────────────────────────────────────────────────────────────

    public EnemyInstance(EnemyData data, EnemyTierData tierData)
    {
        enemyData     = data;
        enemyTierData = tierData;

        failureDamage      = tierData.failureDamage;
        attemptsRemaining  = tierData.maximunDiceThrow;
        currentRPGHealth   = tierData.RPGLife;
        maxRPGHealth       = tierData.RPGLife;
        currentRPGDiceCount= tierData.RPGDiceCount;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Constructor — Enemigo Élite
    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una instancia de élite escalando los stats base del SO según el tier (1-4).
    /// Sintetiza EnemyData y EnemyTierData en runtime para que el resto del código
    /// no requiera cambios (accede a .enemyData.displayName, .enemyTierData.sprite, etc.).
    /// </summary>
    public EnemyInstance(EliteEnemyData data, int eliteTier)
    {
        eliteEnemyData = data;
        eliteTierLevel = UnityEngine.Mathf.Clamp(eliteTier, 1, 4);

        // ── Calcular stats escalados ────────────────────────────────────────
        float lifeMult    = data.GetLifeMultiplier(eliteTierLevel);
        float dmgMult     = data.GetDamageMultiplier(eliteTierLevel);
        int   attempBonus = data.GetAttemptBonus(eliteTierLevel);

        int computedLife   = UnityEngine.Mathf.RoundToInt(data.baseRPGLife      * lifeMult);
        int computedDamage = UnityEngine.Mathf.RoundToInt(data.baseFailureDamage * dmgMult);

        // ── Sintetizar EnemyData ─────────────────────────────────────────────
        // (zero null-pointer changes en CombatManager/UI/Bestiary)
        var syntheticED = UnityEngine.ScriptableObject.CreateInstance<EnemyData>();
        syntheticED.name            = data.name;        // usado por Bestiary
        syntheticED.displayName     = data.displayName;
        syntheticED.affinityType    = data.affinityType;
        syntheticED.affinityRelations = data.affinityRelations;
        syntheticED.isSpirit        = true;             // los élites siempre activan eventos de maldición
        enemyData = syntheticED;

        // ── Sintetizar EnemyTierData ─────────────────────────────────────────
        var syntheticTD = UnityEngine.ScriptableObject.CreateInstance<EnemyTierData>();
        syntheticTD.enemyTier       = EnemyTier.Tier_3;    // máximo para cálculo de tinta/score
        syntheticTD.sprite          = data.sprite;
        syntheticTD.RPGLife         = data.baseRPGLife;    // base para cálculo de statsMultiplier
        syntheticTD.RPGDiceCount    = data.baseRPGDiceCount;
        syntheticTD.failureDamage   = computedDamage;
        syntheticTD.maximunDiceThrow= data.baseMaxAttempts + attempBonus;
        // Comportamiento por turno
        syntheticTD.healChance      = data.healChance;
        syntheticTD.armorChance     = data.armorChance;
        syntheticTD.thornsChance    = data.thornsChance;
        syntheticTD.speedChance     = data.speedChance;
        syntheticTD.doNothingChance = data.doNothingChance;
        syntheticTD.healAmount      = data.healAmount;
        syntheticTD.armorAmount     = data.armorAmount;
        syntheticTD.thornsAmount    = data.thornsAmount;
        syntheticTD.speedAmount     = data.speedAmount;
        enemyTierData = syntheticTD;

        // ── Asignar stats de instancia ───────────────────────────────────────
        failureDamage       = computedDamage;
        attemptsRemaining   = data.baseMaxAttempts + attempBonus;
        currentRPGHealth    = computedLife;
        maxRPGHealth        = computedLife;
        currentRPGDiceCount = data.baseRPGDiceCount;

        UnityEngine.Debug.Log($"[EnemyInstance] Élite creado: {data.displayName}  " +
                              $"Tier {eliteTierLevel}  HP={computedLife}  " +
                              $"Daño={computedDamage}  Intentos={attemptsRemaining}");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Métodos de utilidad
    // ────────────────────────────────────────────────────────────────────────

    public void ApplyStatsMultiplier(float multiplier, int extraAttempts = 0)
    {
        currentRPGHealth = UnityEngine.Mathf.RoundToInt(currentRPGHealth * multiplier);
        maxRPGHealth     = currentRPGHealth;
        failureDamage    = UnityEngine.Mathf.RoundToInt(failureDamage * multiplier);
        attemptsRemaining += extraAttempts;
    }

    /// <summary>Limpia los buffs que expiran al terminar la ronda.</summary>
    public void ClearTemporaryBuffs()
    {
        activeSpeedEvasion = 0f;
        activeArmor        = 0;
        activeThorns       = 0f;
    }
}
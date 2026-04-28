public class EnemyInstance
{
    public EnemyData enemyData; // Datos del enemigo

    public EnemyTierData enemyTierData; // Datos del tier del enemigo

    // Variables de instancia para no modificar el ScriptableObject original
    public int healthThreshold;
    public int diceCount;
    public int failureDamage;

    public int attemptsRemaining; // Intentos restantes para derrotar al enemigo
    public int currentRPGHealth; // Vida actual en modo RPG Tradicional
    public int currentRPGDiceCount; // Cantidad de dados actuales en modo RPG Tradicional

    // Buffs activos del enemigo
    public int activeArmor = 0; // Reduce daño recibido
    public float activeThorns = 0f; // Porcentaje de daño devuelto al jugador (0.0 a 1.0)
    public bool hasSpeedEvasion = false; // El próximo ataque del jugador puede fallar

    public EnemyInstance(EnemyData data, EnemyTierData tierData)
    {
        enemyData = data;
        enemyTierData = tierData;
        
        // Inicializar desde ScriptableObject a variables locales
        healthThreshold = tierData.healthThreshold;
        diceCount = tierData.diceCount;
        failureDamage = tierData.failureDamage;

        attemptsRemaining = tierData.maximunDiceThrow;
        currentRPGHealth = tierData.RPGLife;
        currentRPGDiceCount = tierData.RPGDiceCount;
    }

    // Método para multiplicar stats si es boss, en vez de alterar el ScriptableObject
    public void ApplyStatsMultiplier(float multiplier)
    {
        healthThreshold = UnityEngine.Mathf.RoundToInt(healthThreshold * multiplier);
        currentRPGHealth = UnityEngine.Mathf.RoundToInt(currentRPGHealth * multiplier);
    }

    /// <summary>
    /// Limpia los buffs que expiran al terminar la ronda (si aplica).
    /// </summary>
    public void ClearTemporaryBuffs()
    {
        hasSpeedEvasion = false;
        activeArmor = 0;
        activeThorns = 0f;
    }
}
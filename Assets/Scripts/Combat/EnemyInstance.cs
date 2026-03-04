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
        diceCount = UnityEngine.Mathf.RoundToInt(diceCount * multiplier);
        failureDamage = UnityEngine.Mathf.RoundToInt(failureDamage * multiplier);

        // Es importante multiplicar también la vida del modo RPG
        currentRPGHealth = UnityEngine.Mathf.RoundToInt(currentRPGHealth * multiplier);
        currentRPGDiceCount = UnityEngine.Mathf.RoundToInt(currentRPGDiceCount * multiplier);
    }
}
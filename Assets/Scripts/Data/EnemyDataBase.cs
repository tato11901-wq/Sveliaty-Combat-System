using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Sveliaty/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyData> allEnemies = new List<EnemyData>();

    /// <summary>
    /// Obtiene un enemigo aleatorio con un tier aleatorio
    /// </summary>
    public (EnemyData enemy, EnemyTier tier) GetRandomEnemy()
    {
        if (allEnemies == null || allEnemies.Count == 0)
        {
            Debug.LogError("EnemyDatabase esta vacio");
            return (null, EnemyTier.Tier_1);
        }

        // Seleccionar enemigo aleatorio
        EnemyData randomEnemy = allEnemies[Random.Range(0, allEnemies.Count)];

        // Seleccionar tier aleatorio de los disponibles
        if (randomEnemy.enemyTierData == null || randomEnemy.enemyTierData.Length == 0)
        {
            Debug.LogError("Enemigo " + randomEnemy.displayName + " no tiene tiers configurados");
            return (randomEnemy, EnemyTier.Tier_1);
        }

        EnemyTierData randomTierData = randomEnemy.enemyTierData[Random.Range(0, randomEnemy.enemyTierData.Length)];
        
        return (randomEnemy, randomTierData.enemyTier);
    }

    /// <summary>
    /// Busca un enemigo por su nombre (name del ScriptableObject)
    /// </summary>
    public EnemyData GetEnemyByName(string enemyName)
    {
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy.name == enemyName)
                return enemy;
        }

        Debug.LogWarning("Enemigo con nombre " + enemyName + " no encontrado");
        return null;
    }

    /// <summary>
    /// Obtiene todos los enemigos
    /// </summary>
    public List<EnemyData> GetAllEnemies()
    {
        return allEnemies;
    }
}
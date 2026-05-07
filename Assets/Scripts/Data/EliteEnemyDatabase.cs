using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base de datos de todos los enemigos de Élite disponibles en la run.
/// Se asigna en el Inspector del BossRushManager.
/// </summary>
[CreateAssetMenu(menuName = "Sveliaty/Enemy/Elite Enemy Database")]
public class EliteEnemyDatabase : ScriptableObject
{
    [Tooltip("Lista de todos los élites posibles. Se selecciona uno al azar al llegar a un nodo de élite.")]
    public List<EliteEnemyData> allElites = new List<EliteEnemyData>();

    /// <summary>
    /// Devuelve un élite aleatorio de la base de datos, o null si está vacía.
    /// </summary>
    public EliteEnemyData GetRandom()
    {
        if (allElites == null || allElites.Count == 0)
        {
            Debug.LogWarning("[EliteEnemyDatabase] La base de datos de élites está vacía.");
            return null;
        }
        return allElites[Random.Range(0, allElites.Count)];
    }
}

using System.Collections.Generic;

/// <summary>
/// Sistema estatico para trackear que afinidades ha probado el jugador contra cada enemigo
/// Ahora tambien registra en el BestiaryManager para persistencia
/// </summary>
public static class AffinityDiscoveryTracker
{
    // Diccionario: EnemyID -> HashSet de afinidades descubiertas
    private static Dictionary<int, HashSet<AffinityType>> discoveries = new Dictionary<int, HashSet<AffinityType>>();

    /// <summary>
    /// Registra que el jugador uso una afinidad contra un enemigo
    /// </summary>
    public static void RegisterDiscovery(int enemyId, AffinityType affinity)
    {
        if (!discoveries.ContainsKey(enemyId))
        {
            discoveries[enemyId] = new HashSet<AffinityType>();
        }

        // Añadir al tracking en memoria
        bool wasNew = discoveries[enemyId].Add(affinity);

        // NUEVO: Registrar en BestiaryManager para persistencia
        if (wasNew && BestiaryManager.Instance != null)
        {
            BestiaryManager.Instance.RegisterAffinityDiscovered(enemyId, affinity);
        }
    }

    /// <summary>
    /// Verifica si una afinidad fue descubierta contra un enemigo
    /// </summary>
    public static bool IsDiscovered(int enemyId, AffinityType affinity)
    {
        if (!discoveries.ContainsKey(enemyId))
            return false;

        return discoveries[enemyId].Contains(affinity);
    }

}
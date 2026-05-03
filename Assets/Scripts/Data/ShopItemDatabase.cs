using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base de datos de todos los ShopItemData disponibles en la run.
/// Centraliza el pool de ítems de la tienda como un asset editable desde el Editor.
/// </summary>
[CreateAssetMenu(fileName = "ShopItemDatabase", menuName = "Sveliaty/Shop/Shop Item Database")]
public class ShopItemDatabase : ScriptableObject
{
    [Tooltip("Todos los ítems de estadísticas disponibles para la tienda.")]
    public List<ShopItemData> allItems = new List<ShopItemData>();

    /// <summary>
    /// Devuelve los ítems elegibles para una visita concreta según su minShopVisit.
    /// </summary>
    public List<ShopItemData> GetEligibleItems(int currentVisit)
    {
        var result = new List<ShopItemData>();
        foreach (var item in allItems)
        {
            if (item != null && item.minShopVisit <= currentVisit)
                result.Add(item);
        }
        return result;
    }

    /// <summary>
    /// Selección aleatoria ponderada por appearanceWeight entre los ítems elegibles.
    /// Devuelve null si el pool está vacío.
    /// </summary>
    public ShopItemData PickWeighted(int currentVisit)
    {
        var eligible = GetEligibleItems(currentVisit);
        if (eligible.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var item in eligible) totalWeight += item.appearanceWeight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var item in eligible)
        {
            cumulative += item.appearanceWeight;
            if (roll <= cumulative) return item;
        }

        return eligible[eligible.Count - 1];
    }
}

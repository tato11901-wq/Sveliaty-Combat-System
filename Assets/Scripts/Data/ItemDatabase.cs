using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewItemDatabase", menuName = "Sveliaty/Data Base/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems;

    /// <summary>
    /// Obtiene un ítem aleatorio de la base de datos
    /// </summary>
    public ItemData GetRandomItem()
    {
        if (allItems == null || allItems.Count == 0) return null;
        return allItems[Random.Range(0, allItems.Count)];
    }

    /// <summary>
    /// Devuelve 3 ítems aleatorios sin repetir (ideal para la tienda)
    /// </summary>
    public List<ItemData> GetThreeRandomItems()
    {
        if (allItems == null || allItems.Count < 3) 
        {
            Debug.LogWarning("ItemDatabase: No hay suficientes ítems para devolver 3.");
            return new List<ItemData>(allItems); // Devuelve lo que haya
        }

        List<ItemData> tempItems = new List<ItemData>(allItems);
        List<ItemData> result = new List<ItemData>();

        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, tempItems.Count);
            result.Add(tempItems[randomIndex]);
            tempItems.RemoveAt(randomIndex); // Evitar duplicados
        }

        return result;
    }
}

using UnityEngine;
using System;

/// <summary>
/// Gestiona la adquisición de ítems y la aplicación de sus efectos permanentes al jugador.
/// </summary>
public class ItemManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private PlayerStatsManager playerStatsManager;

    // Evento para cuando se obtiene un ítem (útil para UI)
    public event Action<ItemData> OnItemObtained;

    /// <summary>
    /// Otorga un ítem específico al jugador y aplica sus estadísticas.
    /// </summary>
    public void GiveItemToPlayer(ItemData item, PlayerData playerData)
    {
        if (item == null || playerData == null) return;

        // Añadir a la base de datos del jugador
        playerData.obtainedItems.Add(item);

        // Notificar al gestor de estadísticas para que aplique el nuevo modificador permanente
        if (playerStatsManager != null)
        {
            playerStatsManager.ApplyItemModifiers(item);
        }

        Debug.Log($"Ítem obtenido: {item.itemName}");
        OnItemObtained?.Invoke(item);
    }

    /// <summary>
    /// Remueve un ítem del jugador (opcional, si hay mecánicas de perder ítems)
    /// </summary>
    public void RemoveItemFromPlayer(ItemData item, PlayerData playerData)
    {
        if (item == null || playerData == null) return;

        if (playerData.obtainedItems.Contains(item))
        {
            playerData.obtainedItems.Remove(item);

            if (playerStatsManager != null)
            {
                playerStatsManager.RemoveItemModifiers(item);
            }

            Debug.Log($"Ítem perdido: {item.itemName}");
        }
    }

#if UNITY_EDITOR
    // ==========================================
    // DEBUG: Otorgar ítem aleatorio para probar
    // ==========================================
    public void DebugGiveRandomItem(PlayerData playerData)
    {
        if (itemDatabase == null)
        {
            Debug.LogError("ItemDatabase no asignada en ItemManager.");
            return;
        }

        ItemData randomItem = itemDatabase.GetRandomItem();
        if (randomItem != null)
        {
            GiveItemToPlayer(randomItem, playerData);
        }
    }
#endif
}

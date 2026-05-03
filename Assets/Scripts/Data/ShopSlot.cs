using UnityEngine;

/// <summary>
/// Representa un slot generado de la tienda listo para mostrar en UI.
/// Puede ser un ítem de estadísticas o una habilidad activa.
/// </summary>
public class ShopSlot
{
    public enum SlotType { Item, Ability }

    public SlotType Type { get; private set; }

    // ── Ítem ──
    public ShopItemData Item { get; private set; }
    public int InkPrice   { get; private set; }

    // ── Habilidad ──
    public AbilityData Ability     { get; private set; }
    public int         AbilityTier { get; private set; }  // tier actual del jugador + 1
    public int         CardPrice   { get; private set; }

    public bool IsPurchased { get; private set; }

    // Constructor para ítem
    public ShopSlot(ShopItemData item, int inkPrice)
    {
        Type      = SlotType.Item;
        Item      = item;
        InkPrice  = inkPrice;
    }

    // Constructor para habilidad
    public ShopSlot(AbilityData ability, int targetTier, int cardPrice)
    {
        Type        = SlotType.Ability;
        Ability     = ability;
        AbilityTier = targetTier;
        CardPrice   = cardPrice;
    }

    public void MarkPurchased() => IsPurchased = true;
}

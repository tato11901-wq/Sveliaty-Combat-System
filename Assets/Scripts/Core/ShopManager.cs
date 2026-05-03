using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gestor de la lógica de negocio de la tienda.
/// Responsabilidades:
///  - Generar los slots respetando las reglas del GDD (límite por rama, habilidades válidas).
///  - Manejar compras con Tinta o Cartas.
///  - Manejar rerolls con coste escalable.
///  - Exponer eventos para que la UI reaccione sin acoplamientos directos.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // ── Referencia a configuración ──────────────────────────────────────────
    [Header("Configuración")]
    public ShopSettings      settings;
    public ShopItemDatabase  itemDatabase;    // Pool de ítems (ScriptableObject)
    public AbilityDatabase   abilityDatabase;

    [Header("Referencias Core")]
    public PlayerManager     playerManager;
    public AbilityManager    abilityManager;
    public BossRushManager   bossRushManager; // Para suscribirse a OnShopReached

    // ── Estado de la visita actual ──────────────────────────────────────────
    private List<ShopSlot> currentSlots = new List<ShopSlot>();
    private int visitCount        = 0;   // cuántas veces se abrió la tienda en esta run
    private int rerollsThisVisit  = 0;
    private int slotsBoughtThisVisit = 0;

    // ── Eventos para la UI ──────────────────────────────────────────────────
    /// <summary>Se dispara cuando los slots se regeneran (inicio de visita o reroll).</summary>
    public event Action<List<ShopSlot>> OnSlotsGenerated;

    /// <summary>Slot comprado con éxito.</summary>
    public event Action<ShopSlot> OnSlotPurchased;

    /// <summary>Intento de compra fallido (recursos insuficientes o regla GDD).</summary>
    public event Action<string> OnPurchaseFailed;

    /// <summary>Coste actual del próximo reroll.</summary>
    public event Action<int> OnRerollCostUpdated;

    // ───────────────────────────────────────────────────────────────────────
    // CICLO DE VIDA — conexión por evento
    // ───────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (bossRushManager != null)
            bossRushManager.OnShopReached += OpenShop;
    }

    private void OnDisable()
    {
        if (bossRushManager != null)
            bossRushManager.OnShopReached -= OpenShop;
    }

    // ───────────────────────────────────────────────────────────────────────
    // PUNTO DE ENTRADA: abrir la tienda
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Llamado automáticamente cuando BossRushManager dispara OnShopReached.
    /// También puede invocarse manualmente desde el Inspector o desde pruebas.
    /// </summary>
    public void OpenShop()
    {
        visitCount++;
        rerollsThisVisit     = 0;
        slotsBoughtThisVisit = 0;
        GenerateSlots();
        OnRerollCostUpdated?.Invoke(GetCurrentRerollCost());
    }

    // ───────────────────────────────────────────────────────────────────────
    // GENERACIÓN DE SLOTS
    // ───────────────────────────────────────────────────────────────────────

    private void GenerateSlots()
    {
        currentSlots.Clear();

        for (int i = 0; i < settings.slotCount; i++)
        {
            ShopSlot slot = TryGenerateAbilitySlot() ?? GenerateItemSlot();
            if (slot != null)
                currentSlots.Add(slot);
        }

        OnSlotsGenerated?.Invoke(new List<ShopSlot>(currentSlots));
    }

    /// <summary>
    /// Intenta generar un slot de habilidad según la probabilidad y las reglas GDD.
    /// Devuelve null si no corresponde o no hay habilidades válidas disponibles.
    /// </summary>
    private ShopSlot TryGenerateAbilitySlot()
    {
        if (UnityEngine.Random.value > settings.abilitySlotChance) return null;

        // Recolectar candidatas válidas
        List<(AbilityData ability, int targetTier)> candidates = GetValidAbilityCandidates();
        if (candidates.Count == 0) return null;

        // Elegir una al azar (pesos uniformes — se puede extender con weights en AbilityData)
        var chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        int cardCost = CalculateAbilityCardCost(chosen.targetTier);

        return new ShopSlot(chosen.ability, chosen.targetTier, cardCost);
    }

    /// <summary>
    /// Devuelve las habilidades que el jugador puede comprar en este momento:
    ///  - Si la rama tiene < maxAbilitiesPerBranch → habilidades nuevas de esa rama.
    ///  - Si la rama tiene slots llenos → solo habilidades ya poseídas (para subir Tier).
    /// Nunca incluye habilidades que ya están en Tier 3.
    /// </summary>
    private List<(AbilityData, int)> GetValidAbilityCandidates()
    {
        var result = new List<(AbilityData, int)>();
        if (abilityDatabase == null || abilityManager == null) return result;

        foreach (AffinityType branch in Enum.GetValues(typeof(AffinityType)))
        {
            int slotsUsed   = abilityManager.GetAbilityCountInBranch(branch);
            int currentTier = 0; // para habilidades nuevas el tier destino es 1

            List<AbilityData> branchAbilities = abilityDatabase.GetAbilitiesByAffinity(branch);

            foreach (AbilityData ability in branchAbilities)
            {
                int ownedTier = abilityManager.GetAbilityTier(ability);

                if (ownedTier == 0)
                {
                    // Habilidad NO poseída: solo válida si la rama tiene slot libre
                    if (slotsUsed < settings.maxAbilitiesPerBranch)
                        result.Add((ability, 1));
                }
                else if (ownedTier < 3)
                {
                    // Habilidad poseída con Tier < 3: válida para mejora aunque la rama esté llena
                    result.Add((ability, ownedTier + 1));
                }
                // ownedTier == 3 → ya está al máximo, no se incluye
            }
        }

        return result;
    }

    private ShopSlot GenerateItemSlot()
    {
        ShopItemData item = PickWeightedItem();
        if (item == null) return null;

        int price = item.GetScaledPrice(visitCount, settings.inkPriceScalePerVisit);
        return new ShopSlot(item, price);
    }

    /// <summary>Selección aleatoria ponderada delegada a la ShopItemDatabase.</summary>
    private ShopItemData PickWeightedItem()
    {
        if (itemDatabase == null)
        {
            Debug.LogWarning("ShopManager: itemDatabase no asignado.");
            return null;
        }
        return itemDatabase.PickWeighted(visitCount);
    }

    // ───────────────────────────────────────────────────────────────────────
    // COMPRA
    // ───────────────────────────────────────────────────────────────────────

    public void TryPurchase(ShopSlot slot)
    {
        if (slot == null || slot.IsPurchased)
        {
            OnPurchaseFailed?.Invoke("Este slot ya fue comprado.");
            return;
        }

        if (slot.Type == ShopSlot.SlotType.Item)
            TryPurchaseItem(slot);
        else
            TryPurchaseAbility(slot);
    }

    private void TryPurchaseItem(ShopSlot slot)
    {
        if (playerManager.GetInk() < slot.InkPrice)
        {
            OnPurchaseFailed?.Invoke($"No tienes suficiente Tinta. Necesitas {slot.InkPrice}.");
            return;
        }

        playerManager.SpendInk(slot.InkPrice);
        ApplyItemStats(slot.Item);
        slot.MarkPurchased();
        slotsBoughtThisVisit++;
        CheckFreeReroll();
        OnSlotPurchased?.Invoke(slot);
    }

    private void TryPurchaseAbility(ShopSlot slot)
    {
        AffinityType branch = slot.Ability.affinityType;
        int cardCost = slot.CardPrice;

        if (!playerManager.HasCards(branch, cardCost))
        {
            OnPurchaseFailed?.Invoke(
                $"No tienes suficientes cartas de {branch}. Necesitas {cardCost}.");
            return;
        }

        // Validar nuevamente que no se supere el límite por rama (protección doble)
        int ownedTier = abilityManager.GetAbilityTier(slot.Ability);
        if (ownedTier == 0 && abilityManager.GetAbilityCountInBranch(branch) >= settings.maxAbilitiesPerBranch)
        {
            OnPurchaseFailed?.Invoke($"Ya tienes el máximo de habilidades en la rama {branch}.");
            return;
        }

        playerManager.RemoveCards(branch, cardCost);
        abilityManager.UnlockOrUpgradeAbility(slot.Ability);
        slot.MarkPurchased();
        slotsBoughtThisVisit++;
        CheckFreeReroll();
        OnSlotPurchased?.Invoke(slot);
    }

    private void ApplyItemStats(ShopItemData item)
    {
        // Aplicar usando PlayerStatsManager si está disponible, o fallback a PlayerManager
        var statsManager = FindObjectOfType<PlayerStatsManager>();
        if (statsManager != null)
        {
            // Convertir deltas a StatModifiers permanentes (fuente = el ítem)
            // Se crean ítems puente para reutilizar el pipeline existente
            var tempItem = ScriptableObject.CreateInstance<ItemData>();
            tempItem.bonusFuerza      = item.deltaFuerza;
            tempItem.bonusVelocidad   = item.deltaVelocidad;
            tempItem.bonusDestreza    = item.deltaDestreza;
            tempItem.bonusArmadura    = item.deltaArmadura;
            tempItem.bonusProbCritico = item.deltaProbCritico;
            tempItem.bonusRoboVida    = item.deltaRoboVida;
            statsManager.ApplyItemModifiers(tempItem);
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // REROLL
    // ───────────────────────────────────────────────────────────────────────

    public void TryReroll()
    {
        int cost = GetCurrentRerollCost();

        if (playerManager.GetInk() < cost)
        {
            OnPurchaseFailed?.Invoke($"No tienes suficiente Tinta para el reroll. Necesitas {cost}.");
            return;
        }

        playerManager.SpendInk(cost);
        rerollsThisVisit++;
        OnRerollCostUpdated?.Invoke(GetCurrentRerollCost());
        GenerateSlots();
    }

    /// <summary>Si se compraron los 3 slots, el siguiente reroll es gratuito.</summary>
    private void CheckFreeReroll()
    {
        if (slotsBoughtThisVisit >= settings.slotCount)
        {
            // Reroll gratuito: marcamos que el próximo reroll no cuesta nada
            // Lo hacemos decrementando rerollsThisVisit si está en 0 (truco seguro)
            if (rerollsThisVisit > 0) rerollsThisVisit--;
            OnRerollCostUpdated?.Invoke(0); // Notificar que es gratis
        }
    }

    public int GetCurrentRerollCost()
    {
        return settings.rerollBaseInkCost + rerollsThisVisit * settings.rerollCostIncrement;
    }

    // ───────────────────────────────────────────────────────────────────────
    // PRECIOS
    // ───────────────────────────────────────────────────────────────────────

    private int CalculateAbilityCardCost(int targetTier)
    {
        int baseCost = targetTier switch
        {
            1 => settings.abilityBaseCostTier1,
            2 => settings.abilityBaseCostTier2,
            3 => settings.abilityBaseCostTier3,
            _ => settings.abilityBaseCostTier1
        };
        return baseCost + (visitCount - 1) * settings.abilityCardCostScalePerVisit;
    }

    // ───────────────────────────────────────────────────────────────────────
    // GETTERS
    // ───────────────────────────────────────────────────────────────────────

    public List<ShopSlot> GetCurrentSlots()      => new List<ShopSlot>(currentSlots);
    public int             GetVisitCount()         => visitCount;
    public int             GetRerollsThisVisit()   => rerollsThisVisit;
}

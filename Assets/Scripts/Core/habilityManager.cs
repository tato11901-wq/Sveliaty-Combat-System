using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AbilityManager : MonoBehaviour
{
    [Header("References")]
    public AbilityDatabase abilityDatabase;
    public PlayerManager playerManager;
    
    // Habilidades desbloqueadas y sus Tiers (id -> tier actual, 0 = no poseída)
    private Dictionary<int, int> abilityTiers = new Dictionary<int, int>();
    
    // Sistema de gasto de cartas (configurable)
    public enum CardSpendingMode 
    { 
        Absolute,      // Opcion 1: Gasto permanente
        PerInstance,   // Opcion 2: Recupera al ganar
        Relative       // Opcion 3: Cuenta el maximo historico
    }
    public CardSpendingMode spendingMode = CardSpendingMode.PerInstance;
    
    // Para Opcion 2 y 3
    private Dictionary<AffinityType, int> cardsSpentThisCombat = new Dictionary<AffinityType, int>();
    private Dictionary<AffinityType, int> maxCardsEverHad = new Dictionary<AffinityType, int>(); // Opcion 3
    
    void Start()
    {
        InitializeAbilities();
    }
    
    void InitializeAbilities()
    {
        abilityTiers.Clear();

        // Desbloquear habilidades básicas en Tier 1
        foreach (var ability in abilityDatabase.allAbilities)
        {
            if (ability.isBasicAbility)
                abilityTiers[ability.id] = 1;
        }
        
        // Inicializar tracking
        cardsSpentThisCombat[AffinityType.Fuerza] = 0;
        cardsSpentThisCombat[AffinityType.Agilidad] = 0;
        cardsSpentThisCombat[AffinityType.Destreza] = 0;
        
        UpdateMaxCards();
    }
    
    public List<AbilityData> GetAvailableAbilities(AffinityType type)
    {
        return abilityDatabase.GetAbilitiesByAffinity(type)
            .Where(a => abilityTiers.ContainsKey(a.id) && abilityTiers[a.id] > 0)
            .ToList();
    }
    
    public bool CanUseAbility(AbilityData ability, int currentHealth, int remainingTurns)
    {
        // Verificar vida suficiente
        if (ability.healthCost > currentHealth) return false;
        
        // Verificar turnos suficientes
        if (ability.turnCost > remainingTurns) return false;
        
        // Verificar cartas suficientes
        if (ability.cardCost > playerManager.GetCards(ability.affinityType)) return false;
        
        return true;
    }
    
    public void SpendCards(AffinityType type, int amount)
    {
        playerManager.RemoveCards(type, amount);
        cardsSpentThisCombat[type] += amount;
        
        // Actualizar maximo historico (Opcion 3)
        UpdateMaxCards();
    }
    
    public void OnCombatWon()
    {
        if (spendingMode == CardSpendingMode.PerInstance)
        {
            // Opcion 2: Recuperar cartas gastadas
            playerManager.AddCards(AffinityType.Fuerza, cardsSpentThisCombat[AffinityType.Fuerza]);
            playerManager.AddCards(AffinityType.Agilidad, cardsSpentThisCombat[AffinityType.Agilidad]);
            playerManager.AddCards(AffinityType.Destreza, cardsSpentThisCombat[AffinityType.Destreza]);
        }
        
        // Resetear contador de gasto
        cardsSpentThisCombat[AffinityType.Fuerza] = 0;
        cardsSpentThisCombat[AffinityType.Agilidad] = 0;
        cardsSpentThisCombat[AffinityType.Destreza] = 0;
    }
    
    public void CheckUnlocks()
    {
        // Las habilidades ahora se desbloquean solo desde la tienda.
        // Este método se mantiene por retrocompatibilidad pero no hace nada.
    }

    // ── Métodos para la tienda ─────────────────────────────────────────────

    /// <summary>
    /// Desbloquea una habilidad nueva (Tier 1) o sube su Tier si ya está poseída.
    /// Llamado por ShopManager tras confirmar la compra.
    /// </summary>
    public void UnlockOrUpgradeAbility(AbilityData ability)
    {
        if (!abilityTiers.ContainsKey(ability.id) || abilityTiers[ability.id] == 0)
        {
            abilityTiers[ability.id] = 1;
            Debug.Log($"Habilidad desbloqueada: {ability.abilityName} (Tier 1)");
        }
        else if (abilityTiers[ability.id] < 3)
        {
            abilityTiers[ability.id]++;
            Debug.Log($"Habilidad mejorada: {ability.abilityName} → Tier {abilityTiers[ability.id]}");
        }
        else
        {
            Debug.LogWarning($"{ability.abilityName} ya está en Tier máximo.");
        }
    }

    /// <summary>Devuelve el Tier actual de la habilidad (0 = no poseída).</summary>
    public int GetAbilityTier(AbilityData ability)
    {
        if (ability == null) return 0;
        return abilityTiers.TryGetValue(ability.id, out int tier) ? tier : 0;
    }

    /// <summary>Cantidad de habilidades no-básicas poseídas en la rama (para límite GDD).</summary>
    public int GetAbilityCountInBranch(AffinityType branch)
    {
        int count = 0;
        foreach (var ability in abilityDatabase.GetAbilitiesByAffinity(branch))
        {
            if (!ability.isBasicAbility && abilityTiers.TryGetValue(ability.id, out int t) && t > 0)
                count++;
        }
        return count;
    }
    
    void UpdateMaxCards()
    {
        foreach (AffinityType type in System.Enum.GetValues(typeof(AffinityType)))
        {
            int current = playerManager.GetCards(type);
            if (!maxCardsEverHad.ContainsKey(type) || current > maxCardsEverHad[type])
            {
                maxCardsEverHad[type] = current;
            }
        }
    }
    
    public int GetFinalCardCount(AffinityType type)
    {
        return spendingMode switch
        {
            CardSpendingMode.Relative => maxCardsEverHad[type],
            _ => playerManager.GetCards(type)
        };
    }
}
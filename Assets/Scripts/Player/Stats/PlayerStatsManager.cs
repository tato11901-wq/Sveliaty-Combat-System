using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor centralizado del cálculo de estadísticas.
/// Aplica los ítems como modificadores persistentes y calcula el aporte dinámico de las cartas en tiempo de ejecución.
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    [Header("Configuración de Ratio")]
    [Tooltip("Puntos de estadística otorgados por cada 1 carta de la rama correspondiente.")]
    public float cardStatRatio = 1.0f;

    // Diccionario para acceder rápidamente a la clase CharacterStat de cada StatType
    private Dictionary<StatType, CharacterStat> stats;

    [Header("Debug Inspector")]
    [SerializeField] private PlayerData _playerDataRef;

    private void Awake()
    {
        InitializeStats();
    }

    /// <summary>
    /// Crea las instancias base para cada estadística
    /// </summary>
    private void InitializeStats()
    {
        stats = new Dictionary<StatType, CharacterStat>
        {
            { StatType.Fuerza, new CharacterStat(10f) },      // Valores base iniciales, pueden ser parametrizados
            { StatType.Velocidad, new CharacterStat(10f) },
            { StatType.Destreza, new CharacterStat(10f) },
            { StatType.Armadura, new CharacterStat(0f) },
            { StatType.ProbCritico, new CharacterStat(5f) },
            { StatType.RoboVida, new CharacterStat(0f) }
        };
    }

    /// <summary>
    /// Inicializa el sistema inyectando el PlayerData.
    /// Limpia modificadores anteriores y aplica los persistentes (ítems).
    /// </summary>
    public void Initialize(PlayerData playerData)
    {
        _playerDataRef = playerData;
        
        if (stats == null) InitializeStats();

        // Limpiar modificadores persistentes previos (por si se reinicia la run)
        foreach (var statKvp in stats)
        {
            statKvp.Value.RemoveAllModifiersFromSource(this);
        }

        // Aplicar Modificadores Persistentes provenientes de Ítems
        if (_playerDataRef.obtainedItems != null)
        {
            foreach (ItemData item in _playerDataRef.obtainedItems)
            {
                ApplyItemModifiers(item);
            }
        }
    }

    /// <summary>
    /// Aplica los modificadores de un ítem a las CharacterStats usando el ítem como Source
    /// </summary>
    public void ApplyItemModifiers(ItemData item)
    {
        if (item.bonusFuerza != 0) stats[StatType.Fuerza].AddModifier(new StatModifier(item.bonusFuerza, StatModType.Flat, item));
        if (item.bonusVelocidad != 0) stats[StatType.Velocidad].AddModifier(new StatModifier(item.bonusVelocidad, StatModType.Flat, item));
        if (item.bonusDestreza != 0) stats[StatType.Destreza].AddModifier(new StatModifier(item.bonusDestreza, StatModType.Flat, item));
        
        if (item.bonusArmadura != 0) stats[StatType.Armadura].AddModifier(new StatModifier(item.bonusArmadura, StatModType.Flat, item));
        if (item.bonusProbCritico != 0) stats[StatType.ProbCritico].AddModifier(new StatModifier(item.bonusProbCritico, StatModType.Flat, item));
        if (item.bonusRoboVida != 0) stats[StatType.RoboVida].AddModifier(new StatModifier(item.bonusRoboVida, StatModType.Flat, item));
    }

    /// <summary>
    /// Remueve los modificadores asociados a un ítem en particular
    /// </summary>
    public void RemoveItemModifiers(ItemData item)
    {
        foreach (var statKvp in stats)
        {
            statKvp.Value.RemoveAllModifiersFromSource(item);
        }
    }

    /// <summary>
    /// Obtiene el valor final de la estadística, calculando en el orden estricto:
    /// 1. Base + Ítems (Persistente cacheado)
    /// 2. Cartas (Aditivo Dinámico)
    /// 3. Modificadores de Contexto (Maldiciones, Pasivas de la acción)
    /// </summary>
    /// <param name="statType">La estadística solicitada</param>
    /// <param name="context">Opcional: el contexto del combate actual que contiene cartas y modificadores temporales de la acción</param>
    /// <returns>Valor total calculado</returns>
    public float GetFinalStat(StatType statType, CombatContext context = null)
    {
        if (!stats.ContainsKey(statType)) return 0f;

        // --- ORDEN 1: BASE + ITEMS ---
        // Se lee directamente del CharacterStat (cacheado internamente por isDirty).
        // Contiene el BaseValue inicial + Modificadores Flat, PercentAdd y PercentMult de los ítems.
        float finalValue = stats[statType].Value;

        // --- ORDEN 2: CARTAS ---
        float cardsBonus = 0f;
        int cardsAmount = 0;

        // Priorizamos el contexto si trae cartas específicas para esta acción, sino usamos las globales
        Dictionary<AffinityType, int> cardsSource = (context != null && context.currentCards != null && context.currentCards.Count > 0) 
            ? context.currentCards 
            : (_playerDataRef != null ? _playerDataRef.cardsPerBranch : null);

        if (cardsSource != null)
        {
            if (statType == StatType.Fuerza && cardsSource.ContainsKey(AffinityType.Fuerza))
                cardsAmount = cardsSource[AffinityType.Fuerza];
            else if (statType == StatType.Velocidad && cardsSource.ContainsKey(AffinityType.Agilidad))
                cardsAmount = cardsSource[AffinityType.Agilidad];
            else if (statType == StatType.Destreza && cardsSource.ContainsKey(AffinityType.Destreza))
                cardsAmount = cardsSource[AffinityType.Destreza];
            
            // Las cartas aportan de forma aditiva según el ratio (por defecto 1 carta = 1 punto)
            cardsBonus = cardsAmount * cardStatRatio;
            finalValue += cardsBonus;
        }

        // --- ORDEN 3: CONTEXTO (Maldiciones, Pasivas temporales, etc.) ---
        if (context != null && context.actionModifiers != null && context.actionModifiers.Count > 0)
        {
            // Para asegurar el orden matemático correcto (Flat -> PercentAdd -> PercentMult)
            // hacemos 3 pasadas sobre la lista temporal, aplicando sobre el valor acumulado (Base + Items + Cartas).
            
            // Pasada 3.1: Flat (sumas directas)
            foreach (var mod in context.actionModifiers)
            {
                if (mod.Type == StatModType.Flat) 
                    finalValue += mod.Value;
            }
            
            // Pasada 3.2: PercentAdd (sumamos porcentajes primero para que actúen juntos)
            float sumPercentAdd = 0;
            foreach (var mod in context.actionModifiers)
            {
                if (mod.Type == StatModType.PercentAdd) 
                    sumPercentAdd += mod.Value;
            }
            if (sumPercentAdd != 0) 
                finalValue *= (1 + sumPercentAdd);
            
            // Pasada 3.3: PercentMult (multiplicadores exponenciales independientes)
            foreach (var mod in context.actionModifiers)
            {
                if (mod.Type == StatModType.PercentMult) 
                    finalValue *= (1 + mod.Value);
            }
        }

        return (float)System.Math.Round(finalValue, 4);
    }

    /// <summary>
    /// Añade un modificador temporal global (ej. buff de un combate completo o pasiva).
    /// </summary>
    public void AddTemporaryModifier(StatType statType, StatModifier mod)
    {
        if (stats.ContainsKey(statType))
        {
            stats[statType].AddModifier(mod);
        }
    }

    /// <summary>
    /// Remueve un modificador temporal global por su origen.
    /// </summary>
    public void RemoveTemporaryModifiersFromSource(object source)
    {
        foreach (var statKvp in stats)
        {
            statKvp.Value.RemoveAllModifiersFromSource(source);
        }
    }
}

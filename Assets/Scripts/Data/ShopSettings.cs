using UnityEngine;

/// <summary>
/// ScriptableObject centralizado con todos los parámetros ajustables de la tienda.
/// Permite modificar el balance sin tocar código.
/// </summary>
[CreateAssetMenu(fileName = "ShopSettings", menuName = "Sveliaty/Shop/Shop Settings")]
public class ShopSettings : ScriptableObject
{
    [Header("Slots")]
    [Tooltip("Cantidad de slots que muestra la tienda.")]
    public int slotCount = 3;

    [Header("Probabilidades de Tipo de Slot")]
    [Range(0f, 1f)]
    [Tooltip("Probabilidad de que un slot contenga una habilidad activa (0=nunca, 1=siempre). " +
             "El resto del peso corresponde a ítems de estadísticas.")]
    public float abilitySlotChance = 0.33f;

    [Header("Escalado de Precio - Ítems (Tinta)")]
    [Tooltip("Tinta adicional por cada visita a la tienda.")]
    public int inkPriceScalePerVisit = 8;

    [Header("Escalado de Precio - Habilidades (Cartas)")]
    [Tooltip("Coste base en cartas para una habilidad Tier 1.")]
    public int abilityBaseCostTier1 = 3;
    [Tooltip("Coste base en cartas para mejorar a Tier 2.")]
    public int abilityBaseCostTier2 = 5;
    [Tooltip("Coste base en cartas para mejorar a Tier 3.")]
    public int abilityBaseCostTier3 = 8;
    [Tooltip("Cartas adicionales por visita a la tienda (escala el coste de habilidades).")]
    public int abilityCardCostScalePerVisit = 1;

    [Header("Reroll")]
    [Tooltip("Coste base de Tinta del primer reroll.")]
    public int rerollBaseInkCost = 15;
    [Tooltip("Incremento de Tinta por cada reroll adicional en la misma visita.")]
    public int rerollCostIncrement = 5;

    [Header("Restricciones de Habilidades")]
    [Tooltip("Máximo de habilidades activas por rama.")]
    public int maxAbilitiesPerBranch = 2;
}

using System.Collections.Generic;

/// <summary>
/// Representa el estado del combate actual (o acción) para calcular las estadísticas dinámicamente.
/// Puede ser extendido en el futuro para agregar buffs de acción, estados alterados temporales, etc.
/// </summary>
public class CombatContext
{
    // Cartas disponibles o utilizadas en el momento de calcular la estadística
    public Dictionary<AffinityType, int> currentCards;

    // Modificadores temporales propios de la acción actual
    public List<StatModifier> actionModifiers;

    public CombatContext()
    {
        currentCards = new Dictionary<AffinityType, int>();
        actionModifiers = new List<StatModifier>();
    }

    public CombatContext(Dictionary<AffinityType, int> cards)
    {
        currentCards = new Dictionary<AffinityType, int>(cards);
        actionModifiers = new List<StatModifier>();
    }
}

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Registro centralizado de todas las piezas visuales del personaje.
/// Permite buscar piezas por habilidad, slot o set sin hardcodear nada.
/// </summary>
[CreateAssetMenu(menuName = "Sveliaty/Character/Visual Piece Database", fileName = "VisualPieceDatabase")]
public class VisualPieceDatabase : ScriptableObject
{
    [Tooltip("Lista de todas las piezas visuales disponibles en el juego.")]
    public List<VisualPieceData> allPieces = new List<VisualPieceData>();

    // ─────────────────────────────────────────────────────────────────────────
    // Lookups principales
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Busca la pieza cuya sourceAbility coincide exactamente con la habilidad dada.
    /// Devuelve null si no existe (= la habilidad no genera pieza visual).
    /// Las habilidades básicas nunca tienen pieza; si la tienen, se ignoran.
    /// </summary>
    public VisualPieceData GetPieceForAbility(AbilityData ability)
    {
        if (ability == null) return null;
        if (ability.isBasicAbility) return null;

        return allPieces.FirstOrDefault(p => p != null && p.sourceAbility == ability);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Lookups auxiliares (debugging / editor tools)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Devuelve todas las piezas que ocupan un slot específico.</summary>
    public List<VisualPieceData> GetPiecesBySlot(BodyPartSlot slot)
    {
        return allPieces.Where(p => p != null && p.GetSlot() == slot).ToList();
    }

    /// <summary>
    /// Devuelve todas las piezas de una afinidad concreta
    /// leyendo el affinityType directo del sourceAbility de cada pieza.
    /// </summary>
    public List<VisualPieceData> GetPiecesByAffinity(AffinityType affinity)
    {
        return allPieces
            .Where(p => p != null && p.sourceAbility != null && p.sourceAbility.affinityType == affinity)
            .ToList();
    }
}

using UnityEngine;

/// <summary>
/// Datos de una pieza visual del personaje.
/// MUY SIMPLE a propósito: solo identifica la pieza y su modelo.
/// Toda la lógica de materiales, tiers y colores es responsabilidad
/// de CharacterVisualController + CharacterVisualSettings (global).
/// </summary>
[CreateAssetMenu(menuName = "Sveliaty/Character/Visual Piece", fileName = "NewVisualPiece")]
public class VisualPieceData : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("Nombre descriptivo para el inspector, p.ej. 'Brazos Conejo'.")]
    public string pieceName;

    [Header("Trigger de habilidad")]
    [Tooltip("La habilidad no-básica que desbloquea esta pieza.")]
    public AbilityData sourceAbility;

    [Header("Modelo")]
    [Tooltip("Prefab del modelo voxel en escala de grises. Debe tener MeshRenderer(s).")]
    public GameObject prefab;

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// El slot se deduce automáticamente de la afinidad de sourceAbility.
    /// Fuerza → Brazos | Agilidad → Torso | Destreza → Mascara.
    /// </summary>
    public BodyPartSlot GetSlot()
    {
        if (sourceAbility == null)
        {
            Debug.LogWarning($"[VisualPieceData] '{pieceName}': sourceAbility es null, slot por defecto = Brazos.");
            return BodyPartSlot.Brazos;
        }

        return sourceAbility.affinityType switch
        {
            AffinityType.Fuerza   => BodyPartSlot.Brazos,
            AffinityType.Agilidad => BodyPartSlot.Torso,
            AffinityType.Destreza => BodyPartSlot.Mascara,
            _                     => BodyPartSlot.Brazos
        };
    }
}

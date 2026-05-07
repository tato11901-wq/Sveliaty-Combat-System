using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controlador visual modular del personaje.
///
/// Responsabilidades:
///  - Recibir la lista de habilidades activas y refrescar las piezas 3D.
///  - Instanciar el prefab correcto en el SlotPoint correspondiente.
///  - Delegar TODO lo relacionado con materiales/colores a CharacterVisualSettings.
///  - NO mostrar nada si un slot no tiene habilidad (ausencia intencional).
///
/// Bijección afinidad → slot: Fuerza=Brazos | Agilidad=Torso | Destreza=Mascara.
/// </summary>
public class CharacterVisualController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Slot Points (Transforms vacíos hijos de Personaje)")]
    public Transform slotBrazos;
    public Transform slotTorso;
    public Transform slotMascara;

    [Header("Datos")]
    [Tooltip("Registro de todas las piezas visuales disponibles.")]
    public VisualPieceDatabase pieceDatabase;

    [Tooltip("Configuración global de materiales, colores y tiers. UN solo asset para todo el personaje.")]
    public CharacterVisualSettings visualSettings;

    [Header("Cartas actuales del jugador (para modulación de color)")]
    [Tooltip("Actualizar estos valores antes de llamar a RefreshCharacterVisual.")]
    public int cardsFuerza;
    public int cardsAgilidad;
    public int cardsDestreza;

    // ─────────────────────────────────────────────────────────────────────────
    // Estado interno
    // ─────────────────────────────────────────────────────────────────────────

    private GameObject _instanceBrazos;
    private GameObject _instanceTorso;
    private GameObject _instanceMascara;

    // Caché del estado resuelto actual (para RefreshColors sin reinstanciar)
    private (VisualPieceData piece, int tier)? _stateBrazos;
    private (VisualPieceData piece, int tier)? _stateTorso;
    private (VisualPieceData piece, int tier)? _stateMascara;

    // Nombres de propiedades Standard Specular (evitar string lookups en caliente)
    private static readonly int PropAlbedo    = Shader.PropertyToID("_Color");
    private static readonly int PropSpecColor = Shader.PropertyToID("_SpecColor");
    private static readonly int PropGlossiness= Shader.PropertyToID("_Glossiness");
    private static readonly int PropEmission  = Shader.PropertyToID("_EmissionColor");

    // ─────────────────────────────────────────────────────────────────────────
    // API pública
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Punto de entrada principal. Llama esto cuando el jugador obtiene/mejora una habilidad.
    /// Reinstancia los prefabs que hayan cambiado y actualiza los colores de todos los slots.
    /// </summary>
    public void RefreshCharacterVisual(
        List<ActiveSkillState> activeSkills,
        int cardsFuerza, int cardsAgilidad, int cardsDestreza)
    {
        this.cardsFuerza   = cardsFuerza;
        this.cardsAgilidad = cardsAgilidad;
        this.cardsDestreza = cardsDestreza;

        if (!ValidateDependencies()) return;

        // ── Resolver qué pieza gana en cada slot ──────────────────────────────
        var resolved = new Dictionary<BodyPartSlot, (VisualPieceData piece, int tier)>();

        if (activeSkills != null)
        {
            foreach (ActiveSkillState skillState in activeSkills)
            {
                if (skillState?.ability == null)       continue;
                if (skillState.ability.isBasicAbility) continue;

                VisualPieceData piece = pieceDatabase.GetPieceForAbility(skillState.ability);
                if (piece == null) continue;

                BodyPartSlot slot = piece.GetSlot();
                int tier = Mathf.Clamp(skillState.tier, 1, 3);

                if (!resolved.ContainsKey(slot) || tier >= resolved[slot].tier)
                    resolved[slot] = (piece, tier);
            }
        }

        // ── Aplicar cada slot ─────────────────────────────────────────────────
        ApplySlot(BodyPartSlot.Brazos,  resolved);
        ApplySlot(BodyPartSlot.Torso,   resolved);
        ApplySlot(BodyPartSlot.Mascara, resolved);
    }

    /// <summary>Sobrecarga sin parámetros de cartas: usa los valores del inspector.</summary>
    public void RefreshCharacterVisual(List<ActiveSkillState> activeSkills)
        => RefreshCharacterVisual(activeSkills, cardsFuerza, cardsAgilidad, cardsDestreza);

    /// <summary>
    /// Actualiza SOLO los colores (MaterialPropertyBlock) sin reinstanciar prefabs.
    /// Llamar cuando la cantidad de cartas cambia pero las habilidades no.
    /// </summary>
    public void RefreshColors(int cardsFuerza, int cardsAgilidad, int cardsDestreza)
    {
        this.cardsFuerza   = cardsFuerza;
        this.cardsAgilidad = cardsAgilidad;
        this.cardsDestreza = cardsDestreza;

        if (!ValidateDependencies()) return;

        ReapplyMaterial(BodyPartSlot.Brazos,  _stateBrazos,  _instanceBrazos);
        ReapplyMaterial(BodyPartSlot.Torso,   _stateTorso,   _instanceTorso);
        ReapplyMaterial(BodyPartSlot.Mascara, _stateMascara, _instanceMascara);
    }

    /// <summary>Limpia todas las piezas instanciadas. Útil para reset de run.</summary>
    public void ClearAll()
    {
        ClearSlot(BodyPartSlot.Brazos);
        ClearSlot(BodyPartSlot.Torso);
        ClearSlot(BodyPartSlot.Mascara);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Lógica interna — instanciación
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplySlot(
        BodyPartSlot slot,
        Dictionary<BodyPartSlot, (VisualPieceData piece, int tier)> resolved)
    {
        if (resolved.TryGetValue(slot, out var entry))
            InstantiateAndApply(slot, entry.piece, entry.tier);
        else
            ClearSlot(slot); // Sin habilidad → slot vacío (intencional)
    }

    private void InstantiateAndApply(BodyPartSlot slot, VisualPieceData piece, int tier)
    {
        if (piece.prefab == null)
        {
            Debug.LogWarning($"[CharacterVisualController] '{piece.pieceName}' no tiene prefab asignado.");
            ClearSlot(slot);
            return;
        }

        Transform anchor = GetAnchor(slot);
        if (anchor == null)
        {
            Debug.LogError($"[CharacterVisualController] Slot '{slot}' no tiene Transform asignado.");
            return;
        }

        DestroyInstance(slot);

        GameObject instance = Instantiate(piece.prefab, anchor.position, anchor.rotation, anchor);
        instance.name = $"{piece.pieceName} [T{tier}]";
        SetInstance(slot, instance);
        SaveState(slot, (piece, tier));

        ApplyMaterial(instance, piece, tier);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Lógica interna — materiales
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aplica el material al GameObject instanciado.
    /// T3 → material fijo global.
    /// T1/T2 → MaterialPropertyBlock calculado desde CharacterVisualSettings.
    /// </summary>
    private void ApplyMaterial(GameObject instance, VisualPieceData piece, int tier)
    {
        MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"[CharacterVisualController] '{instance.name}' no tiene MeshRenderers.");
            return;
        }

        if (tier == 3)
        {
            // ── Tier 3: material final único ──────────────────────────────────
            if (visualSettings.tier3FinalMaterial == null)
            {
                Debug.LogWarning("[CharacterVisualController] tier3FinalMaterial no asignado en CharacterVisualSettings.");
                return;
            }
            foreach (var r in renderers)
                r.material = visualSettings.tier3FinalMaterial;
        }
        else
        {
            // ── Tier 1/2: MaterialPropertyBlock dinámico ──────────────────────
            AffinityType affinity = piece.sourceAbility.affinityType;
            int cards = GetCardsForAffinity(affinity);

            Color albedo    = visualSettings.CalculateAlbedo(affinity, cards, tier);
            Color specular  = visualSettings.GetSpecularColor(tier);
            float smoothness= visualSettings.GetSmoothness(tier);
            Color emission  = visualSettings.GetEmissionColor(tier);

            var block = new MaterialPropertyBlock();
            block.SetColor(PropAlbedo,    albedo);
            block.SetColor(PropSpecColor, specular);
            block.SetFloat(PropGlossiness, smoothness);
            block.SetColor(PropEmission,  emission);

            foreach (var r in renderers)
                r.SetPropertyBlock(block);
        }
    }

    /// <summary>Re-aplica material a un slot ya instanciado (para RefreshColors).</summary>
    private void ReapplyMaterial(
        BodyPartSlot slot,
        (VisualPieceData piece, int tier)? state,
        GameObject instance)
    {
        if (state == null || instance == null) return;
        // T3 no usa MaterialPropertyBlock, el color no cambia con cartas
        if (state.Value.tier == 3) return;
        ApplyMaterial(instance, state.Value.piece, state.Value.tier);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers de slot
    // ─────────────────────────────────────────────────────────────────────────

    private void ClearSlot(BodyPartSlot slot)
    {
        DestroyInstance(slot);
        SaveState(slot, null);
    }

    private Transform GetAnchor(BodyPartSlot slot) => slot switch
    {
        BodyPartSlot.Brazos  => slotBrazos,
        BodyPartSlot.Torso   => slotTorso,
        BodyPartSlot.Mascara => slotMascara,
        _                    => null
    };

    private void SetInstance(BodyPartSlot slot, GameObject go)
    {
        switch (slot)
        {
            case BodyPartSlot.Brazos:  _instanceBrazos  = go; break;
            case BodyPartSlot.Torso:   _instanceTorso   = go; break;
            case BodyPartSlot.Mascara: _instanceMascara = go; break;
        }
    }

    private void DestroyInstance(BodyPartSlot slot)
    {
        GameObject existing = slot switch
        {
            BodyPartSlot.Brazos  => _instanceBrazos,
            BodyPartSlot.Torso   => _instanceTorso,
            BodyPartSlot.Mascara => _instanceMascara,
            _                    => null
        };
        if (existing != null)
        {
            Destroy(existing);
            SetInstance(slot, null);
        }
    }

    private void SaveState(BodyPartSlot slot, (VisualPieceData, int)? state)
    {
        switch (slot)
        {
            case BodyPartSlot.Brazos:  _stateBrazos  = state; break;
            case BodyPartSlot.Torso:   _stateTorso   = state; break;
            case BodyPartSlot.Mascara: _stateMascara = state; break;
        }
    }

    private int GetCardsForAffinity(AffinityType affinity) => affinity switch
    {
        AffinityType.Fuerza   => cardsFuerza,
        AffinityType.Agilidad => cardsAgilidad,
        AffinityType.Destreza => cardsDestreza,
        _                     => 0
    };

    private bool ValidateDependencies()
    {
        if (pieceDatabase == null)
        {
            Debug.LogError("[CharacterVisualController] pieceDatabase no asignada.");
            return false;
        }
        if (visualSettings == null)
        {
            Debug.LogError("[CharacterVisualController] visualSettings no asignado.");
            return false;
        }
        return true;
    }

    private void OnDestroy() => ClearAll();
}

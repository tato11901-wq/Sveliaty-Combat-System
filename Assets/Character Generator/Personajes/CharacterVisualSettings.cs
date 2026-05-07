using UnityEngine;

/// <summary>
/// Configuración global de materiales y color para el sistema visual del personaje.
/// UN SOLO asset controla el aspecto de TODAS las piezas en todos los tiers.
///
/// Sistema de color:
///   - 0 cartas  → la pieza tiende a negro/gris (modelo grayscale sin tinte)
///   - N cartas  → la pieza tiende al color de la afinidad (Rojo/Azul/Verde)
///   La intensidad del color se interpola con una AnimationCurve para control artístico.
///
/// Tier 1: colores apagados/pasteles (baja influencia del color de afinidad).
/// Tier 2: colores vivos y saturados (alta influencia del color de afinidad).
/// Tier 3: un único material final fijo que se aplica a cualquier pieza.
/// </summary>
[CreateAssetMenu(menuName = "Sveliaty/Character/Visual Settings", fileName = "CharacterVisualSettings")]
public class CharacterVisualSettings : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    // Tier 3 — Material final único
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Tier 3 — Material Final (compartido por todas las piezas)")]
    [Tooltip("Material definitivo que reemplaza el grayscale en Tier 3. " +
             "Los UVs de todos los modelos están cuadrados para este material.")]
    public Material tier3FinalMaterial;

    // ─────────────────────────────────────────────────────────────────────────
    // Colores por afinidad
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Colores de Afinidad")]
    [Tooltip("Color objetivo de las piezas de Fuerza (Brazos). Con muchas cartas tiende a este color.")]
    public Color fuerzaColor = new Color(0.85f, 0.1f, 0.1f); // Rojo

    [Tooltip("Color objetivo de las piezas de Agilidad (Torso). Con muchas cartas tiende a este color.")]
    public Color agilidadColor = new Color(0.1f, 0.3f, 0.9f); // Azul

    [Tooltip("Color objetivo de las piezas de Destreza (Mascara). Con muchas cartas tiende a este color.")]
    public Color destrezaColor = new Color(0.1f, 0.8f, 0.2f); // Verde

    [Space]
    [Tooltip("Color base cuando el jugador tiene 0 cartas de esa afinidad. " +
             "Negro puro o gris oscuro para reflejar el modelo sin tinte.")]
    public Color zeroCardsColor = new Color(0.15f, 0.15f, 0.15f); // Gris muy oscuro

    // ─────────────────────────────────────────────────────────────────────────
    // Curva de cartas
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Curva de Cards → Color")]
    [Tooltip("Cantidad de cartas que representa el 100% del color de afinidad. " +
             "Con más cartas que este valor el color no sigue aumentando.")]
    public int maxCardsReference = 20;

    [Tooltip("Curva de interpolación: eje X = cartas normalizadas (0-1), eje Y = intensidad del color (0-1). " +
             "Permite control artístico fino: curva suave, escalonada, etc.")]
    public AnimationCurve cardColorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    // Parámetros Standard Specular por Tier
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Tier 1 — Parámetros Standard Specular (apagado/pastel)")]
    [Tooltip("Qué tanto del color de afinidad se mezcla. Bajo = más gris, apagado.")]
    [Range(0f, 1f)] public float tier1ColorIntensity  = 0.35f;

    [Tooltip("Suavidad de la superficie. Bajo = mate.")]
    [Range(0f, 1f)] public float tier1Smoothness      = 0.15f;

    [Tooltip("Color especular en Tier 1. Tono bajo para aspecto mate.")]
    public Color tier1SpecularColor = new Color(0.1f, 0.1f, 0.1f);

    [Tooltip("Emisión en Tier 1. Normalmente negro (sin emisión).")]
    public Color tier1EmissionColor = Color.black;

    [Header("Tier 2 — Parámetros Standard Specular (vivo/definido)")]
    [Tooltip("Qué tanto del color de afinidad se mezcla. Alto = color vivo y saturado.")]
    [Range(0f, 1f)] public float tier2ColorIntensity  = 0.85f;

    [Tooltip("Suavidad de la superficie. Mayor = más brillante.")]
    [Range(0f, 1f)] public float tier2Smoothness      = 0.45f;

    [Tooltip("Color especular en Tier 2. Más vivo que T1.")]
    public Color tier2SpecularColor = new Color(0.3f, 0.3f, 0.3f);

    [Tooltip("Emisión sutil en Tier 2 para dar presencia visual.")]
    public Color tier2EmissionColor = Color.black; // Ajustar por afinidad si se quiere

    // ─────────────────────────────────────────────────────────────────────────
    // API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el color de afinidad objetivo para un slot/afinidad concreto.
    /// </summary>
    public Color GetAffinityColor(AffinityType affinity) => affinity switch
    {
        AffinityType.Fuerza   => fuerzaColor,
        AffinityType.Agilidad => agilidadColor,
        AffinityType.Destreza => destrezaColor,
        _                     => Color.white
    };

    /// <summary>
    /// Calcula el color final del albedo para una pieza según:
    ///   - La afinidad de la pieza (determina el color objetivo)
    ///   - La cantidad de cartas del jugador en esa rama
    ///   - El tier actual (controla la intensidad máxima del color)
    ///
    /// 0 cartas → zeroCardsColor (gris oscuro)
    /// maxCardsReference → color de afinidad pleno (modulado por tier)
    /// </summary>
    /// <param name="affinity">Afinidad de la pieza (determina el color objetivo).</param>
    /// <param name="cardCount">Cartas actuales del jugador en esa afinidad.</param>
    /// <param name="tier">Tier actual de la pieza (1 o 2; T3 no usa este método).</param>
    public Color CalculateAlbedo(AffinityType affinity, int cardCount, int tier)
    {
        // Normalizar cartas en [0, 1]
        float t = maxCardsReference > 0
            ? Mathf.Clamp01((float)cardCount / maxCardsReference)
            : 0f;

        // Pasar por la curva artística
        float curveValue = cardColorCurve.Evaluate(t);

        // Intensidad máxima según tier
        float tierIntensity = tier == 2 ? tier2ColorIntensity : tier1ColorIntensity;

        // Color final: de gris oscuro (0 cartas) al color de afinidad (max cartas × intensidad tier)
        Color targetColor = GetAffinityColor(affinity);
        return Color.Lerp(zeroCardsColor, targetColor, curveValue * tierIntensity);
    }

    /// <summary>Devuelve el specular color correspondiente al tier.</summary>
    public Color GetSpecularColor(int tier) => tier == 2 ? tier2SpecularColor : tier1SpecularColor;

    /// <summary>Devuelve el smoothness correspondiente al tier.</summary>
    public float GetSmoothness(int tier)    => tier == 2 ? tier2Smoothness    : tier1Smoothness;

    /// <summary>Devuelve el emission color correspondiente al tier.</summary>
    public Color GetEmissionColor(int tier) => tier == 2 ? tier2EmissionColor : tier1EmissionColor;
}

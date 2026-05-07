using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject que define un enemigo de Élite.
///
/// A diferencia de los enemigos normales, un Élite tiene un único SO que define
/// su sprite y sus stats base (Tier 1). Los Tiers 2-4 se calculan automáticamente
/// aplicando los multiplicadores configurados en el Inspector.
/// </summary>
[CreateAssetMenu(menuName = "Sveliaty/Enemy/Elite Enemy Data")]
public class EliteEnemyData : ScriptableObject
{
    [Header("Identificación")]
    public string displayName;
    public Sprite sprite;

    [Header("Afinidades")]
    [Tooltip("Afinidad principal (para modo Passive).")]
    public AffinityType affinityType;

    [Tooltip("Relaciones de debilidad/resistencia del élite.")]
    public AffinityRelation[] affinityRelations;

    // ─────────────────────────────────────────────────────────────────────────
    // Stats base (Tier 1)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Stats Base — Tier 1")]
    [Tooltip("Vida del élite en Tier 1.")]
    public int baseRPGLife = 80;

    [Tooltip("Dados que tira el élite en Tier 1.")]
    public int baseRPGDiceCount = 3;

    [Tooltip("Daño que recibe el jugador al perder el combate (Tier 1).")]
    public int baseFailureDamage = 15;

    [Tooltip("Intentos que tiene el jugador para derrotarlo (Tier 1).")]
    public int baseMaxAttempts = 6;

    // ─────────────────────────────────────────────────────────────────────────
    // Comportamiento por turno — Probabilidades de acción
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Comportamiento — Probabilidad de acción (pesos)")]
    public float healChance     = 15f;
    public float armorChance    = 15f;
    public float thornsChance   = 10f;
    public float speedChance    = 10f;
    public float doNothingChance= 50f;

    // ─────────────────────────────────────────────────────────────────────────
    // Comportamiento por turno — Magnitudes de efecto
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Comportamiento — Magnitudes de efecto")]

    [Tooltip("% de vida total que se cura (0.0–1.0).")]
    [Range(0f, 1f)]
    public float healAmount = 0.20f;

    [Tooltip("Armadura plana que gana al activar Armor.")]
    public int armorAmount = 8;

    [Tooltip("% del daño recibido que refleja al jugador (0.0–1.0).")]
    [Range(0f, 1f)]
    public float thornsAmount = 0.35f;

    [Tooltip("Probabilidad de esquivar el próximo ataque (0.0–1.0).")]
    [Range(0f, 1f)]
    public float speedAmount = 0.55f;

    // ─────────────────────────────────────────────────────────────────────────
    // Escalado por Tier
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Escalado por Tier (índice 0 = Tier 1 … índice 3 = Tier 4)")]

    [Tooltip("Multiplicador de vida por tier. Tier1=x1, Tier2=x1.6, Tier3=x2.5, Tier4=x4")]
    public float[] tierLifeMultipliers = { 1.0f, 1.6f, 2.5f, 4.0f };

    [Tooltip("Multiplicador de daño por tier. Tier1=x1, Tier2=x1.4, Tier3=x2, Tier4=x3")]
    public float[] tierDamageMultipliers = { 1.0f, 1.4f, 2.0f, 3.0f };

    [Tooltip("Intentos extra respecto al base por tier.")]
    public int[] tierAttemptBonuses = { 0, 1, 2, 3 };

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    public float GetLifeMultiplier(int tier)
    {
        int idx = Mathf.Clamp(tier - 1, 0, tierLifeMultipliers.Length - 1);
        return tierLifeMultipliers[idx];
    }

    public float GetDamageMultiplier(int tier)
    {
        int idx = Mathf.Clamp(tier - 1, 0, tierDamageMultipliers.Length - 1);
        return tierDamageMultipliers[idx];
    }

    public int GetAttemptBonus(int tier)
    {
        int idx = Mathf.Clamp(tier - 1, 0, tierAttemptBonuses.Length - 1);
        return tierAttemptBonuses[idx];
    }
}

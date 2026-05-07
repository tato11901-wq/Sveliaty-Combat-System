using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Sveliaty/Enemy/Enemy Tier Data")]
public class EnemyTierData : ScriptableObject
{
    public EnemyTier enemyTier;
    public Sprite sprite;

    [Header("Sistema RPG")]
    [Tooltip("Vida del enemigo en modo RPG Tradicional.")]
    public int RPGLife;

    [Tooltip("Cantidad de dados que tira el enemigo en modo RPG Tradicional.")]
    public int RPGDiceCount;

    [Tooltip("Daño que recibe el jugador al perder el combate.")]
    public int failureDamage;

    [Tooltip("Intentos que tiene el jugador para derrotar al enemigo antes de perder el combate.")]
    public int maximunDiceThrow;

    // ─────────────────────────────────────────────────────────────────────────
    // Comportamiento por turno — Probabilidades de acción
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Comportamiento en Turno — Probabilidad de acción (pesos)")]
    [Tooltip("Peso para curarse vida. Se normaliza junto con los demás pesos.")]
    public float healChance = 10f;

    [Tooltip("Peso para ganar armadura.")]
    public float armorChance = 10f;

    [Tooltip("Peso para activar espinas (daño reflejado).")]
    public float thornsChance = 10f;

    [Tooltip("Peso para activar evasión de velocidad (esquivar el próximo ataque).")]
    public float speedChance = 10f;

    [Tooltip("Peso para no hacer nada.")]
    public float doNothingChance = 60f;

    // ─────────────────────────────────────────────────────────────────────────
    // Comportamiento por turno — Magnitud de cada efecto
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Comportamiento en Turno — Magnitud de efectos")]

    [Tooltip("Porcentaje de vida total que se cura el enemigo al ejecutar Heal (0.0 = 0%, 1.0 = 100%).")]
    [Range(0f, 1f)]
    public float healAmount = 0.15f;

    [Tooltip("Cantidad plana de armadura que gana el enemigo al ejecutar Armor.")]
    public int armorAmount = 5;

    [Tooltip("Porcentaje del daño recibido que el enemigo devuelve al jugador al ejecutar Thorns (0.0 = 0%, 1.0 = 100%).")]
    [Range(0f, 1f)]
    public float thornsAmount = 0.30f;

    [Tooltip("Probabilidad de esquivar el próximo ataque del jugador al ejecutar Speed (0.0 = 0%, 1.0 = 100%).")]
    [Range(0f, 1f)]
    public float speedAmount = 0.50f;

    // ─────────────────────────────────────────────────────────────────────────

    public String GetEnemyTier()
    {
        return enemyTier switch
        {
            EnemyTier.Tier_1 => "Enemy Tier 1",
            EnemyTier.Tier_2 => "Enemy Tier 2",
            EnemyTier.Tier_3 => "Enemy Tier 3",
            _ => "Tier_1"
        };
    }
}
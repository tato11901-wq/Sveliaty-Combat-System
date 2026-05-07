using UnityEngine;

/// <summary>
/// Acción que representa el uso de una Habilidad Activa (AbilityData).
/// </summary>
public class AbilityAction : CombatAction
{
    private AbilityData data;

    public AbilityAction(AbilityData abilityData)
    {
        data = abilityData;
    }

    public override string ActionName => data.abilityName;
    public override AffinityType ActionAffinity => data.affinityType;
    public override int CardCost => data.cardCost;
    public override int HealthCost => data.healthCost;
    public override int TurnCost => data.turnCost > 0 ? data.turnCost : 1;

    public override bool Execute(CombatManager manager, TurnContext context)
    {
        EnemyInstance enemy = manager.GetCurrentEnemy();

        // 0. Evasión del enemigo (Buff Velocidad)
        if (enemy.hasSpeedEvasion)
        {
            if (UnityEngine.Random.Range(0f, 1f) < enemy.activeSpeedEvasion)
            {
                Debug.Log($"[{ActionName}] El enemigo evadió la habilidad gracias a su Velocidad.");
                enemy.activeSpeedEvasion = 0f;
                manager.RegisterAttackResult(0, 0, 0, 1f);
                return false;
            }
            enemy.activeSpeedEvasion = 0f;
        }

        // 1. Chance de éxito propia de la habilidad
        if (data.hasSuccessChance)
        {
            if (UnityEngine.Random.Range(0f, 100f) >= data.successChance)
            {
                Debug.Log($"[{ActionName}] FALLÓ la tirada de probabilidad de éxito.");
                return false; // Falló
            }
        }

        // 2. Cálculo de dados
        int baseDiceCount = enemy.currentRPGDiceCount;
        baseDiceCount += data.diceModifier;
        if (data.diceMultiplier != 0) baseDiceCount = Mathf.RoundToInt(baseDiceCount * data.diceMultiplier);
        baseDiceCount += data.diceAddition;
        baseDiceCount = Mathf.Max(1, baseDiceCount);
        
        int diceMax = data.diceMaxValue > 0 ? data.diceMaxValue : 12;
        int roll = manager.RollDice(baseDiceCount, diceMax);

        // 3. Stats dinámicos (PlayerStatsManager)
        float statBonus = 0f;
        StatType correspondingStat = StatType.Fuerza;
        if (ActionAffinity == AffinityType.Fuerza) correspondingStat = StatType.Fuerza;
        else if (ActionAffinity == AffinityType.Agilidad) correspondingStat = StatType.Velocidad;
        else if (ActionAffinity == AffinityType.Destreza) correspondingStat = StatType.Destreza;

        if (manager.statsManager != null)
        {
            CombatContext combatCtx = new CombatContext(context.InitialCards);
            statBonus = manager.statsManager.GetFinalStat(correspondingStat, combatCtx);
        }
        else
        {
            statBonus = context.InitialCards.ContainsKey(ActionAffinity) ? context.InitialCards[ActionAffinity] : 0;
        }

        if (data.cardMultiplier != 0)
        {
            statBonus *= data.cardMultiplier;
        }

        if (manager.curseManager != null && manager.curseManager.HasNegatedCards())
        {
            statBonus = -statBonus;
            Debug.Log($"[{ActionName}] Cartas negadas por maldición.");
        }

        // 4. Multiplicador de afinidad + bonus
        bool isFirstStrike = false;
        float affinityMult = manager.GetAffinityMultiplier(ActionAffinity, out isFirstStrike) + data.affinityMultiplierBonus;
        float multiplier = affinityMult;

        if (BestiaryManager.Instance != null)
        {
            BestiaryManager.Instance.RegisterAffinityDiscovered(enemy.enemyData.name, ActionAffinity);
        }

        // --- GOLPE CRÍTICO ---
        bool isCritical = false;
        float critChance = manager.statsManager != null ? manager.statsManager.GetFinalStat(StatType.ProbCritico, null) : 5f;
        if (UnityEngine.Random.Range(0f, 100f) < critChance)
        {
            isCritical = true;
            multiplier *= 1.5f;
            Debug.Log($"[{ActionName}] ¡GOLPE CRÍTICO!");
        }

        // 5. Daño final y Mitigación (Armadura del enemigo)
        float totalBase = roll + statBonus;
        int totalFinal = Mathf.RoundToInt(totalBase * multiplier);

        if (enemy.activeArmor > 0)
        {
            int damageMitigated = Mathf.Min(enemy.activeArmor, totalFinal);
            totalFinal -= damageMitigated;
            enemy.activeArmor -= damageMitigated; 
            Debug.Log($"[{ActionName}] El enemigo mitigó {damageMitigated} de daño con su Armadura.");
        }

        // 6. Daño reflejado (Espinas)
        if (enemy.activeThorns > 0 && totalFinal > 0)
        {
            int thornsDamage = Mathf.RoundToInt(totalFinal * enemy.activeThorns);
            if (thornsDamage > 0)
            {
                manager.playerManager.ModifyHealth(-thornsDamage);
                manager.NotifyHitReceived(thornsDamage);
                Debug.Log($"[{ActionName}] Espinas: Recibiste {thornsDamage} de daño devuelto.");
            }
        }

        // 7. Reportar resultado
        manager.RegisterAttackResult(roll, Mathf.RoundToInt(statBonus), totalFinal, multiplier, isCritical, affinityMult, isFirstStrike);

        // --- ROBO DE VIDA ---
        float lifesteal = manager.statsManager != null ? manager.statsManager.GetFinalStat(StatType.RoboVida, null) : 0f;
        if (lifesteal > 0 && totalFinal > 0)
        {
            int healAmount = Mathf.Min(totalFinal, Mathf.RoundToInt(lifesteal));
            if (healAmount > 0)
            {
                manager.playerManager.ModifyHealth(healAmount);
                Debug.Log($"[{ActionName}] Robo de vida: Te curaste {healAmount} HP.");
            }
        }

        return true;
    }

    public override void OnActionSuccess(CombatManager manager)
    {
        if (data.hasOnKillEffect)
        {
            manager.playerManager.ModifyHealth(data.onKillHealthReward);
            Debug.Log($"[{ActionName}] Efecto OnKill: Recuperaste {data.onKillHealthReward} HP.");
        }
    }

    public override void OnActionFail(CombatManager manager)
    {
        manager.playerManager.ModifyHealth(-data.onFailHealthPenalty);
        manager.GetCurrentEnemy().attemptsRemaining -= data.onFailTurnPenalty;
        manager.NotifyAttemptsChanged();
        Debug.Log($"[{ActionName}] Efecto OnFail: Perdiste {data.onFailHealthPenalty} HP y {data.onFailTurnPenalty} intentos adicionales.");
    }

    public override bool ConsumeTurn(CombatManager manager)
    {
        if (data.canAvoidTurnConsumption)
        {
            if (UnityEngine.Random.Range(0f, 100f) < data.avoidTurnChance)
            {
                Debug.Log($"[{ActionName}] Efecto especial: No se consumió el turno.");
                return false;
            }
            else
            {
                manager.playerManager.ModifyHealth(-data.avoidTurnFailPenalty);
                Debug.Log($"[{ActionName}] Penalización por fallar en evitar el turno: -{data.avoidTurnFailPenalty} HP.");
            }
        }
        
        return base.ConsumeTurn(manager);
    }
}

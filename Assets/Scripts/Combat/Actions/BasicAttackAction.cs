using UnityEngine;

/// <summary>
/// Acción que representa un Ataque Básico sin habilidades especiales.
/// </summary>
public class BasicAttackAction : CombatAction
{
    private AffinityType chosenAffinity;

    public BasicAttackAction(AffinityType affinity)
    {
        chosenAffinity = affinity;
    }

    public override string ActionName => "Ataque Básico";
    public override AffinityType ActionAffinity => chosenAffinity;
    
    public override int CardCost => 0;
    public override int HealthCost => 0;
    public override int TurnCost => 1;

    public override bool Execute(CombatManager manager, TurnContext context)
    {
        EnemyInstance enemy = manager.GetCurrentEnemy();

        // 0. Evasión del enemigo (Buff Velocidad)
        if (enemy.hasSpeedEvasion)
        {
            if (UnityEngine.Random.Range(0f, 1f) < enemy.activeSpeedEvasion)
            {
                Debug.Log($"[{ActionName}] El enemigo evadió el ataque gracias a su Velocidad.");
                enemy.activeSpeedEvasion = 0f; // Se consume la evasión
                manager.RegisterAttackResult(0, 0, 0, 1f);
                return false;
            }
            enemy.activeSpeedEvasion = 0f; // Se consume incluso si falla
        }

        // 1. Cálculo de dados
        int diceCount = enemy.currentRPGDiceCount;
        int diceMax = 12; // Valor estándar para dados si no hay habilidad
        int roll = manager.RollDice(diceCount, diceMax);

        // 2. Stats dinámicos (PlayerStatsManager)
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

        if (manager.curseManager != null && manager.curseManager.HasNegatedCards())
        {
            statBonus = -statBonus;
            Debug.Log($"[{ActionName}] Cartas negadas por maldición.");
        }

        // 3. Multiplicador de afinidad
        bool isFirstStrike = false;
        float affinityMult = manager.GetAffinityMultiplier(ActionAffinity, out isFirstStrike);
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

        // 4. Daño final y Mitigación (Armadura del enemigo)
        float totalBase = roll + statBonus;
        int totalFinal = Mathf.RoundToInt(totalBase * multiplier);

        if (enemy.activeArmor > 0)
        {
            int damageMitigated = Mathf.Min(enemy.activeArmor, totalFinal);
            totalFinal -= damageMitigated;
            enemy.activeArmor -= damageMitigated; // La armadura se gasta con el daño
            Debug.Log($"[{ActionName}] El enemigo mitigó {damageMitigated} de daño con su Armadura.");
        }

        // 5. Espinas
        if (enemy.activeThorns > 0 && totalFinal > 0)
        {
            int thornsDamage = Mathf.RoundToInt(totalFinal * enemy.activeThorns);
            if (thornsDamage > 0)
            {
                manager.playerManager.ModifyHealth(-thornsDamage);
                manager.NotifyHitReceived(thornsDamage); // <-- NOTIFICAR A LA UI
                Debug.Log($"[{ActionName}] Espinas: Recibiste {thornsDamage} de daño devuelto.");
            }
        }

        // 6. Reportar resultado
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
}

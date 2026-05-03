using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gestor de los combates (Modo TraditionalRPG Exclusivamente).
/// Responsabilidades:
/// - Máquina de estados para Fases de Turno, incluyendo turno pasivo del enemigo.
/// - Sistema de Eventos para Pasivas.
/// - Uso del patrón Command para ejecutar diferentes tipos de acciones.
/// </summary>
public class CombatManager : MonoBehaviour
{
    [Header("Manager References")]
    public PlayerManager playerManager;
    public AbilityManager abilityManager;
    public CurseManager curseManager;
    public PlayerStatsManager statsManager; 

    [Header("Combat Settings")]
    [Range(0, 100)] public float randomCardChance = 30f;

    // Estado del combate ACTUAL
    private EnemyInstance currentEnemy;
    private bool combatEnded = false;
    private bool waitingForCardSelection = false;
    private bool isProcessingPostCombat = false;

    // Estado del TURNO
    private TurnPhase currentPhase;
    private TurnContext currentTurnContext;
    private int turnsUsedThisCombat = 0;
    private int currentTurnNumber = 0;

    // === EVENTOS PARA LA UI ===
    public event Action<EnemyInstance> OnCombatStart;
    public event Action<int, int, int, float> OnAttackResult;
    public event Action<bool, int, AffinityType, int> OnCombatEnd; 
    public event Action<int> OnAttemptsChanged;
    public event Action<int> OnWaitingForCardSelection;
    public event Action<int, int, int, int, EnemyInstance> GameOver; 

    // === EVENTOS PARA PASIVAS ===
    public event Action OnCombatStartEvent;
    public event Action<CombatAction, float> OnDamageResolveEvent;
    public event Action<int> OnHitReceivedEvent;
    public event Action OnEnemyDefeatedEvent;
    public event Action OnCombatEndEvent;
    public event Action<TurnContext> OnTurnStartEvent;
    public event Action OnEnemyTurnEvent;
    public event Action<string> OnEnemyActionEvent; // Descripción de la acción del enemigo para el log

    public void StartCombat(EnemyData enemyData, EnemyTier tier, CombatMode mode, float statsMultiplier = 1f)
    {
        EnemyTierData tierData = GetTierData(enemyData, tier);

        if (tierData == null)
        {
            Debug.LogWarning("Tier " + tier + " no encontrado para " + enemyData.displayName);
            if (enemyData.enemyTierData != null && enemyData.enemyTierData.Length > 0)
                tierData = enemyData.enemyTierData[0];
            else
                return;
        }
        
        currentEnemy = new EnemyInstance(enemyData, tierData);
        if (statsMultiplier != 1f) currentEnemy.ApplyStatsMultiplier(statsMultiplier);
        
        combatEnded = false;
        isProcessingPostCombat = false;
        turnsUsedThisCombat = 0;
        currentTurnNumber = 0;
        currentPhase = TurnPhase.TurnEnd; 

        // Disparar Evento para pasivas
        OnCombatStartEvent?.Invoke();

        Debug.Log("COMBATE INICIADO: " + currentEnemy.enemyData.displayName + " (" + tierData.enemyTier + ")");
        OnCombatStart?.Invoke(currentEnemy);
    }

    private EnemyTierData GetTierData(EnemyData enemy, EnemyTier desiredTier)
    {
        if (enemy == null || enemy.enemyTierData == null) return null;
        foreach (EnemyTierData tierData in enemy.enemyTierData)
        {
            if (tierData.enemyTier == desiredTier) return tierData;
        }
        return null;
    }

    public float GetAffinityMultiplier(AffinityType attackType)
    {
        if (currentEnemy.enemyData.affinityRelations == null) return 1f;
        foreach (var relation in currentEnemy.enemyData.affinityRelations)
        {
            if (relation.type == attackType)
            {
                return relation.multiplier switch
                {
                    AffinityMultiplier.Weak => 1.5f,
                    AffinityMultiplier.Neutral => 1f,
                    AffinityMultiplier.Strong => 0.5f,
                    AffinityMultiplier.Immune => 0f,
                    _ => 1f
                };
            }
        }
        return 1f;
    }

    public int RollDice(int diceCount, int maxValue)
    {
        int total = 0;
        for (int i = 0; i < diceCount; i++) total += UnityEngine.Random.Range(1, maxValue + 1);
        return total;
    }

    public void RegisterAttackResult(int roll, int statBonus, int totalFinal, float multiplier)
    {
        OnDamageResolveEvent?.Invoke(currentTurnContext.UsedAction, totalFinal);

        bool victory = false;
        currentEnemy.currentRPGHealth -= totalFinal;
        
        if (currentEnemy.currentRPGHealth <= 0)
        {
            victory = true;
            OnEnemyDefeatedEvent?.Invoke();
            playerManager.AddScore(CalculateScorePerCombat(multiplier));
            EndCombat(true, playerManager.GetScore(), multiplier);
        }

        OnAttackResult?.Invoke(roll, statBonus, totalFinal, multiplier);
    }
    
    public void NotifyAttemptsChanged()
    {
        OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
    }

    public void PlayerAttempt(CombatAction action)
    {
        if (combatEnded || playerManager == null) return;
        
        // === FASE 1: INICIO DE TURNO ===
        currentPhase = TurnPhase.TurnStart;
        currentTurnNumber++;
        Debug.Log($"--- INICIO TURNO {currentTurnNumber} ---");
        
        currentTurnContext = new TurnContext(currentTurnNumber);
        currentTurnContext.UsedAction = action;
        currentTurnContext.ActionAffinity = action.ActionAffinity;
        currentTurnContext.InitialCards = playerManager.GetAllCards();
        
        // Evento de pasivas
        OnTurnStartEvent?.Invoke(currentTurnContext);

        // === FASE 2: SELECCIÓN DE ACCIÓN ===
        currentPhase = TurnPhase.ActionSelection;
        Debug.Log($"Fase ActionSelection: Evaluando acción {action.ActionName}...");
        
        if (!action.CanExecute(this))
        {
            Debug.LogWarning("Acción cancelada: requisitos no cumplidos.");
            return;
        }
        
        playerManager.ModifyHealth(-action.HealthCost);
        if (abilityManager != null && action.CardCost > 0)
        {
            abilityManager.SpendCards(action.ActionAffinity, action.CardCost);
        }

        // === FASE 3: RESOLUCIÓN ===
        currentPhase = TurnPhase.Resolution;
        Debug.Log("Fase Resolution: Delegando a CombatAction...");
        
        bool actionSuccess = action.Execute(this, currentTurnContext);

        if (actionSuccess) action.OnActionSuccess(this);
        else action.OnActionFail(this);

        // === FASE 4: FIN DE TURNO JUGADOR ===
        EndTurnProcessing(action, actionSuccess);
    }

    public void PlayerAttempt(AbilityData ability)
    {
        PlayerAttempt(new AbilityAction(ability));
    }
    
    public void PlayerAttempt()
    {
        PlayerAttempt(new BasicAttackAction(AffinityType.Fuerza));
    }

    private void EndTurnProcessing(CombatAction action, bool attackSuccess)
    {
        currentPhase = TurnPhase.TurnEnd;
        Debug.Log("Fase TurnEnd: Procesando consumo de turno...");

        // Se consume el turno independientemente de si el ataque acertó o falló
        bool consumedTurn = action.ConsumeTurn(this);
        if (consumedTurn) turnsUsedThisCombat++;
        
        if (RunSaveManager.Instance != null) RunSaveManager.Instance.AutoSave();

        // Verificar si el combate terminó por agotar los intentos
        if (!combatEnded && currentEnemy != null && currentEnemy.attemptsRemaining <= 0 && currentEnemy.currentRPGHealth > 0)
        {
            // El último multiplicador no está disponible aquí, usamos 1f
            EndCombat(false, playerManager.GetScore(), 1f); 
        }

        // Si el combate no terminó, procedemos al turno del enemigo
        if (!combatEnded)
        {
            ProcessEnemyTurn();
        }
    }

    /// <summary>
    /// Fase del Turno del Enemigo donde utiliza sus habilidades pasivas.
    /// </summary>
    private void ProcessEnemyTurn()
    {
        currentPhase = TurnPhase.EnemyTurn;
        Debug.Log("--- TURNO DEL ENEMIGO ---");

        OnEnemyTurnEvent?.Invoke();

        EnemyTierData data = currentEnemy.enemyTierData;
        
        float totalWeight = data.healChance + data.armorChance + data.thornsChance + data.speedChance + data.doNothingChance;
        float randomVal = UnityEngine.Random.Range(0f, totalWeight);
        string actionDescription;

        if (randomVal < data.healChance)
        {
            int healAmount = UnityEngine.Random.Range(10, 25);
            currentEnemy.currentRPGHealth += healAmount;
            if (currentEnemy.currentRPGHealth > currentEnemy.enemyTierData.RPGLife) 
                currentEnemy.currentRPGHealth = currentEnemy.enemyTierData.RPGLife;
            actionDescription = $"Se cura {healAmount} de vida. (HP: {currentEnemy.currentRPGHealth})";
            Debug.Log($"El enemigo se curó {healAmount} de vida.");
        }
        else if (randomVal < data.healChance + data.armorChance)
        {
            int armorGained = UnityEngine.Random.Range(10, 20);
            currentEnemy.activeArmor += armorGained;
            actionDescription = $"Gana {armorGained} de Armadura. (Total: {currentEnemy.activeArmor})";
            Debug.Log($"El enemigo ganó armadura. Armadura actual: {currentEnemy.activeArmor}");
        }
        else if (randomVal < data.healChance + data.armorChance + data.thornsChance)
        {
            currentEnemy.activeThorns = UnityEngine.Random.Range(0.1f, 0.3f);
            actionDescription = $"Activa Espinas ({currentEnemy.activeThorns * 100:F0}% daño reflejado).";
            Debug.Log($"El enemigo activó Espinas ({currentEnemy.activeThorns * 100}% de daño reflejado).");
        }
        else if (randomVal < data.healChance + data.armorChance + data.thornsChance + data.speedChance)
        {
            currentEnemy.hasSpeedEvasion = true;
            actionDescription = "Activa Velocidad. Puede evadir el próximo ataque.";
            Debug.Log($"El enemigo activó Velocidad. Su próximo ataque podría ser evadido.");
        }
        else
        {
            actionDescription = "Decide no hacer nada.";
            Debug.Log($"El enemigo decidió no hacer nada este turno.");
        }

        OnEnemyActionEvent?.Invoke(actionDescription);
        Debug.Log("--- FIN DEL TURNO (Ronda Completada) ---");
    }

    public void EndCombat(bool victory, int finalScore, float lastMultiplier)
    {
        if (isProcessingPostCombat) return;

        isProcessingPostCombat = true;
        combatEnded = true;

        OnCombatEndEvent?.Invoke();

        // Iniciamos la corrutina para darle tiempo a la UI de animar la vida bajando a 0
        StartCoroutine(ProcessEndCombatDelay(victory, finalScore, lastMultiplier));
    }

    private System.Collections.IEnumerator ProcessEndCombatDelay(bool victory, int finalScore, float lastMultiplier)
    {
        // Esperamos a que terminen las animaciones de ataque y reducción de barra de vida
        yield return new WaitForSeconds(0.6f);

        AffinityType rewardCard = default;

        if (victory)
        {
            if (abilityManager != null)
            {
                abilityManager.OnCombatWon();
                abilityManager.CheckUnlocks();
            }

            playerManager.RegisterEnemyDefeated();
            playerManager.RegisterCombatWon();

            if (lastMultiplier >= 1.5f) 
            {
                waitingForCardSelection = true;
                OnWaitingForCardSelection?.Invoke(finalScore);
            }
            else 
            {
                if (UnityEngine.Random.Range(0, 100) < randomCardChance)
                {
                    rewardCard = GetRandomAffinityType();
                    playerManager.AddCards(rewardCard, 1);
                    OnCombatEnd?.Invoke(true, finalScore, rewardCard, 0);
                }
                else
                {
                    OnCombatEnd?.Invoke(true, finalScore, default, 0); 
                }
            }
        }
        else
        {
            playerManager.RegisterCombatLost();
            
            // Requerimientos síncronos de curseManager se mantienen porque devuelven bool
            bool hasShield = curseManager != null && curseManager.HasDamageNegation();
            
            if (hasShield)
            {
                curseManager.ConsumeNegateDamage();
                OnCombatEnd?.Invoke(false, finalScore, default, -1);
            }
            else
            {
                int damage = currentEnemy.failureDamage;
                if (damage >= playerManager.GetCurrentLife())
                {
                    bool deathNegated = curseManager != null && curseManager.CheckAndConsumeDeathNegation();
                    if (deathNegated)
                    {
                        OnCombatEnd?.Invoke(false, finalScore, default, -2);
                    }
                    else
                    {
                        playerManager.ModifyHealth(-damage);
                        OnHitReceivedEvent?.Invoke(damage);
                        GameOver?.Invoke(
                            finalScore, 
                            playerManager.GetCards(AffinityType.Fuerza), 
                            playerManager.GetCards(AffinityType.Agilidad), 
                            playerManager.GetCards(AffinityType.Destreza), 
                            currentEnemy
                        );
                    }
                }
                else
                {
                    playerManager.ModifyHealth(-damage);
                    OnHitReceivedEvent?.Invoke(damage);
                    OnCombatEnd?.Invoke(false, finalScore, default, damage);
                }
            }
        }
        
        if (RunSaveManager.Instance != null) RunSaveManager.Instance.AutoSave();
    }

    public void SelectRewardCard(AffinityType selectedCard)
    {
        if (!waitingForCardSelection) return;
        playerManager.AddCards(selectedCard, 1);
        waitingForCardSelection = false;
        OnCombatEnd?.Invoke(true, playerManager.GetScore(), selectedCard, 0);
    }

    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])System.Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    public int CalculateScorePerCombat(float multiplier)
    {
        int Newscore = 0;
        if(currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_1) Newscore = 1;
        else if(currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_2) Newscore = 2;
        else if(currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_3) Newscore = 3;

        if(multiplier >= 1.5f) Newscore += 1;
        return Newscore;
    }

    public bool ShouldShowCurseEvent()
    {
        if (curseManager == null) return false;
        return curseManager.ShouldTriggerCurseEvent(turnsUsedThisCombat, currentEnemy.enemyData.isSpirit);
    }

    public void TriggerCurseEventFromUI()
    {
        if (curseManager != null) curseManager.TriggerCurseChoiceEvent();
    }

    public void ResetPostCombatFlag()
    {
        isProcessingPostCombat = false;
    }

    public int GetTurnsUsedThisCombat() => turnsUsedThisCombat;

    public void SetTurnsUsedThisCombat(int turns)
    {
        turnsUsedThisCombat = turns;
        currentTurnNumber = turns;
    }

    // GETTERS
    public EnemyInstance GetCurrentEnemy() => currentEnemy;
    public bool IsCombatEnded() => combatEnded;
    public TurnPhase GetCurrentPhase() => currentPhase;
    public int GetPlayerLife() => playerManager != null ? playerManager.GetCurrentLife() : 0;
    public int GetPlayerMaxLife() => playerManager != null ? playerManager.GetMaxLife() : 100;
    public int GetCurrentTurnNumber() => currentTurnNumber;
    public int GetCardsOfType(AffinityType type) => playerManager.GetCards(type);
    public bool HasActiveEnemy() => currentEnemy != null;
    
    public int GetCurrentCards()
    {
        if (currentEnemy == null) return 0;
        return playerManager.GetCards(AffinityType.Fuerza);
    }

    // === MÉTODOS OBSOLETOS PARA RETROCOMPATIBILIDAD CON UI ANTIGUA ===
    [Obsolete("El combate ahora siempre opera bajo la lógica TraditionalRPG.")]
    public CombatMode GetCombatMode() => CombatMode.TraditionalRPG;

    [Obsolete("El tipo de ataque ahora se define por el CombatAction. Este método ya no tiene efecto.")]
    public void SelectAttackType(AffinityType type) { }
}
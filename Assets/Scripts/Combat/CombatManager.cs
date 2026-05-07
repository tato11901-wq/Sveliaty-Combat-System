using UnityEngine;
using System;
using System.Collections.Generic;
using Sveliaty.Passives;

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
    [Range(0, 100)] public float randomCardChance = 50f;

    // Estado del combate ACTUAL
    private EnemyInstance currentEnemy;
    private bool combatEnded = false;
    private int pendingCardSelections = 0;
    private bool isProcessingPostCombat = false;
    private bool isEliteRound = false; 
    private bool isFinalBoss = false; 
    
    public int LastInkReward { get; private set; }

    // Estado del TURNO
    private TurnPhase currentPhase;
    private TurnContext currentTurnContext;
    private int turnsUsedThisCombat = 0;
    private int currentTurnNumber = 0;

    // === EVENTOS PARA LA UI ===
    public event Action<EnemyInstance> OnCombatStart;
    public event Action<int, int, int, float, bool, float, bool> OnAttackResult; // roll, bonus, total, multiplier, isCritical, affinityMult, isFirstStrike
    public event Action<bool, int, AffinityType, int> OnCombatEnd; 
    public event Action<int> OnAttemptsChanged;
    public event Action<int> OnWaitingForCardSelection;
    public event Action<int> OnWaitingForPassiveSelection; // <--- AÑADIDO PARA PASIVAS
    public event Action<int, int, int, int, EnemyInstance> GameOver; 

    // === EVENTOS PARA PASIVAS ===
    public event Action OnCombatStartEvent;
    public event Action<CombatAction, float> OnDamageResolveEvent;
    public event Action<int> OnHitReceivedEvent;
    public event Action OnEnemyDefeatedEvent;
    public event Action OnCombatEndEvent;
    public event Action<TurnContext> OnTurnStartEvent;
    public event Action OnEnemyTurnEvent;
    public event Action OnEnemyHealedEvent; // <--- AÑADIDO
    public event Action<string> OnEnemyActionEvent; // Descripción de la acción del enemigo para el log

    public void SetEliteRound(bool isElite)
    {
        isEliteRound = isElite;
    }

    public void SetFinalBoss(bool isFinal)
    {
        isFinalBoss = isFinal;
    }

    private void Start()
    {
        if (PassiveManager.Instance != null)
        {
            PassiveManager.Instance.RegisterCombatManager(this);
            PassiveManager.Instance.RegisterPlayerManager(playerManager);
        }
    }

    public void StartCombat(EnemyData enemyData, EnemyTier tier, CombatMode mode, float statsMultiplier = 1f, int extraAttempts = 0)
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
        if (statsMultiplier != 1f || extraAttempts != 0) 
            currentEnemy.ApplyStatsMultiplier(statsMultiplier, extraAttempts);
        
        combatEnded = false;
        isProcessingPostCombat = false;
        isEliteRound = false;
        isFinalBoss = false;
        turnsUsedThisCombat = 0;
        currentTurnNumber = 0;
        currentPhase = TurnPhase.TurnEnd; 

        if (playerManager != null && statsManager != null)
        {
            playerManager.SetArmor(Mathf.RoundToInt(statsManager.GetFinalStat(StatType.Armadura, null)));
        }

        // Disparar Evento para pasivas
        OnCombatStartEvent?.Invoke();
        
        // Aplicar maldiciones de PreCombate
        if (curseManager != null)
        {
            curseManager.OnPreCombat(currentEnemy);
        }

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

    /// <summary>
    /// Inicia un combate contra un enemigo élite. El tier (1-4) determina el escalado
    /// de stats desde el SO. No aplica bossStatsMultiplier adicional ya que el
    /// escalado está bakeado en el EliteEnemyData.
    /// </summary>
    public void StartEliteCombat(EliteEnemyData eliteData, int eliteTier, int extraAttempts = 0)
    {
        currentEnemy = new EnemyInstance(eliteData, eliteTier);

        if (extraAttempts > 0)
            currentEnemy.attemptsRemaining += extraAttempts;

        if (Sveliaty.Passives.PassiveManager.Instance != null)
        {
            int modifiedAttempts = Sveliaty.Passives.PassiveManager.Instance.GetModifiedAttempts(0);
            currentEnemy.attemptsRemaining += modifiedAttempts;
        }

        combatEnded          = false;
        isProcessingPostCombat = false;
        isEliteRound         = false;
        isFinalBoss          = false;
        turnsUsedThisCombat  = 0;
        currentTurnNumber    = 0;
        currentPhase         = TurnPhase.TurnEnd;

        if (playerManager != null && statsManager != null)
            playerManager.SetArmor(UnityEngine.Mathf.RoundToInt(statsManager.GetFinalStat(StatType.Armadura, null)));

        OnCombatStartEvent?.Invoke();

        if (curseManager != null)
            curseManager.OnPreCombat(currentEnemy);

        Debug.Log($"COMBATE ÉLITE INICIADO: {eliteData.displayName} (Tier {eliteTier})");
        OnCombatStart?.Invoke(currentEnemy);
    }

    public float GetAffinityMultiplier(AffinityType attackType, out bool fromFirstStrikePassive)
    {
        fromFirstStrikePassive = false;
        if (currentEnemy.enemyData == null || currentEnemy.enemyData.affinityRelations == null) 
        {
            // Sin relaciones de afinidad (no debería ocurrir, pero es seguro retornar 1)
            if (PassiveManager.Instance != null && PassiveManager.Instance.IsFirstHitAlwaysSuperEffective())
            {
                fromFirstStrikePassive = true;
                return PassiveManager.Instance.GetModifiedSuperEffectiveMultiplier(1.5f);
            }
            return 1f;
        }
        foreach (var relation in currentEnemy.enemyData.affinityRelations)
        {
            if (relation.type == attackType)
            {
                bool isFirstHitPassive = PassiveManager.Instance != null && PassiveManager.Instance.IsFirstHitAlwaysSuperEffective();

                if (relation.multiplier == AffinityMultiplier.Weak)
                {
                    float mult = isFirstHitPassive ? 2f : 1.5f;
                    if (isFirstHitPassive) fromFirstStrikePassive = true;
                    return PassiveManager.Instance != null ? PassiveManager.Instance.GetModifiedSuperEffectiveMultiplier(mult) : mult;
                }
                
                if (isFirstHitPassive)
                {
                    fromFirstStrikePassive = true;
                    return PassiveManager.Instance != null ? PassiveManager.Instance.GetModifiedSuperEffectiveMultiplier(1.5f) : 1.5f;
                }

                return relation.multiplier switch
                {
                    AffinityMultiplier.Neutral => 1f,
                    AffinityMultiplier.Strong => 0.5f,
                    AffinityMultiplier.Immune => 0f,
                    _ => 1f
                };
            }
        }
        
        // Si no se encontró en la lista pero tiene el pasivo de primer golpe
        if (PassiveManager.Instance != null && PassiveManager.Instance.IsFirstHitAlwaysSuperEffective())
        {
            fromFirstStrikePassive = true;
            return PassiveManager.Instance.GetModifiedSuperEffectiveMultiplier(1.5f);
        }

        return 1f;
    }

    public int RollDice(int diceCount, int maxValue)
    {
        int total = 0;
        for (int i = 0; i < diceCount; i++) total += UnityEngine.Random.Range(1, maxValue + 1);
        return total;
    }

    public void RegisterAttackResult(int roll, int statBonus, int totalFinal, float multiplier, bool isCritical = false, float affinityMultiplier = 1f, bool isFirstStrike = false)
    {
        if (PassiveManager.Instance != null)
        {
            totalFinal = PassiveManager.Instance.GetModifiedDamageDealt(totalFinal);
        }

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

        OnAttackResult?.Invoke(roll, statBonus, totalFinal, multiplier, isCritical, affinityMultiplier, isFirstStrike);
    }
    
    public void NotifyAttemptsChanged()
    {
        OnAttemptsChanged?.Invoke(currentEnemy.attemptsRemaining);
    }

    public void NotifyHitReceived(int damage)
    {
        OnHitReceivedEvent?.Invoke(damage);
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

        // Aplicar maldiciones de inicio de turno
        if (curseManager != null)
        {
            curseManager.OnTurnStart();
        }

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
        if (PassiveManager.Instance != null && !PassiveManager.Instance.CanUseAbility())
        {
            Debug.Log("[Pasivas] Uso de habilidades bloqueado.");
            return;
        }
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
            currentPhase = TurnPhase.EnemyTurn; // Bloquea la UI para evitar spam de clicks
            StartCoroutine(ProcessEnemyTurnDelayed());
        }
    }

    private System.Collections.IEnumerator ProcessEnemyTurnDelayed()
    {
        // Esperamos 0.6s para que la barra de vida baje visualmente antes de que el enemigo se cure u otra acción
        yield return new WaitForSeconds(0.6f);
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
            int healAmount = Mathf.RoundToInt(currentEnemy.maxRPGHealth * data.healAmount);
            currentEnemy.currentRPGHealth += healAmount;
            if (currentEnemy.currentRPGHealth > currentEnemy.maxRPGHealth)
                currentEnemy.currentRPGHealth = currentEnemy.maxRPGHealth;
            actionDescription = $"Se cura {healAmount} de vida. (HP: {currentEnemy.currentRPGHealth})";
            Debug.Log($"El enemigo se curó {healAmount} de vida.");
            OnEnemyHealedEvent?.Invoke();
        }
        else if (randomVal < data.healChance + data.armorChance)
        {
            currentEnemy.activeArmor += data.armorAmount;
            actionDescription = $"Gana {data.armorAmount} de Armadura. (Total: {currentEnemy.activeArmor})";
            Debug.Log($"El enemigo ganó {data.armorAmount} armadura. Armadura actual: {currentEnemy.activeArmor}");
        }
        else if (randomVal < data.healChance + data.armorChance + data.thornsChance)
        {
            currentEnemy.activeThorns = data.thornsAmount;
            actionDescription = $"Activa Espinas ({data.thornsAmount * 100:F0}% daño reflejado).";
            Debug.Log($"El enemigo activó Espinas ({data.thornsAmount * 100}% de daño reflejado).");
        }
        else if (randomVal < data.healChance + data.armorChance + data.thornsChance + data.speedChance)
        {
            currentEnemy.activeSpeedEvasion = data.speedAmount;
            actionDescription = $"Activa Velocidad. Probabilidad de evadir el próximo ataque: {data.speedAmount * 100:F0}%.";
            Debug.Log($"El enemigo activó Velocidad ({data.speedAmount * 100}% evasión).");
        }
        else
        {
            actionDescription = "Decide no hacer nada.";
            Debug.Log($"El enemigo decidió no hacer nada este turno.");
        }

        OnEnemyActionEvent?.Invoke(actionDescription);
        Debug.Log("--- FIN DEL TURNO (Ronda Completada) ---");
        
        currentPhase = TurnPhase.TurnEnd; // Desbloqueamos la UI para el próximo turno
    }

    public void EndCombat(bool victory, int finalScore, float lastMultiplier)
    {
        if (isProcessingPostCombat) return;

        isProcessingPostCombat = true;
        combatEnded = true;

        OnCombatEndEvent?.Invoke();

        if (curseManager != null)
        {
            curseManager.OnPostCombat(victory);
        }

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

            // === CÁLCULO DE RECOMPENSA DE TINTA (Según GDD) ===
            int inkReward = 15;
            switch(currentEnemy.enemyTierData.enemyTier)
            {
                case EnemyTier.Tier_1: inkReward = 15; break;
                case EnemyTier.Tier_2: inkReward = 25; break;
                case EnemyTier.Tier_3: inkReward = 40; break;
            }

            // Escala con el multiplicador de stats del enemigo (si la vida fue multiplicada)
            float statsMultiplier = (float)currentEnemy.maxRPGHealth / currentEnemy.enemyTierData.RPGLife;
            if (statsMultiplier > 0f) inkReward = Mathf.RoundToInt(inkReward * statsMultiplier);

            // Enemigos élite dan 50% más de tinta
            if (currentEnemy.enemyData.isSpirit) inkReward = Mathf.RoundToInt(inkReward * 1.5f);

            // Bonus por explotar debilidad (rendimiento en combate)
            bool wasSuperEffective = lastMultiplier > 1f;
            if (wasSuperEffective) inkReward += 10;
            
            if (PassiveManager.Instance != null)
            {
                inkReward = PassiveManager.Instance.GetModifiedInkReward(inkReward, isEliteRound);
            }

            if (curseManager != null && curseManager.HasRewardBlock())
            {
                inkReward = 0;
                Debug.Log("[CombatManager] Recompensa de tinta bloqueada por maldición.");
            }
            
            LastInkReward = inkReward;
            playerManager.AddInk(inkReward);

            int randomCardsToGive = 0;
            int choiceCardsToGive = 0;
            
            bool isElite = currentEnemy.IsElite;
            EnemyTier tier = currentEnemy.enemyTierData.enemyTier;

            if (isElite)
            {
                // Élite: 5 random, +1 de elección si fue con afinidad
                randomCardsToGive = 5;
                if (wasSuperEffective) choiceCardsToGive = 1;
            }
            else if (tier == EnemyTier.Tier_2 || tier == EnemyTier.Tier_3)
            {
                // Tier 2 y 3: 2 cartas totales
                if (wasSuperEffective)
                {
                    choiceCardsToGive = 1;
                    randomCardsToGive = 1;
                }
                else
                {
                    randomCardsToGive = 2;
                }
            }
            else // Tier 1
            {
                if (wasSuperEffective)
                {
                    choiceCardsToGive = 1;
                    randomCardsToGive = 1;
                }
                else
                {
                    float prob = PassiveManager.Instance != null ? PassiveManager.Instance.GetModifiedCardRewardProb(randomCardChance) : randomCardChance;
                    if (UnityEngine.Random.Range(0f, 100f) < prob)
                    {
                        randomCardsToGive = 1;
                    }
                }
            }

            // Aplicar modificadores de pasivas a las cantidades finales
            if (PassiveManager.Instance != null)
            {
                PassiveManager.Instance.GetModifiedCardRewardAmount(ref randomCardsToGive, ref choiceCardsToGive, wasSuperEffective);
            }

            if (curseManager != null && curseManager.HasRewardBlock())
            {
                randomCardsToGive = 0;
                choiceCardsToGive = 0;
                Debug.Log("[CombatManager] Recompensas de cartas bloqueadas por maldición.");
            }

            // Entregar cartas aleatorias
            if (randomCardsToGive > 0)
            {
                for (int i = 0; i < randomCardsToGive; i++)
                {
                    rewardCard = GetRandomAffinityType();
                    playerManager.AddCards(rewardCard, 1);
                }
                if (wasSuperEffective) OnEnemyActionEvent?.Invoke($"¡DEBILIDAD EXPLOTADA! Ganas {randomCardsToGive} carta(s) aleatoria(s).");
                Debug.Log($"Otorgadas {randomCardsToGive} cartas aleatorias.");
            }

            // Entregar elecciones
            pendingCardSelections = choiceCardsToGive;
            Debug.Log($"[CombatManager] Victoria procesada. pendingCardSelections={pendingCardSelections}, isSpirit={currentEnemy.enemyData.isSpirit}, wasSuperEffective={wasSuperEffective}");
            
            if (isEliteRound && OnWaitingForPassiveSelection != null)
            {
                // Solo pausar para elegir pasiva si la UI está en escena (hay suscriptores)
                Debug.Log("[CombatManager] Élite de ronda derrotado. Disparando selección de pasiva.");
                OnWaitingForPassiveSelection?.Invoke(finalScore);
            }
            else
            {
                ContinueToCardSelection(finalScore);
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
                
                // Si es el jefe final, el daño de fallo es letal instantáneamente
                if (isFinalBoss)
                {
                    damage = playerManager.GetCurrentLife();
                    Debug.Log("[CombatManager] ¡Derrota ante el Jefe Final! Daño letal aplicado.");
                }

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
        if (pendingCardSelections <= 0) return;
        playerManager.AddCards(selectedCard, 1);
        pendingCardSelections--;
        
        if (pendingCardSelections <= 0)
        {
            OnCombatEnd?.Invoke(true, playerManager.GetScore(), selectedCard, 0);
        }
        else
        {
            // Aún quedan elecciones, abrimos de nuevo el selector
            OnWaitingForCardSelection?.Invoke(playerManager.GetScore());
        }
    }

    public void SelectPassive(PassiveSkill selectedPassive)
    {
        if (PassiveManager.Instance != null && selectedPassive != null)
        {
            PassiveManager.Instance.EquipPassive(selectedPassive);
        }
        ContinueToCardSelection(playerManager.GetScore());
    }

    public void ContinueToCardSelection(int score)
    {
        Debug.Log($"[CombatManager] ContinueToCardSelection. pendingCardSelections={pendingCardSelections}, score={score}");
        
        if (PassiveManager.Instance != null && !PassiveManager.Instance.CanGainCards())
        {
            pendingCardSelections = 0;
            Debug.Log("[CombatManager] El jugador no puede ganar más cartas. Omitiendo selección.");
        }

        if (pendingCardSelections > 0)
        {
            Debug.Log("[CombatManager] Disparando OnWaitingForCardSelection");
            OnWaitingForCardSelection?.Invoke(score);
        }
        else 
        {
            Debug.Log("[CombatManager] Disparando OnCombatEnd (victoria)");
            OnCombatEnd?.Invoke(true, score, default, 0); 
        }
    }

    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])System.Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    public int CalculateScorePerCombat(float multiplier)
    {
        int score;

        // Los élites dan score extra proporcional a su tier (T1=4, T2=5, T3=6, T4=7)
        if (currentEnemy.IsElite)
        {
            score = 3 + currentEnemy.eliteTierLevel;
        }
        else
        {
            score = 0;
            if (currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_1) score = 1;
            else if (currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_2) score = 2;
            else if (currentEnemy.enemyTierData.enemyTier == EnemyTier.Tier_3) score = 3;
        }

        if (multiplier >= 1.5f) score += 1;
        return score;
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
    
    public int PendingCardSelections => pendingCardSelections;

    // === MÉTODOS OBSOLETOS PARA RETROCOMPATIBILIDAD CON UI ANTIGUA ===
    [Obsolete("El combate ahora siempre opera bajo la lógica TraditionalRPG.")]
    public CombatMode GetCombatMode() => CombatMode.TraditionalRPG;

    [Obsolete("El tipo de ataque ahora se define por el CombatAction. Este método ya no tiene efecto.")]
    public void SelectAttackType(AffinityType type) { }
}

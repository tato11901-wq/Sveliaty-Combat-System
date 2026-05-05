using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Gestor central de la interfaz V2. 
    /// Su única responsabilidad es suscribirse a los eventos del núcleo de combate
    /// y delegar la información a los sub-módulos visuales.
    /// </summary>
    public class CombatUIController : MonoBehaviour
    {
        [Header("Core References")]
        public CombatManager combatManager;
        public BossRushManager bossRushManager;
        public PlayerManager playerManager;
        public AbilityManager abilityManager;

        [Header("UI Modules")]
        public TopBarUI topBarUI;
        public BottomStatsUI bottomStatsUI;
        public CardInteractionUI cardInteractionUI;
        public EnemyVisualsUI enemyVisualsUI;
        public CombatLogUI combatLogUI;
        public CurseChoiceUIV2 curseChoiceUI;
        public RewardChoiceUIV2 rewardChoiceUI;

        public GameObject victoryScreen;
        public GameObject defeatScreen;
        public TextMeshProUGUI resultsText;
        public TextMeshProUGUI defeatMessageText;

        private bool currentRunIsGameOver = false;

        private void OnEnable()
        {
            if (combatManager == null) return;

            // Suscripciones al motor de combate
            combatManager.OnCombatStart      += HandleCombatStart;
            combatManager.OnTurnStartEvent    += HandleTurnStart;
            combatManager.OnEnemyTurnEvent    += HandleEnemyTurn;
            combatManager.OnEnemyHealedEvent  += HandleEnemyHealed;
            combatManager.OnEnemyActionEvent  += HandleEnemyAction;
            combatManager.OnHitReceivedEvent  += HandleHitReceived;
            combatManager.OnAttackResult      += HandleAttackResult;
            combatManager.OnCombatEnd         += HandleCombatEnd;
            combatManager.OnAttemptsChanged   += HandleAttemptsChanged;
            combatManager.GameOver            += HandleGameOver;
            
            // Suscripciones a la progresión
            if (bossRushManager != null)
            {
                bossRushManager.OnProgressionUpdate += HandleProgressionUpdate;
            }

            if (playerManager != null)
            {
                playerManager.OnCardsChanged += HandleCardsChanged;
                playerManager.OnInkChanged   += HandleInkChanged;
            }

            victoryScreen?.SetActive(false);
            defeatScreen?.SetActive(false);

            // Asegurar que el panel de maldiciones y recompensas estén activos para que se suscriban a los eventos
            if (curseChoiceUI != null && !curseChoiceUI.gameObject.activeSelf)
            {
                curseChoiceUI.gameObject.SetActive(true);
            }
            if (rewardChoiceUI != null && !rewardChoiceUI.gameObject.activeSelf)
            {
                rewardChoiceUI.gameObject.SetActive(true);
            }

            // IMPORTANTE: Si ya hay un combate activo (ej: al reiniciar escena), forzar inicialización
            if (combatManager.HasActiveEnemy())
            {
                HandleCombatStart(combatManager.GetCurrentEnemy());
            }
        }

        private void OnDisable()
        {
            if (combatManager == null) return;

            combatManager.OnCombatStart      -= HandleCombatStart;
            combatManager.OnTurnStartEvent    -= HandleTurnStart;
            combatManager.OnEnemyTurnEvent    -= HandleEnemyTurn;
            combatManager.OnEnemyHealedEvent  -= HandleEnemyHealed;
            combatManager.OnEnemyActionEvent  -= HandleEnemyAction;
            combatManager.OnHitReceivedEvent  -= HandleHitReceived;
            combatManager.OnAttackResult      -= HandleAttackResult;
            combatManager.OnCombatEnd         -= HandleCombatEnd;
            combatManager.OnAttemptsChanged   -= HandleAttemptsChanged;
            combatManager.GameOver            -= HandleGameOver;

            if (bossRushManager != null)
            {
                bossRushManager.OnProgressionUpdate -= HandleProgressionUpdate;
            }

            if (playerManager != null)
            {
                playerManager.OnCardsChanged -= HandleCardsChanged;
                playerManager.OnInkChanged   -= HandleInkChanged;
            }
        }

        private void HandleCombatStart(EnemyInstance enemy)
        {
            currentRunIsGameOver = false;
            victoryScreen?.SetActive(false);
            defeatScreen?.SetActive(false);

            UpdateTopBarEnemyInfo();
            bottomStatsUI?.UpdateHealth(combatManager.GetPlayerLife(), combatManager.GetPlayerMaxLife());
            bottomStatsUI?.UpdateAttempts(enemy.attemptsRemaining);
            cardInteractionUI?.UpdateDeckCounts(playerManager);
            UpdateBottomCardStats();
            bottomStatsUI?.UpdateInk(playerManager.GetInk());
            enemyVisualsUI?.SetupEnemy(enemy);
            combatLogUI?.LogCombatStart(enemy.enemyData.displayName);
            
            // Forzar actualización inicial de la línea de tiempo
            if (bossRushManager != null)
            {
                topBarUI?.UpdateTimeline(bossRushManager.GetUpcomingNodes(5));
            }
        }

        private void HandleTurnStart(TurnContext ctx)
        {
            // El jugador recupera el control, actualizamos los mazos
            cardInteractionUI?.UpdateDeckCounts(playerManager);
            UpdateTopBarEnemyInfo();
            combatLogUI?.ClearTurnLog();
        }

        private void HandleEnemyTurn()
        {
            // La acción específica llegará por HandleEnemyAction
            UpdateTopBarEnemyInfo();
        }

        private void HandleEnemyHealed()
        {
            bottomStatsUI?.PlayEnemyHealAnimation();
        }

        private void HandleEnemyAction(string actionDescription)
        {
            string enemyName = combatManager.GetCurrentEnemy()?.enemyData.displayName ?? "Enemigo";
            combatLogUI?.LogEnemyAction($"{enemyName}: {actionDescription}");
            UpdateTopBarEnemyInfo(); // Para reflejar curas o armadura
        }

        private void HandleHitReceived(int damage)
        {
            combatLogUI?.LogDamageReceived(damage);
            bottomStatsUI?.PlayDamageAnimation();
            bottomStatsUI?.UpdateHealth(combatManager.GetPlayerLife(), combatManager.GetPlayerMaxLife());
        }

        private void HandleAttackResult(int roll, int bonus, int total, float multiplier, bool isCritical, float affinityMultiplier)
        {
            enemyVisualsUI?.PlayDamageAnimation();
            combatLogUI?.LogAttackResult(
                combatManager.GetCurrentEnemy()?.enemyData.displayName ?? "Enemigo",
                roll, bonus, total, multiplier, isCritical, affinityMultiplier
            );
            UpdateTopBarEnemyInfo();
        }

        private void HandleAttemptsChanged(int attemptsRemaining)
        {
            bottomStatsUI?.UpdateAttempts(attemptsRemaining);
        }

        private void HandleCombatEnd(bool victory, int finalScore, AffinityType rewardCard, int lifeLost)
        {
            if (victory)
            {
                combatLogUI?.LogCombatResult(true, finalScore);
                enemyVisualsUI?.PlayDeathAnimation();
                victoryScreen?.SetActive(true);
                if (resultsText != null)
                    resultsText.text = $"Puntuación: {finalScore}\nObtuviste: {rewardCard}";
            }
            else
            {
                combatLogUI?.LogCombatResult(false, finalScore);
                defeatScreen?.SetActive(true);
                bottomStatsUI?.PlayDamageAnimation();

                if (defeatMessageText != null)
                {
                    string msg = $"<size=120%>DERROTA</size>\n\nPuntuación: {finalScore}";

                    if (lifeLost > 0)
                        msg += $"\n\nPerdiste <color=red>{lifeLost}</color> de vida.";
                    else if (lifeLost == -1)
                        msg += "\n\n<b><color=green>\u00a1DAÑO DENEGADO POR\nEFECTO DE CARTA!</color></b>";
                    else if (lifeLost == -2)
                        msg += "\n\n<b><color=orange>\u00a1MUERTE DENEGADA POR\nEFECTO DE CARTA!</color></b>\n<size=80%>Tu vida se ha fijado en 1.</size>";

                    defeatMessageText.text = msg;
                }
            }

            cardInteractionUI?.HideAllCards();
        }

        private void HandleGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy)
        {
            currentRunIsGameOver = true;
            combatLogUI?.Log($"\u00a1GAME OVER! Derrotado por {defeatedBy?.enemyData.displayName ?? "el enemigo"}");
            defeatScreen?.SetActive(true);
            bottomStatsUI?.PlayDamageAnimation();
            cardInteractionUI?.HideAllCards();

            if (defeatMessageText != null)
            {
                defeatMessageText.text =
                    $"<size=120%>GAME OVER</size>\n\n" +
                    $"Puntuación final: {finalScore}\n\n" +
                    $"Cartas:\n" +
                    $"<color=#FF6B6B>Fuerza: {fuerzaCards}</color>  " +
                    $"<color=#6BCB77>Agilidad: {agilidadCards}</color>  " +
                    $"<color=#74B3CE>Destreza: {destrezaCards}</color>";
            }
        }

        private void HandleProgressionUpdate(int current, int max)
        {
            if (bossRushManager != null)
            {
                topBarUI?.UpdateTimeline(bossRushManager.GetUpcomingNodes(5));
            }
        }

        // ==========================================
        // NAVEGACIÓN POST-COMBATE
        // ==========================================

        public void ContinueRun()
        {
            victoryScreen?.SetActive(false);
            defeatScreen?.SetActive(false);

            // Verificar si hay evento de maldición antes de seguir
            if (combatManager != null && combatManager.ShouldShowCurseEvent())
            {
                // Ir directo a la elección de maldición
                combatManager.TriggerCurseEventFromUI();
            }
            else
            {
                bossRushManager?.ContinueToNextCombat();
            }
        }

        public void GoToMainMenu()
        {
            if (!currentRunIsGameOver && defeatScreen != null && defeatScreen.activeSelf)
            {
                ContinueRun();
                return;
            }

            // Limpiar run en progreso antes de salir
            bossRushManager?.ForceEndRun();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
        }

        private void UpdateTopBarEnemyInfo()
        {
            if (combatManager == null || bossRushManager == null) return;
            
            var currentEnemy = combatManager.GetCurrentEnemy();

            if (topBarUI != null)
            {
                topBarUI.UpdateEnemyInfo(
                    currentEnemy,
                    bossRushManager.GetCurrentNodeIndex(),
                    bossRushManager.GetMaxEnemies()
                );
            }

            if (bottomStatsUI != null && currentEnemy != null)
            {
                bottomStatsUI.UpdateEnemyHealth(currentEnemy.currentRPGHealth, currentEnemy.maxRPGHealth, currentEnemy.activeArmor);
            }
        }

        private void HandleCardsChanged(AffinityType type, int amount)
        {
            UpdateBottomCardStats();
        }

        private void HandleInkChanged(int newInk)
        {
            bottomStatsUI?.UpdateInk(newInk);
        }

        private void UpdateBottomCardStats()
        {
            if (bottomStatsUI != null && playerManager != null)
            {
                int itemFuerza = 0, itemAgilidad = 0, itemDestreza = 0;
                if (combatManager != null && combatManager.statsManager != null)
                {
                    itemFuerza   = Mathf.RoundToInt(combatManager.statsManager.GetItemBonus(StatType.Fuerza));
                    itemAgilidad = Mathf.RoundToInt(combatManager.statsManager.GetItemBonus(StatType.Velocidad));
                    itemDestreza = Mathf.RoundToInt(combatManager.statsManager.GetItemBonus(StatType.Destreza));
                }

                bottomStatsUI.UpdateCardStats(
                    playerManager.GetCards(AffinityType.Fuerza),
                    playerManager.GetCards(AffinityType.Agilidad),
                    playerManager.GetCards(AffinityType.Destreza),
                    itemFuerza,
                    itemAgilidad,
                    itemDestreza
                );
            }
        }
    }
}

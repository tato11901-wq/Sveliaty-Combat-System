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

        [Header("Screens")]
        public GameObject victoryScreen;
        public GameObject defeatScreen;
        public TextMeshProUGUI resultsText;

        private void OnEnable()
        {
            if (combatManager == null) return;

            // Suscripciones al motor de combate
            combatManager.OnCombatStart += HandleCombatStart;
            combatManager.OnTurnStartEvent += HandleTurnStart;
            combatManager.OnEnemyTurnEvent += HandleEnemyTurn;
            combatManager.OnHitReceivedEvent += HandleHitReceived;
            combatManager.OnAttackResult += HandleAttackResult;
            combatManager.OnCombatEnd += HandleCombatEnd;
            combatManager.OnAttemptsChanged += HandleAttemptsChanged;
            
            // Suscripciones a la progresión
            if (bossRushManager != null)
            {
                bossRushManager.OnProgressionUpdate += HandleProgressionUpdate;
            }

            victoryScreen?.SetActive(false);
            defeatScreen?.SetActive(false);
        }

        private void OnDisable()
        {
            if (combatManager == null) return;

            combatManager.OnCombatStart -= HandleCombatStart;
            combatManager.OnTurnStartEvent -= HandleTurnStart;
            combatManager.OnEnemyTurnEvent -= HandleEnemyTurn;
            combatManager.OnHitReceivedEvent -= HandleHitReceived;
            combatManager.OnAttackResult -= HandleAttackResult;
            combatManager.OnCombatEnd -= HandleCombatEnd;
            combatManager.OnAttemptsChanged -= HandleAttemptsChanged;

            if (bossRushManager != null)
            {
                bossRushManager.OnProgressionUpdate -= HandleProgressionUpdate;
            }
        }

        private void HandleCombatStart(EnemyInstance enemy)
        {
            victoryScreen?.SetActive(false);
            defeatScreen?.SetActive(false);

            topBarUI?.UpdateEnemyInfo(enemy);
            bottomStatsUI?.UpdateHealth(combatManager.GetPlayerLife(), combatManager.GetPlayerMaxLife());
            bottomStatsUI?.UpdateAttempts(enemy.attemptsRemaining);
            cardInteractionUI?.UpdateDeckCounts(playerManager);
            
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
            topBarUI?.UpdateEnemyInfo(combatManager.GetCurrentEnemy());
        }

        private void HandleEnemyTurn()
        {
            // El enemigo actuó (ej. se puso armadura)
            topBarUI?.UpdateEnemyInfo(combatManager.GetCurrentEnemy());
        }

        private void HandleHitReceived(int damage)
        {
            bottomStatsUI?.PlayDamageAnimation();
            bottomStatsUI?.UpdateHealth(combatManager.GetPlayerLife(), combatManager.GetPlayerMaxLife());
        }

        private void HandleAttackResult(int roll, int bonus, int total, float multiplier)
        {
            // Actualizar vida del enemigo o mostrar textos de daño flotante aquí
            topBarUI?.UpdateEnemyInfo(combatManager.GetCurrentEnemy());
        }

        private void HandleAttemptsChanged(int attemptsRemaining)
        {
            bottomStatsUI?.UpdateAttempts(attemptsRemaining);
        }

        private void HandleCombatEnd(bool victory, int finalScore, AffinityType rewardCard, int lifeLost)
        {
            if (victory)
            {
                victoryScreen?.SetActive(true);
                if (resultsText != null) resultsText.text = $"Puntuación: {finalScore}\nObtuviste: {rewardCard}";
            }
            else
            {
                defeatScreen?.SetActive(true);
                bottomStatsUI?.PlayDamageAnimation(); // Shake the screen/hearts on death
            }
            
            cardInteractionUI?.HideAllCards();
        }

        private void HandleProgressionUpdate(int current, int max)
        {
            if (bossRushManager != null)
            {
                topBarUI?.UpdateTimeline(bossRushManager.GetUpcomingNodes(5));
            }
        }

        // Métodos públicos para la navegación post-combate
        public void ContinueRun()
        {
            victoryScreen?.SetActive(false);
            bossRushManager?.ContinueToNextCombat();
        }
    }
}

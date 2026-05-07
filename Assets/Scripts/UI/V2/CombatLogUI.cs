using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Módulo de log de combate. Muestra en un panel con scroll todos los eventos
    /// del combate: ataques, daño, buffs del enemigo, resultados, etc.
    /// </summary>
    public class CombatLogUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Texto principal donde se mostrará el resumen del turno")]
        public TextMeshProUGUI turnSummaryText;

        // Variables de estado del turno actual
        private string playerAction = "";
        private string enemyAction = "";
        private string extraInfo = "";

        // === COLORES POR TIPO DE EVENTO ===
        private static readonly string COLOR_DAMAGE   = "#FF6B6B"; // Rojo suave - daño al enemigo
        private static readonly string COLOR_RECEIVED = "#FFB347"; // Naranja - daño recibido
        private static readonly string COLOR_VICTORY  = "#6BCB77"; // Verde - victoria
        private static readonly string COLOR_ENEMY    = "#C77DFF"; // Morado - acción del enemigo
        private static readonly string COLOR_INFO     = "#ADB5BD"; // Gris - información general
        private static readonly string COLOR_CRITICAL = "#4FC3F7"; // Azul brillante - golpe crítico
        private static readonly string COLOR_AFFINITY = "#FF5252"; // Rojo brillante - súper efectivo (afinidad)

        public void LogAttackResult(string abilityName, int roll, int bonus, int total, float multiplier, bool isCritical = false, float affinityMultiplier = 1f, bool isFirstStrike = false)
        {
            // Construir etiquetas de estado
            string tags = "";
            if (affinityMultiplier >= 1.5f)
            {
                if (isFirstStrike)
                {
                    if (affinityMultiplier >= 2f)
                        tags += $" <color={COLOR_AFFINITY}>¡SÚPER EFECTIVO x2 (GOLPE CERTERO)!</color>";
                    else
                        tags += $" <color={COLOR_AFFINITY}>¡SÚPER EFECTIVO (GOLPE CERTERO)!</color>";
                }
                else
                {
                    tags += $" <color={COLOR_AFFINITY}>¡SÚPER EFECTIVO!</color>";
                }
            }
            else if (affinityMultiplier <= 0.5f && affinityMultiplier > 0f)
                tags += $" <color={COLOR_INFO}>(Resistido)</color>";
            else if (affinityMultiplier == 0f)
                tags += $" <color={COLOR_INFO}>(Inmune)</color>";
            
            if (isCritical)
                tags += $" <color={COLOR_CRITICAL}>¡CRÍTICO!</color>";

            playerAction = $"<color={COLOR_DAMAGE}>[TÚ]</color> {abilityName} → Dados: {roll} | Bonus: +{bonus} | Daño Total: {total} (x{multiplier:F1}){tags}";
            UpdateSummary();
        }

        public void LogDamageReceived(int damage)
        {
            extraInfo = $"<color={COLOR_RECEIVED}>[RECIBIDO] -{damage} HP</color>";
            UpdateSummary();
        }

        public void LogEnemyAction(string actionDescription)
        {
            enemyAction = $"<color={COLOR_ENEMY}>[ENEMIGO]</color> {actionDescription}";
            UpdateSummary();
        }

        public void LogCombatStart(string enemyName)
        {
            ClearLog();
            extraInfo = $"<color={COLOR_INFO}>── Combate vs <b>{enemyName}</b> ──</color>";
            UpdateSummary();
        }

        public void LogCombatResult(bool victory, int finalScore)
        {
            if (victory)
                extraInfo = $"<color={COLOR_VICTORY}>[VICTORIA] Score: {finalScore}</color>";
            else
                extraInfo = $"<color={COLOR_RECEIVED}>[DERROTA]</color>";
            
            UpdateSummary();
        }

        public void LogCurseObtained(string curseName)
        {
            extraInfo = $"<color={COLOR_ENEMY}>[MALDICIÓN] Obtuviste: <b>{curseName}</b></color>";
            UpdateSummary();
        }

        public void Log(string message)
        {
            extraInfo = $"<color={COLOR_INFO}>{message}</color>";
            UpdateSummary();
        }

        public void ClearLog()
        {
            playerAction = "";
            enemyAction = "";
            extraInfo = "";
            UpdateSummary();
        }

        public void ClearTurnLog()
        {
            // Se llama al inicio de cada turno del jugador para borrar las acciones del turno anterior
            playerAction = "";
            enemyAction = "";
            extraInfo = "";
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (turnSummaryText == null) return;

            // Construimos el string mostrando solo las líneas que tienen texto
            string finalString = "";

            if (!string.IsNullOrEmpty(extraInfo))
                finalString += extraInfo + "\n";
            
            if (!string.IsNullOrEmpty(playerAction))
                finalString += playerAction + "\n";
                
            if (!string.IsNullOrEmpty(enemyAction))
                finalString += enemyAction;

            turnSummaryText.text = finalString.TrimEnd();
        }
    }
}

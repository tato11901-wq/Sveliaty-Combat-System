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
        [Tooltip("El ScrollRect que contiene el log")]
        public ScrollRect scrollRect;
        [Tooltip("El Transform Content dentro del ScrollRect (con Vertical Layout Group)")]
        public Transform logContent;
        [Tooltip("Prefab de una línea de texto del log (GameObject con TextMeshProUGUI)")]
        public GameObject logLinePrefab;

        [Header("Settings")]
        [Tooltip("Máximo de líneas antes de eliminar las más antiguas")]
        public int maxLines = 50;

        private List<GameObject> logLines = new List<GameObject>();

        // === COLORES POR TIPO DE EVENTO ===
        private static readonly string COLOR_DAMAGE   = "#FF6B6B"; // Rojo suave - daño al enemigo
        private static readonly string COLOR_RECEIVED = "#FFB347"; // Naranja - daño recibido
        private static readonly string COLOR_VICTORY  = "#6BCB77"; // Verde - victoria
        private static readonly string COLOR_ENEMY    = "#C77DFF"; // Morado - acción del enemigo
        private static readonly string COLOR_INFO     = "#ADB5BD"; // Gris - información general
        private static readonly string COLOR_CRITICAL = "#FFD700"; // Dorado - multiplicador alto

        public void LogAttackResult(string abilityName, int roll, int bonus, int total, float multiplier)
        {
            string multStr = multiplier >= 1.5f
                ? $"<color={COLOR_CRITICAL}>x{multiplier:F1}</color>"
                : $"x{multiplier:F1}";

            AddLine($"<color={COLOR_DAMAGE}>[ATAQUE] {abilityName}</color> → " +
                    $"Dado: {roll} + Stat: {bonus} = <b>{total}</b> dmg ({multStr})");
        }

        public void LogDamageReceived(int damage)
        {
            AddLine($"<color={COLOR_RECEIVED}>[RECIBIDO] -{damage} HP</color>");
        }

        public void LogEnemyAction(string actionDescription)
        {
            AddLine($"<color={COLOR_ENEMY}>[ENEMIGO] {actionDescription}</color>");
        }

        public void LogCombatStart(string enemyName)
        {
            AddSeparator();
            AddLine($"<color={COLOR_INFO}>── Combate vs <b>{enemyName}</b> ──</color>");
        }

        public void LogCombatResult(bool victory, int finalScore)
        {
            if (victory)
                AddLine($"<color={COLOR_VICTORY}>[VICTORIA] Score: {finalScore}</color>");
            else
                AddLine($"<color={COLOR_RECEIVED}>[DERROTA]</color>");
            AddSeparator();
        }

        public void LogCurseObtained(string curseName)
        {
            AddLine($"<color={COLOR_ENEMY}>[MALDICIÓN] Obtuviste: <b>{curseName}</b></color>");
        }

        public void Log(string message)
        {
            AddLine($"<color={COLOR_INFO}>{message}</color>");
        }

        // ==================
        // INTERNOS
        // ==================

        private void AddLine(string text)
        {
            if (logContent == null || logLinePrefab == null) return;

            // Eliminar líneas viejas si supera el límite
            if (logLines.Count >= maxLines)
            {
                Destroy(logLines[0]);
                logLines.RemoveAt(0);
            }

            GameObject lineObj = Instantiate(logLinePrefab, logContent);
            TextMeshProUGUI tmp = lineObj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;

            logLines.Add(lineObj);

            // Auto-scroll al fondo en el siguiente frame
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void AddSeparator()
        {
            AddLine("<color=#495057>────────────────────</color>");
        }

        public void ClearLog()
        {
            foreach (var line in logLines) Destroy(line);
            logLines.Clear();
        }
    }
}

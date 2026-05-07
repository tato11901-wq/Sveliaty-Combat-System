using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Controlador de UI de la tienda. 
    /// Solo muestra datos y delega acciones al ShopManager.
    /// No contiene lógica de negocio.
    /// </summary>
    public class ShopUIController : MonoBehaviour
    {
        [Header("Referencia al gestor")]
        public ShopManager shopManager;
        public PlayerManager playerManager;

        [Header("Slots UI")]
        [Tooltip("Panel raíz de cada slot. Debe haber tantos como ShopSettings.slotCount (default 3).")]
        public ShopSlotUI[] slotPanels;

        [Header("Reroll")]
        public Button  rerollButton;
        public TextMeshProUGUI rerollCostText;

        [Header("Recursos del jugador")]
        public TextMeshProUGUI inkText;
        public TextMeshProUGUI fuerzaCardsText;
        public TextMeshProUGUI agilidadCardsText;
        public TextMeshProUGUI destrezaCardsText;

        [Header("Stats del jugador (opcional)")]
        [Tooltip("Referencia al PlayerStatsManager para mostrar el estado actual de las stats.")]
        public PlayerStatsManager statsManager;

        [Tooltip("TMP donde se muestran todas las estadísticas actuales del jugador.")]
        public TextMeshProUGUI playerStatsText;

        [Header("Feedback")]
        public TextMeshProUGUI feedbackText;
        public float           feedbackDisplayTime = 2.5f;

        [Header("Panel raíz")]
        public GameObject shopPanel;

        private Coroutine feedbackRoutine;

        // ── Ciclo de vida ───────────────────────────────────────────────────
        // IMPORTANTE: las suscripciones van en Awake/OnDestroy, NO en OnEnable/OnDisable.
        // El ShopPanel empieza desactivado, por lo que OnEnable nunca correría
        // antes de que ShopManager dispare los eventos al abrir la tienda.

        private void Awake()
        {
            // Auto-búsqueda de referencias no asignadas en el Inspector
            if (statsManager == null)
                statsManager = FindObjectOfType<PlayerStatsManager>();

            if (statsManager == null)
                Debug.LogWarning("[ShopUIController] statsManager no asignado y no encontrado en escena. Las stats no se mostrarán.");

            if (playerStatsText == null)
                Debug.LogWarning("[ShopUIController] playerStatsText no asignado. Asigna un TMP en el Inspector para ver las stats.");

            if (shopManager != null)
            {
                shopManager.OnSlotsGenerated    += HandleSlotsGenerated;
                shopManager.OnSlotPurchased     += HandleSlotPurchased;
                shopManager.OnPurchaseFailed    += HandlePurchaseFailed;
                shopManager.OnRerollCostUpdated += HandleRerollCostUpdated;
            }

            if (playerManager != null)
            {
                playerManager.OnInkChanged   += UpdateInkDisplay;
                playerManager.OnCardsChanged += UpdateCardsDisplay;
            }


            if (feedbackText != null) feedbackText.text = "";
        }

        private void OnDestroy()
        {
            if (shopManager != null)
            {
                shopManager.OnSlotsGenerated    -= HandleSlotsGenerated;
                shopManager.OnSlotPurchased     -= HandleSlotPurchased;
                shopManager.OnPurchaseFailed    -= HandlePurchaseFailed;
                shopManager.OnRerollCostUpdated -= HandleRerollCostUpdated;
            }

            if (playerManager != null)
            {
                playerManager.OnInkChanged   -= UpdateInkDisplay;
                playerManager.OnCardsChanged -= UpdateCardsDisplay;
            }
        }

        // ── Eventos del ShopManager ────────────────────────────────────────

        private void HandleSlotsGenerated(List<ShopSlot> slots)
        {
            // Activar el panel automáticamente al recibir los slots
            if (shopPanel != null) shopPanel.SetActive(true);

            RefreshResourceDisplay();

            for (int i = 0; i < slotPanels.Length; i++)
            {
                if (i < slots.Count)
                    slotPanels[i].Setup(slots[i], this);
                else
                    slotPanels[i].SetEmpty();
            }
        }

        private void HandleSlotPurchased(ShopSlot slot)
        {
            RefreshResourceDisplay();
            RefreshStatsDisplay();
            string msg = slot.Type == ShopSlot.SlotType.Item
                ? $"✓ {slot.Item.itemName} comprado."
                : $"✓ {slot.Ability.abilityName} (Tier {slot.AbilityTier}) desbloqueado.";
            ShowFeedback(msg, Color.green);
        }

        private void HandlePurchaseFailed(string reason)
        {
            ShowFeedback($"✗ {reason}", Color.red);
        }

        private void HandleRerollCostUpdated(int newCost)
        {
            if (rerollCostText != null)
                rerollCostText.text = newCost == 0 ? "GRATIS" : $"{newCost} Tinta";
        }

        // ── Acciones de botones ────────────────────────────────────────────

        public void OnRerollClicked()
        {
            shopManager?.TryReroll();
        }

        public void OnSlotPurchaseClicked(ShopSlot slot)
        {
            shopManager?.TryPurchase(slot);
        }

        public void OnCloseShopClicked()
        {
            shopPanel?.SetActive(false);
            shopManager?.CloseShop();
        }

        // ── Display de recursos ────────────────────────────────────────────

        private void RefreshResourceDisplay()
        {
            if (playerManager == null) return;
            UpdateInkDisplay(playerManager.GetInk());
            UpdateCardsDisplay(AffinityType.Fuerza,   playerManager.GetCards(AffinityType.Fuerza));
            UpdateCardsDisplay(AffinityType.Agilidad, playerManager.GetCards(AffinityType.Agilidad));
            UpdateCardsDisplay(AffinityType.Destreza, playerManager.GetCards(AffinityType.Destreza));
            RefreshStatsDisplay();
        }

        private void RefreshStatsDisplay()
        {
            if (playerStatsText == null || statsManager == null) return;

            float fuerza   = statsManager.GetFinalStat(StatType.Fuerza,      null);
            float velocidad= statsManager.GetFinalStat(StatType.Velocidad,   null);
            float destreza = statsManager.GetFinalStat(StatType.Destreza,    null);
            float armadura = statsManager.GetFinalStat(StatType.Armadura,    null);
            float critico  = statsManager.GetFinalStat(StatType.ProbCritico, null);
            float roboVida = statsManager.GetFinalStat(StatType.RoboVida,    null);

            // Separar la contribución de ítems (sin cartas) para mostrar el desglose
            float itemFuerza    = statsManager.GetItemBonus(StatType.Fuerza);
            float itemVelocidad = statsManager.GetItemBonus(StatType.Velocidad);
            float itemDestreza  = statsManager.GetItemBonus(StatType.Destreza);
            float itemArmadura  = statsManager.GetItemBonus(StatType.Armadura);
            float itemCritico   = statsManager.GetItemBonus(StatType.ProbCritico) - 5f; // base = 5
            float itemRoboVida  = statsManager.GetItemBonus(StatType.RoboVida);

            playerStatsText.text =
                $"<b>Estadísticas actuales</b>\n" +
                FormatStat("⚔ Fuerza",    fuerza,    itemFuerza)    + "\n" +
                FormatStat("💨 Velocidad", velocidad, itemVelocidad) + "\n" +
                FormatStat("🎯 Destreza",  destreza,  itemDestreza)  + "\n" +
                FormatStat("🛡 Armadura",  armadura,  itemArmadura)  + "\n" +
                FormatStat("💥 Crítico",   critico,   itemCritico,   "%") + "\n" +
                FormatStat("❤ Robo Vida", roboVida,  itemRoboVida);
        }

        /// <summary>
        /// Formatea una línea de stat mostrando el valor total y el delta de ítems entre paréntesis.
        /// Ejemplo: "⚔ Fuerza  12  (+5 ítems)"
        /// </summary>
        private string FormatStat(string label, float total, float itemDelta, string suffix = "")
        {
            string totalStr = total % 1 == 0 ? ((int)total).ToString() : total.ToString("F1");
            string deltaStr = "";

            if (itemDelta != 0)
            {
                string sign = itemDelta > 0 ? "+" : "";
                string color = itemDelta > 0 ? "#7FFF7F" : "#FF7F7F";
                string itemVal = itemDelta % 1 == 0 ? ((int)itemDelta).ToString() : itemDelta.ToString("F1");
                deltaStr = $"  <color={color}>({sign}{itemVal}{suffix} ítems)</color>";
            }

            return $"{label}:  <b>{totalStr}{suffix}</b>{deltaStr}";
        }

        private void UpdateInkDisplay(int newInk)
        {
            if (inkText != null) inkText.text = $"Tinta: {newInk}";
        }

        private void UpdateCardsDisplay(AffinityType type, int amount)
        {
            // Calcular el bonus de ítems para el stat correspondiente a esta rama
            float itemBonus = 0f;
            if (statsManager != null)
            {
                StatType correspondingStat = type switch
                {
                    AffinityType.Fuerza   => StatType.Fuerza,
                    AffinityType.Agilidad => StatType.Velocidad,
                    AffinityType.Destreza => StatType.Destreza,
                    _                     => StatType.Fuerza
                };
                itemBonus = statsManager.GetItemBonus(correspondingStat);
            }

            // Formatear el texto: "5" si no hay bonus, "5 (+3 ítems)" si hay bonus de ítems
            string text = amount.ToString();
            if (itemBonus != 0)
            {
                string sign  = itemBonus > 0 ? "+" : "";
                string color = itemBonus > 0 ? "#7FFF7F" : "#FF7F7F";
                string val   = itemBonus % 1 == 0 ? ((int)itemBonus).ToString() : itemBonus.ToString("F1");
                text += $" <color={color}>({sign}{val})</color>";
            }

            switch (type)
            {
                case AffinityType.Fuerza:
                    if (fuerzaCardsText   != null) fuerzaCardsText.text   = text; break;
                case AffinityType.Agilidad:
                    if (agilidadCardsText != null) agilidadCardsText.text = text; break;
                case AffinityType.Destreza:
                    if (destrezaCardsText != null) destrezaCardsText.text = text; break;
            }
        }

        // ── Feedback ───────────────────────────────────────────────────────

        private void ShowFeedback(string message, Color color)
        {
            if (feedbackText == null) return;
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackText.text  = message;
            feedbackText.color = color;
            feedbackRoutine = StartCoroutine(ClearFeedbackAfter(feedbackDisplayTime));
        }

        private System.Collections.IEnumerator ClearFeedbackAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (feedbackText != null) feedbackText.text = "";
        }
    }
}

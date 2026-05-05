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
        }

        private void UpdateInkDisplay(int newInk)
        {
            if (inkText != null) inkText.text = $"Tinta: {newInk}";
        }

        private void UpdateCardsDisplay(AffinityType type, int amount)
        {
            switch (type)
            {
                case AffinityType.Fuerza:
                    if (fuerzaCardsText != null)   fuerzaCardsText.text   = amount.ToString(); break;
                case AffinityType.Agilidad:
                    if (agilidadCardsText != null) agilidadCardsText.text = amount.ToString(); break;
                case AffinityType.Destreza:
                    if (destrezaCardsText != null) destrezaCardsText.text = amount.ToString(); break;
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

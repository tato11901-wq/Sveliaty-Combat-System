using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Componente de UI para un único slot de la tienda.
    /// Muestra nombre, descripción, precio y el botón de compra.
    /// Se configura desde ShopUIController.
    /// </summary>
    public class ShopSlotUI : MonoBehaviour
    {
        [Header("Elementos comunes")]
        public Image           iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI priceText;
        public Button          buyButton;
        public TextMeshProUGUI buyButtonText;   // TMP del label del botón de compra

        [Header("Panel Background")]
        [Tooltip("Image de fondo del slot. Cambia de color seg\u00fan el tipo (\u00edtem o habilidad).")]
        public Image panelBackground;

        [Header("Overlay de comprado")]
        [Tooltip("Image que se activa encima del slot al comprarlo. " +
                 "Debe tener Raycast Target = true para bloquear interacci\u00f3n.")]
        public Image purchasedOverlay;   // Image semitransparente sobre el slot

        [Header("Badge de tipo (opcional)")]
        public TextMeshProUGUI typeBadgeText;       // "ÍTEM" o "HABILIDAD"
        public Image           typeBadgeBackground;

        [Header("Colores de tipo")]
        public Color itemColor    = new Color(0.2f, 0.6f, 1f);
        public Color abilityColor = new Color(1f, 0.6f, 0.2f);

        private ShopSlot          _slot;
        private ShopUIController  _controller;

        // ── Setup ──────────────────────────────────────────────────────────

        public void Setup(ShopSlot slot, ShopUIController controller)
        {
            _slot       = slot;
            _controller = controller;

            gameObject.SetActive(true);
            if (purchasedOverlay != null) purchasedOverlay.enabled = false;
            if (buyButtonText != null) buyButtonText.text = "Comprar";

            buyButton?.onClick.RemoveAllListeners();

            // Colorear el fondo del panel según el tipo de slot
            if (panelBackground != null)
                panelBackground.color = slot.Type == ShopSlot.SlotType.Item ? itemColor : abilityColor;

            if (slot.Type == ShopSlot.SlotType.Item)
                SetupItemSlot(slot);
            else
                SetupAbilitySlot(slot);

            buyButton?.onClick.AddListener(OnBuyClicked);
            
            // Añadir efecto de hover si no existe
            if (buyButton != null && buyButton.GetComponent<GenericHoverEffect>() == null)
            {
                buyButton.gameObject.AddComponent<GenericHoverEffect>();
            }
        }

        private void SetupItemSlot(ShopSlot slot)
        {
            if (iconImage != null)
                iconImage.sprite = slot.Item.icon;

            if (nameText != null)
                nameText.text = slot.Item.itemName;

            if (descriptionText != null)
                descriptionText.text = slot.Item.description;

            if (priceText != null)
                priceText.text = $"{slot.InkPrice} Tinta";

            SetBadge("ÍTEM", itemColor);
        }


        private void SetupAbilitySlot(ShopSlot slot)
        {
            if (iconImage != null)
                iconImage.sprite = slot.Ability.icon;

            if (nameText != null)
                nameText.text = $"{slot.Ability.abilityName}  T{slot.AbilityTier}";

            if (descriptionText != null)
                descriptionText.text = slot.Ability.description;

            if (priceText != null)
                priceText.text = $"{slot.CardPrice} Cartas {slot.Ability.affinityType}";

            SetBadge($"HABILIDAD · {slot.Ability.affinityType}", abilityColor);
        }

        private void SetBadge(string label, Color color)
        {
            if (typeBadgeText != null)       typeBadgeText.text = label;
            if (typeBadgeBackground != null) typeBadgeBackground.color = color;
        }

        public void SetEmpty()
        {
            gameObject.SetActive(false);
        }

        // ── Interacción ────────────────────────────────────────────────────

        private void OnBuyClicked()
        {
            if (_slot == null || _controller == null) return;
            _controller.OnSlotPurchaseClicked(_slot);

            if (_slot.IsPurchased)
                ShowPurchasedState();
        }

        private void ShowPurchasedState()
        {
            // Activar la imagen encima del slot.
            // Con Raycast Target = true en la Image, bloquea todos los clics
            // a los elementos que están debajo (botón de compra incluido).
            if (purchasedOverlay != null) purchasedOverlay.enabled = true;

            // Cambiar el texto del botón para dar feedback visual claro
            if (buyButtonText != null) buyButtonText.text = "¡Comprado!";
        }
    }
}

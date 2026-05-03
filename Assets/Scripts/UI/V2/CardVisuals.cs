using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Script que vive en el Prefab de carta.
    /// Recibe un AbilityData y rellena todos los visuales automáticamente.
    /// Los 3 fondos de rama se configuran una sola vez en el Inspector del Prefab.
    /// </summary>
    public class CardVisuals : MonoBehaviour
    {
        [Header("Card Backgrounds (configurar en el Prefab)")]
        public Sprite fuerzaBackground;
        public Sprite agilidadBackground;
        public Sprite destrezaBackground;

        [Header("Visual References (hijos del Prefab)")]
        public Image cardBackground;   // La imagen del fondo de la carta
        public Image abilityIcon;      // El sprite único de la habilidad (su ilustración)
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI costText;
        [Tooltip("Solo para deck selectors: muestra cuántas cartas de esta rama tienes")]
        public TextMeshProUGUI countText;

        /// <summary>
        /// Rellena la carta con los datos del ScriptableObject.
        /// Llamar una sola vez justo después de Instantiate().
        /// </summary>
        public void Setup(AbilityData ability)
        {
            if (ability == null) return;

            // Fondo según la rama de la habilidad
            if (cardBackground != null)
            {
                cardBackground.sprite = GetBackgroundForAffinity(ability.affinityType);
            }

            // Sprite / ilustración propia de la habilidad
            if (abilityIcon != null)
            {
                abilityIcon.sprite = ability.icon;
                // Si no tiene icono asignado en el SO, ocultar la imagen
                abilityIcon.enabled = ability.icon != null;
            }

            // Textos
            if (nameText != null)
                nameText.text = ability.abilityName;

            if (descText != null)
                descText.text = ability.description;

            if (costText != null)
            {
                if (ability.cardCost > 0)
                    costText.text = $"{ability.cardCost} carta(s)";
                else if (ability.healthCost > 0)
                    costText.text = $"{ability.healthCost} HP";
                else
                    costText.text = "Gratis";
            }
        }

        /// <summary>
        /// Actualiza solo el contador de cartas disponibles (para los deck selectors).
        /// </summary>
        public void UpdateCount(int count)
        {
            if (countText != null)
                countText.text = count.ToString();
        }

        private Sprite GetBackgroundForAffinity(AffinityType affinity)
        {
            return affinity switch
            {
                AffinityType.Fuerza    => fuerzaBackground,
                AffinityType.Agilidad  => agilidadBackground,
                AffinityType.Destreza  => destrezaBackground,
                _                      => fuerzaBackground
            };
        }
    }
}

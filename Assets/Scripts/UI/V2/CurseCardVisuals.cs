using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Script que vive en el Prefab de carta de maldición.
    /// Se rellena automáticamente con un CurseData.
    /// </summary>
    public class CurseCardVisuals : MonoBehaviour
    {
        [Header("Visual References (hijos del Prefab)")]
        public Image cardBackground;
        public Image curseIcon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI typeText;

        [Header("Colores por tipo de maldición")]
        public Color positiveColor = new Color(0.27f, 0.67f, 0.35f); // Verde
        public Color negativeColor = new Color(0.8f,  0.2f,  0.2f);  // Rojo
        public Color gamblingColor = new Color(0.8f,  0.75f, 0.1f);  // Dorado

        public void Setup(CurseData curse)
        {
            if (curse == null) return;

            if (curseIcon != null)
            {
                curseIcon.sprite  = curse.icon;
                curseIcon.enabled = curse.icon != null;
            }

            if (nameText != null)
                nameText.text = curse.curseName;

            if (descText != null)
                descText.text = curse.description;

            if (typeText != null)
            {
                typeText.text = curse.type switch
                {
                    CurseType.Positive => "POSITIVA",
                    CurseType.Negative => "NEGATIVA",
                    CurseType.Gambling => "AZAR",
                    _                  => ""
                };
                typeText.color = GetColorForType(curse.type);
            }

            if (cardBackground != null)
                cardBackground.color = GetColorForType(curse.type);
        }

        private Color GetColorForType(CurseType type) => type switch
        {
            CurseType.Positive => positiveColor,
            CurseType.Negative => negativeColor,
            CurseType.Gambling => gamblingColor,
            _                  => Color.white
        };
    }
}

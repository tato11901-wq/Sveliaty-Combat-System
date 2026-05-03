using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Panel de selección de recompensa (Carta de afinidad) V2.
    /// Aparece cuando el jugador gana con un multiplicador alto.
    /// </summary>
    public class RewardChoiceUIV2 : MonoBehaviour
    {
        [Header("References")]
        public CombatManager combatManager;
        public CombatLogUI combatLogUI;

        [Header("UI Elements")]
        public GameObject mainPanel;
        public TextMeshProUGUI titleText;
        public Transform cardsContainer;
        public GameObject cardPrefab;

        [Header("Template Data (Opcional)")]
        [Tooltip("Habilidades 'dummy' para mostrar como representación visual de la recompensa")]
        public AbilityData fuerzaTemplate;
        public AbilityData agilidadTemplate;
        public AbilityData destrezaTemplate;

        private List<GameObject> activeCards = new List<GameObject>();

        void Start()
        {
            mainPanel?.SetActive(false);
            if (combatManager != null)
                combatManager.OnWaitingForCardSelection += ShowRewardChoice;
        }

        void OnDestroy()
        {
            if (combatManager != null)
                combatManager.OnWaitingForCardSelection -= ShowRewardChoice;
        }

        public void ShowRewardChoice(int finalScore)
        {
            if (mainPanel == null) return;

            mainPanel.SetActive(true);
            if (titleText != null)
                titleText.text = $"¡VICTORIA!\nPuntuación: {finalScore}\nElige tu recompensa:";

            ClearCards();

            // Instanciar las 3 opciones
            CreateRewardOption(fuerzaTemplate, AffinityType.Fuerza, 0);
            CreateRewardOption(agilidadTemplate, AffinityType.Agilidad, 1);
            CreateRewardOption(destrezaTemplate, AffinityType.Destreza, 2);
        }

        private void CreateRewardOption(AbilityData template, AffinityType type, int index)
        {
            if (cardPrefab == null || cardsContainer == null) return;

            GameObject cardObj = Instantiate(cardPrefab, cardsContainer);
            activeCards.Add(cardObj);

            // Configurar visuales (usando el mismo script de cartas de combate)
            CardVisuals visuals = cardObj.GetComponent<CardVisuals>();
            if (visuals != null && template != null)
            {
                visuals.Setup(template);
            }

            // Animación de entrada
            cardObj.transform.localScale = Vector3.zero;
            cardObj.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .SetDelay(index * 0.15f)
                .SetUpdate(true);

            // Botón de selección
            Button btn = cardObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnRewardSelected(type));
            }
        }

        private void OnRewardSelected(AffinityType type)
        {
            combatLogUI?.Log($"Recompensa elegida: 1 carta de {type}");
            combatManager?.SelectRewardCard(type);
            
            // Animación de salida y cerrar
            mainPanel.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                mainPanel.SetActive(false);
                mainPanel.transform.localScale = Vector3.one;
                ClearCards();
            }).SetUpdate(true);
        }

        private void ClearCards()
        {
            foreach (var card in activeCards) Destroy(card);
            activeCards.Clear();
        }
    }
}

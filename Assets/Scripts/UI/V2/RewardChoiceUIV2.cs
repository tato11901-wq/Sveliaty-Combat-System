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

        [Header("Contadores Actuales (Opcional)")]
        public TextMeshProUGUI fuerzaCountText;
        public TextMeshProUGUI agilidadCountText;
        public TextMeshProUGUI destrezaCountText;



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
            {
                int inkGained = combatManager != null ? combatManager.LastInkReward : 0;
                string inkString = inkGained > 0 ? $"\nGanaste {inkGained} de tinta." : "\n(Ganancia de tinta bloqueada por Pasiva/Maldición)";
                titleText.text = $"¡VICTORIA!\nPuntuación: {finalScore}{inkString}\nElige tu recompensa:";
            }

            ClearCards();

            // Actualizar contadores si están asignados
            if (combatManager != null && combatManager.playerManager != null)
            {
                if (fuerzaCountText != null) fuerzaCountText.text = $"Tienes {combatManager.playerManager.GetCards(AffinityType.Fuerza)} cartas de Fuerza";
                if (agilidadCountText != null) agilidadCountText.text = $"Tienes {combatManager.playerManager.GetCards(AffinityType.Agilidad)} cartas de Agilidad";
                if (destrezaCountText != null) destrezaCountText.text = $"Tienes {combatManager.playerManager.GetCards(AffinityType.Destreza)} cartas de Destreza";
            }

            // Instanciar las 3 opciones
            CreateRewardOption(AffinityType.Fuerza, 0);
            CreateRewardOption(AffinityType.Agilidad, 1);
            CreateRewardOption(AffinityType.Destreza, 2);
        }

        private void CreateRewardOption(AffinityType type, int index)
        {
            if (cardPrefab == null || cardsContainer == null) return;

            GameObject cardObj = Instantiate(cardPrefab, cardsContainer);
            activeCards.Add(cardObj);

            // Configurar visuales (usando el mismo script de cartas de combate)
            CardVisuals visuals = cardObj.GetComponent<CardVisuals>();
            if (visuals != null)
            {
                visuals.SetupReward(type);
                
                // Si el prefab tiene un contador interno, lo usamos también
                if (combatManager != null && combatManager.playerManager != null)
                {
                    visuals.UpdateCount(combatManager.playerManager.GetCards(type));
                }
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
            
            if (combatManager != null && combatManager.PendingCardSelections > 0)
            {
                return;
            }

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

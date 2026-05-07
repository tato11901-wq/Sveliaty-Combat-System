using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Sveliaty.UI.V2
{
    public class CardInteractionUI : MonoBehaviour
    {
        [Header("References")]
        public CombatManager combatManager;
        public AbilityManager abilityManager;

        [Header("Botones de Mazo (Estáticos en Escena)")]
        public Button fuerzaButton;
        public Button agilidadButton;
        public Button destrezaButton;

        [Header("Contador de Cartas en Botones (Opcional)")]
        public TextMeshProUGUI fuerzaCountText;
        public TextMeshProUGUI agilidadCountText;
        public TextMeshProUGUI destrezaCountText;

        [Header("Cards Display")]
        [Tooltip("Contenedor donde se despliegan las cartas del mazo al abrir uno")]
        public Transform cardsContainer;
        public GameObject cardPrefab;

        private List<GameObject> activeCards = new List<GameObject>();

        private void Start()
        {
            if (fuerzaButton != null)
            {
                fuerzaButton.onClick.AddListener(() => ShowCardsForAffinity(AffinityType.Fuerza));
                fuerzaButton.gameObject.AddComponent<GenericHoverEffect>();
            }

            if (agilidadButton != null)
            {
                agilidadButton.onClick.AddListener(() => ShowCardsForAffinity(AffinityType.Agilidad));
                agilidadButton.gameObject.AddComponent<GenericHoverEffect>();
            }

            if (destrezaButton != null)
            {
                destrezaButton.onClick.AddListener(() => ShowCardsForAffinity(AffinityType.Destreza));
                destrezaButton.gameObject.AddComponent<GenericHoverEffect>();
            }
        }

        /// <summary>
        /// Actualiza los contadores de cartas en los botones de mazo.
        /// </summary>
        public void UpdateDeckCounts(PlayerManager playerManager)
        {
            if (playerManager == null) return;

            int fuerza   = playerManager.GetCards(AffinityType.Fuerza);
            int agilidad = playerManager.GetCards(AffinityType.Agilidad);
            int destreza = playerManager.GetCards(AffinityType.Destreza);

            if (fuerzaCountText   != null) fuerzaCountText.text   = fuerza.ToString();
            if (agilidadCountText != null) agilidadCountText.text = agilidad.ToString();
            if (destrezaCountText != null) destrezaCountText.text = destreza.ToString();

            // Los botones siempre están activos: el golpe básico siempre está disponible
        }

        /// <summary>
        /// Destruye todas las cartas de habilidad actualmente visibles.
        /// </summary>
        public void HideAllCards()
        {
            foreach (var card in activeCards)
            {
                if (card != null) Destroy(card);
            }
            activeCards.Clear();
        }

        private void ShowCardsForAffinity(AffinityType type)
        {
            if (abilityManager == null || combatManager == null) return;
            if (cardsContainer == null || cardPrefab == null)
            {
                Debug.LogWarning("CardInteractionUI: Falta asignar cardsContainer o cardPrefab.");
                return;
            }

            HideAllCards();

            List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);
            int playerLife    = combatManager.GetPlayerLife();
            int enemyAttempts = combatManager.GetCurrentEnemy()?.attemptsRemaining ?? 0;

            foreach (AbilityData ability in abilities)
            {
                GameObject cardObj = Instantiate(cardPrefab, cardsContainer);
                activeCards.Add(cardObj);

                // Rellenar visuals
                CardVisuals visuals = cardObj.GetComponent<CardVisuals>();
                if (visuals != null) visuals.Setup(ability);

                // Configurar interacción
                Button cardBtn = cardObj.GetComponent<Button>();
                if (cardBtn != null)
                {
                    bool canUse = abilityManager.CanUseAbility(ability, playerLife, enemyAttempts);
                    cardBtn.interactable = canUse;

                    if (canUse)
                    {
                        cardBtn.onClick.AddListener(() => ExecuteAttack(ability));

                        // Hover effect
                        var hover = cardObj.gameObject.AddComponent<GenericHoverEffect>();
                        hover.hoverScale = 1.08f;
                        hover.duration = 0.15f;
                    }
                }
                else
                {
                    Debug.LogWarning($"CardInteractionUI: El prefab '{cardPrefab.name}' no tiene Button.");
                }
            }
        }

        private void ExecuteAttack(AbilityData ability)
        {
            if (combatManager == null) return;
            HideAllCards();
            combatManager.PlayerAttempt(new AbilityAction(ability));
        }
    }
}

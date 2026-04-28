using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

namespace Sveliaty.UI.V2
{
    public class CardInteractionUI : MonoBehaviour
    {
        [Header("References")]
        public CombatManager combatManager;
        public AbilityManager abilityManager;

        [Header("Deck Selectors")]
        public Button fuerzaDeckButton;
        public TextMeshProUGUI fuerzaCountText;
        public Button agilidadDeckButton;
        public TextMeshProUGUI agilidadCountText;
        public Button destrezaDeckButton;
        public TextMeshProUGUI destrezaCountText;

        [Header("Cards Display")]
        public Transform cardsContainer;
        public GameObject cardPrefab; // Prefab con un componente Button y visuales de la carta
        
        private List<GameObject> activeCards = new List<GameObject>();

        private void Start()
        {
            fuerzaDeckButton?.onClick.AddListener(() => ShowCardsForAffinity(AffinityType.Fuerza));
            agilidadDeckButton?.onClick.AddListener(() => ShowCardsForAffinity(AffinityType.Agilidad));
            destrezaDeckButton?.onClick.AddListener(() => ShowCardsForAffinity(AffinityType.Destreza));
        }

        public void UpdateDeckCounts(PlayerManager playerManager)
        {
            if (playerManager == null) return;

            if (fuerzaCountText != null) fuerzaCountText.text = playerManager.GetCards(AffinityType.Fuerza).ToString();
            if (agilidadCountText != null) agilidadCountText.text = playerManager.GetCards(AffinityType.Agilidad).ToString();
            if (destrezaCountText != null) destrezaCountText.text = playerManager.GetCards(AffinityType.Destreza).ToString();
            
            // Refrescar las cartas visibles si es que hay un mazo abierto
            // Nota: Podrías querer guardar qué afinidad está abierta y volver a llamar a ShowCardsForAffinity
        }

        public void HideAllCards()
        {
            foreach (var card in activeCards) Destroy(card);
            activeCards.Clear();
        }

        private void ShowCardsForAffinity(AffinityType type)
        {
            if (abilityManager == null || combatManager == null) return;
            if (cardsContainer == null || cardPrefab == null) return;

            HideAllCards();

            List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);
            int playerLife = combatManager.GetPlayerLife();
            int enemyAttempts = combatManager.GetCurrentEnemy()?.attemptsRemaining ?? 0;

            foreach (AbilityData ability in abilities)
            {
                GameObject cardObj = Instantiate(cardPrefab, cardsContainer);
                activeCards.Add(cardObj);

                // Configurar visuales de la carta (Requiere un script en tu Prefab, ej: CardVisuals)
                // Por ahora asumiremos que tiene Textos básicos en sus hijos
                TextMeshProUGUI[] texts = cardObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = ability.abilityName;

                Button cardBtn = cardObj.GetComponent<Button>();
                if (cardBtn != null)
                {
                    bool canUse = abilityManager.CanUseAbility(ability, playerLife, enemyAttempts);
                    cardBtn.interactable = canUse;
                    
                    if (canUse)
                    {
                        cardBtn.onClick.AddListener(() => ExecuteAttack(ability));
                        
                        // Animación hover sencilla
                        EventTrigger trigger = cardObj.gameObject.AddComponent<EventTrigger>();
                        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                        entryEnter.callback.AddListener((data) => { cardObj.transform.DOScale(1.1f, 0.2f); });
                        trigger.triggers.Add(entryEnter);

                        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                        entryExit.callback.AddListener((data) => { cardObj.transform.DOScale(1f, 0.2f); });
                        trigger.triggers.Add(entryExit);
                    }
                }
            }
        }

        private void ExecuteAttack(AbilityData ability)
        {
            if (combatManager == null) return;

            // Ocultar cartas al atacar
            HideAllCards();

            // Lanzar el ataque usando el sistema de Commands
            combatManager.PlayerAttempt(new AbilityAction(ability));
        }
    }
}

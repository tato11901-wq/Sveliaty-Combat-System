using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Panel de selección de maldición V2 UNIFICADO.
    /// Gestiona todo en una sola vista, animando la transición de selección
    /// a descripción sin cambiar de panel.
    /// </summary>
    public class CurseChoiceUIV2 : MonoBehaviour
    {
        [Header("References")]
        public CurseManager curseManager;
        public CombatManager combatManager;
        public BossRushManager bossRushManager;
        public CombatLogUI combatLogUI;

        [Header("Main Panel")]
        public CanvasGroup mainCanvasGroup;

        [Header("Selection Area")]
        public Transform cardsContainer;
        public GameObject curseCardPrefab;

        [Header("Description Area (Oculta al inicio)")]
        public CanvasGroup descriptionCanvasGroup;
        public TextMeshProUGUI selectedCurseNameText;
        public TextMeshProUGUI selectedCurseDescText;
        public Button continueButton;

        private List<CurseData> currentOptions;
        private CurseData selectedCurse;
        private List<GameObject> activeCards = new List<GameObject>();

#if UNITY_EDITOR
        [Header("[DEBUG]")]
        public float debugRerollHoldTime = 0.4f;
        private float debugRerollTimer = 0f;

        void Update()
        {
            if (mainCanvasGroup == null || !mainCanvasGroup.gameObject.activeSelf || descriptionCanvasGroup.gameObject.activeSelf) return;
            if (Input.GetKey(KeyCode.R))
            {
                debugRerollTimer += Time.unscaledDeltaTime;
                if (debugRerollTimer >= debugRerollHoldTime)
                {
                    debugRerollTimer = 0f;
                    ShowChoiceEvent(curseManager.DebugGetNewCurseOptions());
                }
            }
            else debugRerollTimer = 0f;
        }
#endif

        void Start()
        {
            if (mainCanvasGroup != null) mainCanvasGroup.gameObject.SetActive(false);
            if (descriptionCanvasGroup != null) 
            {
                descriptionCanvasGroup.alpha = 0f;
                descriptionCanvasGroup.gameObject.SetActive(false);
            }

            if (curseManager != null)
                curseManager.OnCurseChoiceEvent += ShowChoiceEvent;

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinuePressed);
        }

        void OnDestroy()
        {
            if (curseManager != null)
                curseManager.OnCurseChoiceEvent -= ShowChoiceEvent;
        }

        void ShowChoiceEvent(List<CurseData> options)
        {
            currentOptions = options;
            selectedCurse = null;

            ClearCards();
            
            // Resetear visuales del panel
            if (descriptionCanvasGroup != null)
            {
                descriptionCanvasGroup.alpha = 0f;
                descriptionCanvasGroup.gameObject.SetActive(false);
            }
            
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.gameObject.SetActive(true);
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.DOFade(1f, 0.5f).SetUpdate(true);
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (curseCardPrefab == null || cardsContainer == null) break;

                GameObject cardObj = Instantiate(curseCardPrefab, cardsContainer);
                activeCards.Add(cardObj);

                CurseCardVisuals visuals = cardObj.GetComponent<CurseCardVisuals>();
                if (visuals != null) visuals.Setup(options[i]);

                // Animación de entrada: desde abajo
                cardObj.transform.localScale = Vector3.zero;
                cardObj.transform.DOScale(Vector3.one, 0.6f)
                    .SetEase(Ease.OutBack)
                    .SetDelay(i * 0.1f)
                    .SetUpdate(true);

                int index = i;
                Button btn = cardObj.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnCardSelected(index));
            }
        }

        void OnCardSelected(int index)
        {
            if (currentOptions == null || index >= currentOptions.Count) return;
            selectedCurse = currentOptions[index];

            // Bloquear interacción
            foreach (var card in activeCards)
            {
                Button btn = card.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }

            // Animación: La elegida resalta, las otras se van
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (i == index)
                {
                    activeCards[i].transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
                    // Opcional: moverla a una posición central superior
                    // activeCards[i].transform.DOLocalMove(Vector3.up * 100f, 0.5f).SetUpdate(true);
                }
                else
                {
                    activeCards[i].transform.DOScale(0f, 0.4f).SetEase(Ease.InBack).SetUpdate(true);
                }
            }

            // Aplicar lógica
            string details = curseManager.ObtainCurse(selectedCurse);
            combatLogUI?.LogCurseObtained(selectedCurse.curseName);

            // Mostrar descripción en el mismo panel
            ShowDescriptionArea(details);
        }

        void ShowDescriptionArea(string extraDetails)
        {
            if (selectedCurse == null) return;

            if (descriptionCanvasGroup != null)
            {
                descriptionCanvasGroup.gameObject.SetActive(true);
                // Animación suave de aparición del texto y botón
                descriptionCanvasGroup.DOFade(1f, 0.5f).SetDelay(0.3f).SetUpdate(true);
            }

            if (selectedCurseNameText != null) selectedCurseNameText.text = selectedCurse.curseName;
            
            string desc = selectedCurse.description;
            if (!string.IsNullOrEmpty(extraDetails)) desc += "\n\n" + extraDetails;
            if (selectedCurseDescText != null) selectedCurseDescText.text = desc;
        }

        void OnContinuePressed()
        {
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.DOFade(0f, 0.4f).OnComplete(() => {
                    mainCanvasGroup.gameObject.SetActive(false);
                    ClearCards();
                    ContinueToNextEnemy();
                }).SetUpdate(true);
            }
            else
            {
                ContinueToNextEnemy();
            }
        }

        void ContinueToNextEnemy()
        {
            if (bossRushManager == null) return;
            combatManager?.ResetPostCombatFlag();
            bossRushManager.ContinueToNextCombat();
        }

        void ClearCards()
        {
            foreach (var card in activeCards) Destroy(card);
            activeCards.Clear();
        }
    }
}

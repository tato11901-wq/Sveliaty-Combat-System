using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using Sveliaty.Passives;

namespace Sveliaty.UI.V2
{
    public class PassiveChoiceUI : MonoBehaviour
    {
        [Header("References")]
        public CombatManager combatManager;
        public PassiveDatabase passiveDatabase;

        [Header("UI Elements")]
        public GameObject mainPanel;
        
        [Header("Fixed Slots")]
        public PassiveSlotUI[] slots; // Asignar 3 slots en el inspector

        void Start()
        {
            mainPanel?.SetActive(false);
            if (combatManager != null)
                combatManager.OnWaitingForPassiveSelection += ShowPassiveChoice;
        }

        void OnDestroy()
        {
            if (combatManager != null)
                combatManager.OnWaitingForPassiveSelection -= ShowPassiveChoice;
        }

        public void ShowPassiveChoice(int finalScore)
        {
            if (mainPanel == null || passiveDatabase == null || slots == null || slots.Length < 3)
            {
                Debug.LogWarning("[PassiveChoiceUI] Faltan referencias o slots en el inspector.");
                return;
            }

            mainPanel.SetActive(true);
            mainPanel.transform.localScale = Vector3.zero;
            mainPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);

            List<PassiveSkill> currentPassives = null;
            if (PassiveManager.Instance != null)
            {
                currentPassives = new List<PassiveSkill>(PassiveManager.Instance.ActivePassives);
            }
            List<PassiveSkill> choices = passiveDatabase.GetThreeRandomPassives(currentPassives);

            for (int i = 0; i < slots.Length; i++)
            {
                if (i < choices.Count)
                {
                    var passive = choices[i];
                    var slot = slots[i];

                    slot.button.gameObject.SetActive(true);
                    if (slot.nameText != null) slot.nameText.text = passive.passiveName;
                    if (slot.descText != null) slot.descText.text = passive.description;

                    slot.button.onClick.RemoveAllListeners();
                    slot.button.onClick.AddListener(() => OnPassiveSelected(passive));
                }
                else
                {
                    slots[i].button.gameObject.SetActive(false);
                }
            }
        }

        private void OnPassiveSelected(PassiveSkill selectedPassive)
        {
            combatManager?.SelectPassive(selectedPassive);
            
            // Animación de salida y cerrar
            mainPanel.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                mainPanel.SetActive(false);
                mainPanel.transform.localScale = Vector3.one;
            }).SetUpdate(true);
        }
    }

    [System.Serializable]
    public class PassiveSlotUI
    {
        public Button button;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
    }
}

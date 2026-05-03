using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

namespace Sveliaty.UI.V2
{
    public class BottomStatsUI : MonoBehaviour
    {
        [Header("Player Health Visuals")]
        [Tooltip("Imagen tipo 'Filled' para la barra de vida")]
        public UnityEngine.UI.Image healthFillImage;
        [Tooltip("Texto numérico XX/YY")]
        public TextMeshProUGUI healthText;
        
        [Header("Enemy Health Visuals")]
        [Tooltip("Imagen tipo 'Filled' para la barra de vida del enemigo")]
        public UnityEngine.UI.Image enemyHealthFillImage;
        [Tooltip("Texto numérico XX/YY del enemigo")]
        public TextMeshProUGUI enemyHealthText;
        
        [Header("Attempts Visuals")]
        public Transform attemptsContainer;
        public GameObject attemptTokenPrefab;

        // Cache
        private List<GameObject> activeHearts = new List<GameObject>();
        private List<GameObject> activeTokens = new List<GameObject>();

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (healthText != null)
            {
                healthText.text = $"{currentHealth}/{maxHealth}";
            }

            if (healthFillImage != null && maxHealth > 0)
            {
                float fillAmount = (float)Mathf.Max(0, currentHealth) / maxHealth;
                healthFillImage.DOFillAmount(fillAmount, 0.3f).SetEase(Ease.OutCubic);
            }
        }

        public void UpdateEnemyHealth(int currentHealth, int maxHealth)
        {
            if (enemyHealthText != null)
            {
                enemyHealthText.text = $"{currentHealth}/{maxHealth}";
            }

            if (enemyHealthFillImage != null && maxHealth > 0)
            {
                float fillAmount = (float)Mathf.Max(0, currentHealth) / maxHealth;
                enemyHealthFillImage.DOFillAmount(fillAmount, 0.3f).SetEase(Ease.OutCubic);
            }
        }

        public void UpdateAttempts(int currentAttempts)
        {
            if (attemptsContainer == null || attemptTokenPrefab == null) return;

            foreach (var t in activeTokens) Destroy(t);
            activeTokens.Clear();

            for (int i = 0; i < currentAttempts; i++)
            {
                GameObject token = Instantiate(attemptTokenPrefab, attemptsContainer);
                activeTokens.Add(token);
            }
        }

        public void PlayDamageAnimation()
        {
            // Shake a la barra de vida o a la cámara
            if (healthFillImage != null)
            {
                healthFillImage.transform.parent.DOComplete();
                healthFillImage.transform.parent.DOShakePosition(0.4f, 15f, 20);
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.DOComplete();
                mainCam.transform.DOShakePosition(0.3f, 0.4f, 25, 90f);
            }
        }
    }
}

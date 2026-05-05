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
        [Tooltip("Imagen tipo 'Filled' para la armadura del enemigo (Gris)")]
        public UnityEngine.UI.Image enemyArmorFillImage;
        
        [Header("Attempts Visuals")]
        public Transform attemptsContainer;
        public GameObject attemptTokenPrefab;

        [Header("Card Stats Visuals")]
        public TextMeshProUGUI fuerzaCardsText;
        public TextMeshProUGUI agilidadCardsText;
        public TextMeshProUGUI destrezaCardsText;

        [Header("Ink Visuals")]
        public TextMeshProUGUI inkText;

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

        public void UpdateEnemyHealth(int currentHealth, int maxHealth, int currentArmor = 0)
        {
            if (enemyHealthText != null)
            {
                if (currentArmor > 0)
                    enemyHealthText.text = $"{currentHealth} <color=#AAAAAA>(+{currentArmor} armadura)</color> / {maxHealth}";
                else
                    enemyHealthText.text = $"{currentHealth} / {maxHealth}";
            }

            if (maxHealth > 0)
            {
                float healthFill = (float)Mathf.Max(0, currentHealth) / maxHealth;
                if (enemyHealthFillImage != null)
                {
                    enemyHealthFillImage.DOFillAmount(healthFill, 0.3f).SetEase(Ease.OutCubic);
                }

                if (enemyArmorFillImage != null)
                {
                    // Ahora la barra gris solo representa la cantidad de armadura (se dibuja encima de la salud)
                    float armorFill = (float)Mathf.Max(0, currentArmor) / maxHealth;
                    enemyArmorFillImage.DOFillAmount(armorFill, 0.3f).SetEase(Ease.OutCubic);
                    
                    // Desvanecer la barra gris si no hay armadura para que no estorbe visualmente
                    if (currentArmor <= 0)
                    {
                        enemyArmorFillImage.DOFade(0f, 0.2f);
                    }
                    else
                    {
                        enemyArmorFillImage.DOFade(1f, 0.2f);
                    }
                }
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

        public void UpdateCardStats(int fuerza, int agilidad, int destreza, int itemFuerza = 0, int itemAgilidad = 0, int itemDestreza = 0)
        {
            if (fuerzaCardsText != null)
                fuerzaCardsText.text = itemFuerza != 0 ? $"{fuerza} {(itemFuerza > 0 ? "+" : "")}{itemFuerza}" : fuerza.ToString();
            if (agilidadCardsText != null)
                agilidadCardsText.text = itemAgilidad != 0 ? $"{agilidad} {(itemAgilidad > 0 ? "+" : "")}{itemAgilidad}" : agilidad.ToString();
            if (destrezaCardsText != null)
                destrezaCardsText.text = itemDestreza != 0 ? $"{destreza} {(itemDestreza > 0 ? "+" : "")}{itemDestreza}" : destreza.ToString();
        }

        public void UpdateInk(int ink)
        {
            if (inkText != null) inkText.text = ink.ToString();
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

        public void PlayEnemyHealAnimation()
        {
            if (enemyHealthFillImage != null)
            {
                enemyHealthFillImage.transform.parent.DOComplete();
                
                Color originalColor = enemyHealthFillImage.color;
                // La barra se pone verde durante medio segundo mientras sube, y luego vuelve a su color
                enemyHealthFillImage.DOColor(Color.green, 0.2f).OnComplete(() => {
                    enemyHealthFillImage.DOColor(originalColor, 0.4f);
                });
                
                enemyHealthFillImage.transform.parent.DOPunchScale(Vector3.one * 0.15f, 0.4f);
            }
        }
    }
}

using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

namespace Sveliaty.UI.V2
{
    public class BottomStatsUI : MonoBehaviour
    {
        [Header("Health Visuals")]
        public Transform healthContainer;
        public GameObject fullHeartPrefab;
        public GameObject emptyHeartPrefab;
        
        [Header("Attempts Visuals")]
        public Transform attemptsContainer;
        public GameObject attemptTokenPrefab;

        // Cache
        private List<GameObject> activeHearts = new List<GameObject>();
        private List<GameObject> activeTokens = new List<GameObject>();

        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (healthContainer == null) return;

            // Limpiar corazones viejos
            foreach (var h in activeHearts) Destroy(h);
            activeHearts.Clear();

            // Dibujar corazones
            for (int i = 0; i < maxHealth; i++)
            {
                GameObject prefabToUse = (i < currentHealth) ? fullHeartPrefab : emptyHeartPrefab;
                if (prefabToUse != null)
                {
                    GameObject heart = Instantiate(prefabToUse, healthContainer);
                    activeHearts.Add(heart);
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

        public void PlayDamageAnimation()
        {
            // Shake a la barra de vida o a la cámara
            if (healthContainer != null)
            {
                healthContainer.DOComplete();
                healthContainer.DOShakePosition(0.4f, 15f, 20);
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

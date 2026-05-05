using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

namespace Sveliaty.UI.V2
{
    /// <summary>
    /// Módulo visual encargado exclusivamente de renderizar y animar al enemigo central.
    /// </summary>
    public class EnemyVisualsUI : MonoBehaviour
    {
        [Header("References")]
        public Image enemyImage;

        [Header("Dice Visuals")]
        public Transform diceContainer;
        public GameObject dicePrefab;

        private List<GameObject> activeDice = new List<GameObject>();

        /// <summary>
        /// Actualiza la imagen del enemigo e inicia una animación de aparición.
        /// </summary>
        public void SetupEnemy(EnemyInstance enemy)
        {
            if (enemyImage == null)
            {
                Debug.LogError("EnemyVisualsUI: No hay referencia a la Image del enemigo en el Inspector.");
                return;
            }

            if (enemy != null && enemy.enemyTierData != null && enemy.enemyTierData.sprite != null)
            {
                enemyImage.sprite = enemy.enemyTierData.sprite;
                enemyImage.color = Color.white;
                enemyImage.gameObject.SetActive(true);
                
                // Resetear escala por si venía de una animación de muerte
                enemyImage.transform.localScale = Vector3.one;

                // Mostrar los dados debajo del sprite
                UpdateDiceCount(enemy.currentRPGDiceCount);
            }
            else
            {
                string enemyName = enemy != null ? enemy.enemyData.displayName : "NULL";
                Debug.LogWarning($"EnemyVisualsUI: El enemigo {enemyName} no tiene sprite asignado en su TierData ({enemy?.enemyTierData?.name}).");
                enemyImage.color = new Color(1, 1, 1, 0); // Invisible si no hay imagen
            }
        }

        /// <summary>
        /// Animación cuando el enemigo recibe daño.
        /// </summary>
        public void PlayDamageAnimation()
        {
            if (enemyImage == null) return;

            // Efecto de parpadeo rojo y vibración
            enemyImage.transform.DOShakePosition(0.3f, 10f, 20, 90f);
            enemyImage.DOColor(Color.red, 0.15f).OnComplete(() =>
            {
                enemyImage.DOColor(Color.white, 0.15f);
            });
        }

        /// <summary>
        /// Animación cuando el enemigo es derrotado.
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (enemyImage == null) return;

            enemyImage.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack);
            enemyImage.DOFade(0f, 0.4f);
        }

        /// <summary>
        /// Instancia prefabs de dados debajo del sprite del enemigo.
        /// </summary>
        public void UpdateDiceCount(int count)
        {
            if (diceContainer == null || dicePrefab == null) return;

            foreach (var d in activeDice) Destroy(d);
            activeDice.Clear();

            for (int i = 0; i < count; i++)
            {
                GameObject dice = Instantiate(dicePrefab, diceContainer);
                activeDice.Add(dice);
            }
        }
    }
}

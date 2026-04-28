using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Sveliaty.UI.V2
{
    public class TopBarUI : MonoBehaviour
    {
        [Header("Enemy Info")]
        public TextMeshProUGUI enemyNameText;
        public TextMeshProUGUI magicArmorText;
        
        [Header("Timeline Visuals")]
        public Transform timelineContainer;
        public GameObject combatNodePrefab;
        public GameObject shopNodePrefab;
        public GameObject bossNodePrefab;
        
        // Cache visual para limpiar
        private List<GameObject> activeNodes = new List<GameObject>();

        public void UpdateEnemyInfo(EnemyInstance enemy)
        {
            if (enemy == null) return;

            if (enemyNameText != null)
                enemyNameText.text = enemy.enemyData.displayName;

            if (magicArmorText != null)
            {
                string buffs = "";
                if (enemy.activeArmor > 0) buffs += $"ARMADURA: {enemy.activeArmor} ";
                if (enemy.activeThorns > 0) buffs += $"ESPINAS: {enemy.activeThorns * 100}% ";
                if (enemy.hasSpeedEvasion) buffs += "EVASIÓN ";

                magicArmorText.text = buffs;
            }
        }

        public void UpdateTimeline(List<NodeType> upcomingNodes)
        {
            if (timelineContainer == null) return;

            // Limpiar nodos viejos
            foreach (var node in activeNodes)
            {
                Destroy(node);
            }
            activeNodes.Clear();

            // Instanciar nuevos nodos
            foreach (NodeType nodeType in upcomingNodes)
            {
                GameObject prefab = nodeType switch
                {
                    NodeType.Combat => combatNodePrefab,
                    NodeType.Shop => shopNodePrefab,
                    NodeType.Boss => bossNodePrefab,
                    _ => combatNodePrefab
                };

                if (prefab != null)
                {
                    GameObject newNode = Instantiate(prefab, timelineContainer);
                    activeNodes.Add(newNode);
                }
            }
        }
    }
}

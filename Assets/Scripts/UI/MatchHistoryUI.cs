using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI que muestra el historial de partidas
/// </summary>
public class MatchHistoryUI : MonoBehaviour
{
    [Header("References")]
    public GameObject historyPanel;
    public Transform contentParent;
    public GameObject entryPrefab;
    public Button backButton;

    void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(HideHistory);
        }
    }

    public void ShowHistory()
    {
        if (historyPanel != null) historyPanel.SetActive(true);
        RefreshList();
    }

    public void HideHistory()
    {
        if (historyPanel != null) historyPanel.SetActive(false);
    }

    public void RefreshList()
    {
        // Limpiar lista actual
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Obtener datos
        if (MatchHistoryManager.Instance == null) return;
        List<MatchHistoryManager.MatchEntry> history = MatchHistoryManager.Instance.GetHistory();

        // Poblar lista
        foreach (var entry in history)
        {
            GameObject go = Instantiate(entryPrefab, contentParent);
            MatchHistoryEntryUI entryUI = go.GetComponent<MatchHistoryEntryUI>();
            if (entryUI != null)
            {
                entryUI.Setup(entry);
            }
        }
    }
}

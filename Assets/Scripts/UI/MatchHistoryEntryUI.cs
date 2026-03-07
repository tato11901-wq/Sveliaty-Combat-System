using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Script para un elemento individual en la lista de historial de partidas
/// </summary>
public class MatchHistoryEntryUI : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI dateText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI defeatedByText;

    [Header("Visuals")]
    public Image backgroundImage;
    public Color victoryColor = new Color(0.7f, 1f, 0.7f, 0.5f);
    public Color defeatColor = new Color(1f, 0.7f, 0.7f, 0.5f);

    public void Setup(MatchHistoryManager.MatchEntry entry)
    {
        if (dateText != null) dateText.text = entry.date;
        if (scoreText != null) scoreText.text = $"Puntos: {entry.score}";
        if (statsText != null) statsText.text = $"F:{entry.fuerza} A:{entry.agilidad} D:{entry.destreza}";
        
        if (resultText != null)
        {
            resultText.text = entry.isVictory ? "VICTORIA" : "DERROTA";
            resultText.color = entry.isVictory ? Color.green : Color.red;
        }

        if (defeatedByText != null)
        {
            defeatedByText.text = string.IsNullOrEmpty(entry.defeatedBy) ? "" : $"{entry.defeatedBy}";
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = entry.isVictory ? victoryColor : defeatColor;
        }
    }
}

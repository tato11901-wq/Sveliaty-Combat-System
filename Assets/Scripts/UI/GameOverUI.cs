using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador de la UI de Game Over
/// Muestra estadísticas finales y el enemigo que derrotó al jugador
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Referencias de Textos")]
    public TextMeshProUGUI titleText; // NUEVO: Para cambiar el título gigante entre Game Over y Victoria
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI fuerzaCardsText;
    public TextMeshProUGUI agilidadCardsText;
    public TextMeshProUGUI destrezaCardsText;
    public TextMeshProUGUI defeatedByHeaderText; // NUEVO: Para el texto superior ("Enemigo final" o "Fuiste derrotado por")
    public TextMeshProUGUI defeatedByText; // SOLO cambiará al nombre del enemigo

    [Header("Referencias de Imagen")]
    public Image defeatedBySprite;

    [Header("Botones")]
    public Button finishButton;

    void Start()
    {
        // Configurar botón
        if (finishButton != null)
        {
            finishButton.onClick.AddListener(OnFinishButtonClicked);
        }
    }

    /// <summary>
    /// Muestra la pantalla de Game Over con las estadísticas finales
    /// </summary>
    public void ShowGameOver(int finalScore, int fuerzaCards, int agilidadCards, int destrezaCards, EnemyInstance defeatedBy, bool isVictory = false)
    {
        Debug.Log($"💀 Mostrando Game Over - Score: {finalScore}");

        // Actualizar título (si está asignado)
        if (titleText != null)
        {
            titleText.text = isVictory ? "Victoria definitiva" : "Game Over";
        }

        // Actualizar textos de estadísticas
        if (scoreText != null)
        {
            scoreText.text = $"Puntuación Final: {finalScore}";
        }

        if (fuerzaCardsText != null)
        {
            fuerzaCardsText.text = $"Fuerza: {fuerzaCards}";
        }

        if (agilidadCardsText != null)
        {
            agilidadCardsText.text = $"Agilidad: {agilidadCards}";
        }

        if (destrezaCardsText != null)
        {
            destrezaCardsText.text = $"Destreza: {destrezaCards}";
        }

        // Mostrar enemigo que te derrotó o el boss final
        if (isVictory)
        {
            if (defeatedBy != null)
            {
                if (defeatedByHeaderText != null)
                {
                    defeatedByHeaderText.text = "Enemigo final";
                }

                if (defeatedByText != null)
                {
                    defeatedByText.text = defeatedBy.enemyData.displayName;
                }

                if (defeatedBySprite != null && defeatedBy.enemyTierData.sprite != null)
                {
                    defeatedBySprite.sprite = defeatedBy.enemyTierData.sprite;
                    defeatedBySprite.enabled = true;
                }
            }
            else
            {
                if (defeatedByHeaderText != null)
                {
                    defeatedByHeaderText.text = "¡FELICIDADES!";
                }

                if (defeatedByText != null)
                {
                    defeatedByText.text = "Has completado el Boss Rush";
                }

                if (defeatedBySprite != null)
                {
                    defeatedBySprite.enabled = false;
                }
            }
        }
        else if (defeatedBy != null)
        {
            if (defeatedByHeaderText != null)
            {
                defeatedByHeaderText.text = "Fuiste derrotado por";
            }

            if (defeatedByText != null)
            {
                defeatedByText.text = defeatedBy.enemyData.displayName;
            }

            if (defeatedBySprite != null && defeatedBy.enemyTierData.sprite != null)
            {
                defeatedBySprite.sprite = defeatedBy.enemyTierData.sprite;
                defeatedBySprite.enabled = true;
            }
        }
        else
        {
            if (defeatedByHeaderText != null)
            {
                defeatedByHeaderText.text = "Game Over";
            }

            if (defeatedByText != null)
            {
                defeatedByText.text = "Fuiste derrotado";
            }

            if (defeatedBySprite != null)
            {
                defeatedBySprite.enabled = false;
            }
        }
    }

    /// <summary>
    /// Maneja el clic en el botón "Finalizar Partida"
    /// </summary>
    void OnFinishButtonClicked()
    {
        Debug.Log("Finalizando partida, volviendo al menú");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance es null");
        }
    }

    /// <summary>
    /// Opcional: Añadir efectos al activar el panel
    /// </summary>
    void OnEnable()
    {
        // Aquí puedes añadir animaciones, sonidos, etc.
    }
}
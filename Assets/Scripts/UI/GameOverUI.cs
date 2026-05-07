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

    [Header("Contenedores")]
    public GameObject gameOverContent; // EL PANEL que quieres ocultar (para no apagar todo el Canvas)

    [Header("Referencias Externas")]
    // El generador de personaje ya no es post-partida: el personaje se actualiza
    // dinámicamente durante la run desde CharacterVisualController.
    // Esta referencia se conserva por compatibilidad futura.
    public CharacterVisualController characterVisualController;

    // Variables para guardar stats finales
    private int finalFuerza;
    private int finalAgilidad;
    private int finalDestreza;
    private bool wasVictory;

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
        wasVictory = isVictory;
        Debug.Log($"💀 Mostrando Game Over - Score: {finalScore} (Victoria: {isVictory})");

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
            finalFuerza = fuerzaCards;
        }

        if (agilidadCardsText != null)
        {
            agilidadCardsText.text = $"Agilidad: {agilidadCards}";
            finalAgilidad = agilidadCards;
        }

        if (destrezaCardsText != null)
        {
            destrezaCardsText.text = $"Destreza: {destrezaCards}";
            finalDestreza = destrezaCards;
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
        
        // GUARDAR EN HISTORIAL
        if (MatchHistoryManager.Instance != null)
        {
            string enemyName = defeatedBy != null && defeatedBy.enemyData != null ? defeatedBy.enemyData.displayName : (isVictory ? "Victoria Final" : "Desconocido");
            MatchHistoryManager.Instance.SaveMatch(finalScore, fuerzaCards, agilidadCards, destrezaCards, isVictory, enemyName);
        }
    }

    /// <summary>
    /// Maneja el clic en el botón "Finalizar Partida" / "Continuar"
    /// </summary>
    void OnFinishButtonClicked()
    {
        Debug.Log($"Botón Finalizar presionado. wasVictory={wasVictory}");

        if (gameOverContent != null)
            gameOverContent.SetActive(false);
        else
            gameObject.SetActive(false);

        if (wasVictory)
        {
            // Si ganamos, vamos al generador de personaje
            UnityEngine.SceneManagement.SceneManager.LoadScene("Character Scene");
        }
        else
        {
            // Si perdimos, vamos al menú principal
            // (Si RestartGame carga la StartScene, nos sirve)
            if (GameManager.Instance != null)
                GameManager.Instance.RestartGame();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
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
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;

/// <summary>
/// Controlador de la escena final de victoria.
/// 
/// Al cargarse, lee VictoryData de GlobalData y actualiza el personaje visual.
/// Ofrece dos botones: guardar foto en escritorio y volver al menú principal.
/// 
/// Setup en escena:
///   - Asignar CharacterVisualController del Personaje.
///   - Asignar los dos botones en el Inspector.
///   - (Opcional) Asignar scoreText y feedbackText para mostrar texto en pantalla.
/// </summary>
public class CharacterSceneController : MonoBehaviour
{
    [Header("Sistema visual del personaje")]
    public CharacterVisualController characterVisualController;

    [Header("UI — Botones")]
    [Tooltip("Botón para capturar y guardar foto del personaje en el escritorio.")]
    public Button photoButton;

    [Tooltip("Botón para volver al menú principal.")]
    public Button menuButton;

    [Header("UI — Textos (opcionales)")]
    [Tooltip("Muestra la puntuación final de la run.")]
    public TextMeshProUGUI scoreText;

    [Tooltip("Feedback temporal para el botón de foto (ej: '¡Foto guardada!').")]
    public TextMeshProUGUI feedbackText;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Conectar botones
        if (photoButton != null)
            photoButton.onClick.AddListener(TakePhoto);

        if (menuButton != null)
            menuButton.onClick.AddListener(GoToMenu);

        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);

        // Leer datos de victoria y aplicar al personaje
        LoadAndApplyVictoryData();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Inicialización
    // ─────────────────────────────────────────────────────────────────────────

    private void LoadAndApplyVictoryData()
    {
        Sveliaty.Core.VictoryData data = Sveliaty.Core.GlobalData.PendingVictoryData;

        if (data == null)
        {
            Debug.LogWarning("[CharacterSceneController] No hay VictoryData. " +
                             "Personaje se muestra sin piezas (puede ser intencional en testing).");

            if (characterVisualController != null)
                characterVisualController.ClearAll();
            return;
        }

        // Mostrar puntuación si el texto está asignado
        if (scoreText != null)
            scoreText.text = $"Puntuación final: {data.finalScore}";

        // Aplicar visual del personaje
        if (characterVisualController != null)
        {
            characterVisualController.RefreshCharacterVisual(
                data.activeSkills,
                data.cardsFuerza,
                data.cardsAgilidad,
                data.cardsDestreza
            );
        }
        else
        {
            Debug.LogError("[CharacterSceneController] characterVisualController no asignado.");
        }

        // Consumir el dato (evita que persista en una siguiente carga)
        Sveliaty.Core.GlobalData.PendingVictoryData = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Botón: Foto
    // ─────────────────────────────────────────────────────────────────────────

    private void TakePhoto()
    {
        StartCoroutine(CaptureAndSave());
    }

    private IEnumerator CaptureAndSave()
    {
        // Desactivar UI antes de capturar para que no salga en la foto
        if (photoButton != null)  photoButton.gameObject.SetActive(false);
        if (menuButton != null)   menuButton.gameObject.SetActive(false);
        if (scoreText != null)    scoreText.gameObject.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        // Esperar un frame para que Unity aplique los cambios de UI
        yield return new WaitForEndOfFrame();

        // Construir ruta de escritorio
        string escritorio = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string carpeta    = Path.Combine(escritorio, "Sveliaty", "Personajes");

        if (!Directory.Exists(carpeta))
            Directory.CreateDirectory(carpeta);

        string timestamp   = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string rutaCompleta = Path.Combine(carpeta, $"Personaje_{timestamp}.png");

        ScreenCapture.CaptureScreenshot(rutaCompleta);

        Debug.Log($"[CharacterSceneController] Foto guardada en: {rutaCompleta}");

        // Restaurar UI
        if (photoButton != null)  photoButton.gameObject.SetActive(true);
        if (menuButton != null)   menuButton.gameObject.SetActive(true);
        if (scoreText != null)    scoreText.gameObject.SetActive(true);

        // Mostrar feedback
        if (feedbackText != null)
        {
            feedbackText.text = $"¡Foto guardada!\n{rutaCompleta}";
            feedbackText.gameObject.SetActive(true);
            yield return new WaitForSeconds(3f);
            feedbackText.gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Botón: Menú
    // ─────────────────────────────────────────────────────────────────────────

    private void GoToMenu()
    {
        Sveliaty.Core.GlobalData.PendingVictoryData = null;
        Sveliaty.Core.GlobalData.ShouldLoadSave = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI del menu de inicio
/// Muestra opciones para iniciar nueva run o continuar run guardada
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [Header("References")]
    public GameObject bestiaryPanel; // Panel del bestiario

    [Header("UI Elements")]
    public Button continueButton;
    public Button newRunRPGButton;
    public Button bestiaryButton;
    public Button quitButton;

    [Header("Continue Button Info")]
    public TextMeshProUGUI continueButtonText;
    public GameObject continueInfoPanel; // Panel con info de la run guardada

    void Start()
    {
        // Configurar botones
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinuePressed);
        
        
        
        if (newRunRPGButton != null)
            newRunRPGButton.onClick.AddListener(() => OnNewRunPressed());

        if (bestiaryButton != null)
        {
            bestiaryPanel.SetActive(false); // Asegurarse de que el panel del bestiario esté oculto al inicio
            bestiaryButton.onClick.AddListener(() => 
            {
                // Aquí puedes cargar la escena del bestiario o activar su panel
                bestiaryPanel.SetActive(true);
                Debug.Log("Botón de Bestiario presionado");
            });
        }
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);

        // Actualizar UI
        RefreshUI();
    }

    void OnEnable()
    {
        RefreshUI();
    }

    /// <summary>
    /// Actualiza la UI segun si hay run guardada o no
    /// </summary>
    void RefreshUI()
    {
        bool hasSavedRun = RunSaveManager.HasSavedRun();

        // Mostrar/ocultar boton continuar
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSavedRun);
        }

        // Actualizar texto del boton
        if (hasSavedRun && continueButtonText != null)
        {
            RunSaveManager.RunSaveData saveInfo = RunSaveManager.GetSavedRunInfo();
            
            if (saveInfo != null)
            {
                continueButtonText.text = "CONTINUAR\n" + 
                                         "HP: " + saveInfo.playerCurrentLife + "/" + saveInfo.playerMaxLife + "\n" +
                                         "Score: " + saveInfo.playerScore;
            }
            else
            {
                continueButtonText.text = "CONTINUAR";
            }
        }

        // Mostrar info adicional si existe panel
        if (continueInfoPanel != null)
        {
            continueInfoPanel.SetActive(hasSavedRun);
            
            if (hasSavedRun)
            {
                // Aqui podrias añadir mas info visual
            }
        }
    }

    /// <summary>
    /// Continuar run guardada
    /// </summary>
    void OnContinuePressed()
    {
        if (!RunSaveManager.HasSavedRun())
        {
            Debug.LogWarning("No hay run guardada para continuar");
            return;
        }

        Debug.Log("Continuando run guardada...");

        Sveliaty.Core.GlobalData.ShouldLoadSave = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Combat Scene");
    }

    /// <summary>
    /// Iniciar nueva run
    /// </summary>
    void OnNewRunPressed()
    {
        // Si hay run guardada, preguntar confirmacion
        if (RunSaveManager.HasSavedRun())
        {
            ShowNewRunConfirmation();
        }
        else
        {
            StartNewRun();
        }
    }

    /// <summary>
    /// Muestra panel de confirmacion para sobrescribir run guardada
    /// </summary>
    void ShowNewRunConfirmation()
    {
        // Aqui podrias crear un panel de confirmacion
        // Por ahora, preguntamos directamente
        
        Debug.Log("ADVERTENCIA: Hay una run guardada. Iniciar nueva run la sobrescribira.");
        
        // TODO: Crear UI de confirmacion
        // Por ahora, iniciar directamente
        StartNewRun();
    }

    void StartNewRun()
    {
        Debug.Log("Iniciando nueva run");

        RunSaveManager.ClearSavedRun();

        Sveliaty.Core.GlobalData.ShouldLoadSave = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Combat Scene");
    }

    void OnQuitPressed()
    {
        Debug.Log("Saliendo del juego...");
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
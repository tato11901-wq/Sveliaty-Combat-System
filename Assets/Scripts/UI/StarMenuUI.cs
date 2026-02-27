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
    public GameManager gameManager;
    public RunSaveManager runSaveManager;
    public GameObject bestiaryPanel; // Panel del bestiario
    public GameObject[] Combatpanels;

    [Header("UI Elements")]
    public Button continueButton;
    public Button newRunPlayerChoosesButton;
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
        
        
        if (newRunPlayerChoosesButton != null)
            newRunPlayerChoosesButton.onClick.AddListener(() => OnNewRunPressed(CombatMode.PlayerChooses));
        
        if (newRunRPGButton != null)
            newRunRPGButton.onClick.AddListener(() => OnNewRunPressed(CombatMode.TraditionalRPG));

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
        if (runSaveManager == null)
        {
            Debug.LogError("RunSaveManager no asignado");
            return;
        }

        bool hasSavedRun = runSaveManager.HasSavedRun();

        // Mostrar/ocultar boton continuar
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSavedRun);
        }

        // Actualizar texto del boton
        if (hasSavedRun && continueButtonText != null)
        {
            RunSaveManager.RunSaveData saveInfo = runSaveManager.GetSavedRunInfo();
            
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
        if (runSaveManager == null)
        {
            Debug.LogError("RunSaveManager no asignado");
            return;
        }

        if (!runSaveManager.HasSavedRun())
        {
            Debug.LogWarning("No hay run guardada para continuar");
            return;
        }

        Debug.Log("Continuando run guardada...");

        // Cargar run
        bool success = runSaveManager.LoadSavedRun();

        if (success)
        {
            // Ocultar menu
            gameObject.SetActive(false);
            
            // Activar gameplay panel
            if (gameManager != null)
            {
                for (int i = 0; i < Combatpanels.Length; i++)
                {
                    Combatpanels[i].SetActive(true);
                }


            }
        }
        else
        {
            Debug.LogError("Error al cargar run guardada");
            // Aqui podrias mostrar un mensaje de error al jugador
        }
    }

    /// <summary>
    /// Iniciar nueva run
    /// </summary>
    void OnNewRunPressed(CombatMode mode)
    {
        // Si hay run guardada, preguntar confirmacion
        if (runSaveManager != null && runSaveManager.HasSavedRun())
        {
            ShowNewRunConfirmation(mode);
        }
        else
        {
            StartNewRun(mode);
        }
    }

    /// <summary>
    /// Muestra panel de confirmacion para sobrescribir run guardada
    /// </summary>
    void ShowNewRunConfirmation(CombatMode mode)
    {
        // Aqui podrias crear un panel de confirmacion
        // Por ahora, preguntamos directamente
        
        Debug.Log("ADVERTENCIA: Hay una run guardada. Iniciar nueva run la sobrescribira.");
        
        // TODO: Crear UI de confirmacion
        // Por ahora, iniciar directamente
        StartNewRun(mode);
    }

    void StartNewRun(CombatMode mode)
    {
        Debug.Log("Iniciando nueva run: " + mode);

        // Eliminar guardado anterior
        if (runSaveManager != null)
        {
            runSaveManager.ClearSavedRun();
        }

        // Iniciar run
        if (gameManager != null)
        {
            gameManager.StartNewRun(mode);
        }
        else
        {
            Debug.LogError("GameManager no asignado");
        }
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
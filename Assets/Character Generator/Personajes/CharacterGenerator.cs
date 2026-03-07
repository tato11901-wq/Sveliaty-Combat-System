using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class CharacterManager : MonoBehaviour
{
    [Header("Data")]
    public BodyParts parts;

    [Header("UI Images")]
    public Image cabezaImage;
    public Image cuerpoImage;
    public Image brazosImage;

    [Header("UI Containers")]
    public GameObject mainPanel; // El panel que contiene toda la UI del generador
    public Button backToMenuButton; // Botón para volver al menú principal

    [Header("Value Texts")]
    public TMP_Text fuerzaText;
    public TMP_Text velocidadText;
    public TMP_Text destrezaText;

    void Start()
    {
        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.AddListener(VolverAlMenu);
        }
    }

    /// <summary>
    /// Configura el personaje basado en estadísticas externas al final de la partida.
    /// </summary>
    public void InicializarPostPartida(int fuerza, int agilidad, int destreza)
    {
        if (mainPanel != null) mainPanel.SetActive(true);

        // Clampear para asegurar que están en el rango 1-9 de los assets
        int f = Mathf.Clamp(fuerza, 1, 9);
        int v = Mathf.Clamp(agilidad, 1, 9); // Mapeamos Agilidad a Velocidad visual
        int d = Mathf.Clamp(destreza, 1, 9);

        // Actualizar textos y sprites directamente
        if (fuerzaText != null) fuerzaText.text = f.ToString();
        if (velocidadText != null) velocidadText.text = v.ToString();
        if (destrezaText != null) destrezaText.text = d.ToString();

        int idxBrazos = Mathf.Clamp(f - 1, 0, parts.Brazos.Length - 1);
        int idxCuerpo = Mathf.Clamp(v - 1, 0, parts.Cuerpos.Length - 1);
        int idxCabeza = Mathf.Clamp(d - 1, 0, parts.Cabezas.Length - 1);

        if (brazosImage != null) brazosImage.sprite = parts.Brazos[idxBrazos];
        if (cuerpoImage != null) cuerpoImage.sprite = parts.Cuerpos[idxCuerpo];
        if (cabezaImage != null) cabezaImage.sprite = parts.Cabezas[idxCabeza];
    }

    public void VolverAlMenu()
    {
        Debug.Log("CharacterManager: Volviendo al menú principal");
        if (mainPanel != null) mainPanel.SetActive(false);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    public void GuardarImagen()
    {
        string escritorio = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string carpeta = Path.Combine(escritorio, "Personajes", "Resultados");

        if (!Directory.Exists(carpeta))
            Directory.CreateDirectory(carpeta);

        string nombreArchivo = "Personaje_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string rutaCompleta = Path.Combine(carpeta, nombreArchivo);

        ScreenCapture.CaptureScreenshot(rutaCompleta);

        Debug.Log("Imagen guardada en: " + rutaCompleta);
    }


    Texture2D CapturarPantallaUI()
    {
        Canvas canvas = cabezaImage.canvas;

        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        Vector2 size = rectTransform.sizeDelta;

        Texture2D texture = new Texture2D((int)size.x, (int)size.y, TextureFormat.RGB24, false);

        RenderTexture renderTexture = new RenderTexture((int)size.x, (int)size.y, 24);
        Camera cam = Camera.main;

        cam.targetTexture = renderTexture;
        cam.Render();

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
        texture.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;

        Destroy(renderTexture);

        return texture;
    }
}

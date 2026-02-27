using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI del Bestiario
/// Permite navegar entre enemigos y ver su informacion descubierta
/// </summary>
public class BestiaryUI : MonoBehaviour
{
    [Header("References")]
    public BestiaryManager bestiaryManager;
    public EnemyDatabase enemyDatabase;
    public GameObject bestiaryPanel;

    [Header("UI Elements - Enemy Info")]
    public Image enemySprite;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyDescriptionText;

    [Header("UI Elements - Tier Buttons")]
    public Button tier1Button;
    public Button tier2Button;
    public Button tier3Button;

    [Header("UI Elements - Affinity Display")]
    public TextMeshProUGUI fuerzaAffinityText;
    public TextMeshProUGUI agilidadAffinityText;
    public TextMeshProUGUI destrezaAffinityText;

    [Header("Navigation")]
    public Button previousButton;
    public Button nextButton;
    public Button backButton;

    [Header("Colors")]
    public Color defeatedColor = Color.white;
    public Color discoveredColor = Color.gray;
    public Color unknownColor = Color.black;

    // Estado actual
    private int currentEnemyIndex = 0;
    private EnemyTier currentSelectedTier = EnemyTier.Tier_1;

    void Start()
    {
        // Configurar botones
        if (previousButton != null)
            previousButton.onClick.AddListener(OnPreviousPressed);
        
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextPressed);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        if (tier1Button != null)
            tier1Button.onClick.AddListener(() => SelectTier(EnemyTier.Tier_1));
        
        if (tier2Button != null)
            tier2Button.onClick.AddListener(() => SelectTier(EnemyTier.Tier_2));
        
        if (tier3Button != null)
            tier3Button.onClick.AddListener(() => SelectTier(EnemyTier.Tier_3));

        // Mostrar primer enemigo
        RefreshUI();
    }

    void OnEnable()
    {
        RefreshUI();
    }

    /// <summary>
    /// Actualiza toda la UI con el enemigo actual
    /// </summary>
    void RefreshUI()
    {
        if (enemyDatabase == null || enemyDatabase.allEnemies.Count == 0)
        {
            Debug.LogError("EnemyDatabase vacio o no asignado");
            return;
        }

        // Obtener enemigo actual
        EnemyData currentEnemy = enemyDatabase.allEnemies[currentEnemyIndex];
        
        // Verificar estado en bestiario
        bool hasEncountered = bestiaryManager.HasEncountered(currentEnemy.id);
        bool hasDefeated = bestiaryManager.HasDefeated(currentEnemy.id);

        // Actualizar nombre y descripcion
        UpdateEnemyInfo(currentEnemy, hasEncountered, hasDefeated);

        // Actualizar botones de tier
        UpdateTierButtons(currentEnemy);

        // Actualizar display de afinidades
        UpdateAffinityDisplay(currentEnemy);

        // Actualizar sprite del tier seleccionado
        UpdateEnemySprite(currentEnemy);
    }

    /// <summary>
    /// Actualiza nombre y descripcion del enemigo
    /// </summary>
    void UpdateEnemyInfo(EnemyData enemy, bool hasEncountered, bool hasDefeated)
    {
        if (enemyNameText != null)
        {
            if (!hasEncountered)
            {
                enemyNameText.text = "???";
            }
            else
            {
                enemyNameText.text = enemy.displayName;
            }
        }

        if (enemyDescriptionText != null)
        {
            if (!hasEncountered)
            {
                enemyDescriptionText.text = "No descubierto aun";
            }
            else if (!hasDefeated)
            {
                enemyDescriptionText.text = "Encontrado pero no derrotado";
            }
            else
            {
                // Aqui podrias añadir una descripcion custom al EnemyData
                enemyDescriptionText.text = "Derrotado";
            }
        }
    }

    /// <summary>
    /// Actualiza los botones de tier segun progreso
    /// </summary>
    void UpdateTierButtons(EnemyData enemy)
    {
        UpdateSingleTierButton(tier1Button, enemy, EnemyTier.Tier_1);
        UpdateSingleTierButton(tier2Button, enemy, EnemyTier.Tier_2);
        UpdateSingleTierButton(tier3Button, enemy, EnemyTier.Tier_3);
    }

    void UpdateSingleTierButton(Button button, EnemyData enemy, EnemyTier tier)
    {
        if (button == null) return;

        bool discovered = bestiaryManager.IsTierDiscovered(enemy.id, tier);
        bool defeated = bestiaryManager.IsTierDefeated(enemy.id, tier);

        // Colorear segun estado
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (defeated)
                buttonImage.color = defeatedColor;
            else if (discovered)
                buttonImage.color = discoveredColor;
            else
                buttonImage.color = unknownColor;
        }

        // Interactividad
        button.interactable = discovered;
    }

    /// <summary>
    /// Actualiza el display de afinidades
    /// </summary>
    void UpdateAffinityDisplay(EnemyData enemy)
    {
        bool hasEncountered = bestiaryManager.HasEncountered(enemy.id);

        if (!hasEncountered)
        {
            // No descubierto
            if (fuerzaAffinityText != null)
                fuerzaAffinityText.text = "???";
            if (agilidadAffinityText != null)
                agilidadAffinityText.text = "???";
            if (destrezaAffinityText != null)
                destrezaAffinityText.text = "???";
            return;
        }

        // Mostrar afinidades descubiertas
        UpdateSingleAffinity(fuerzaAffinityText, enemy, AffinityType.Fuerza);
        UpdateSingleAffinity(agilidadAffinityText, enemy, AffinityType.Agilidad);
        UpdateSingleAffinity(destrezaAffinityText, enemy, AffinityType.Destreza);
    }

    void UpdateSingleAffinity(TextMeshProUGUI text, EnemyData enemy, AffinityType affinity)
    {
        if (text == null) return;

        bool discovered = bestiaryManager.IsAffinityDiscovered(enemy.id, affinity);

        if (!discovered)
        {
            text.text = "No descubierto aun";
            return;
        }

        // Obtener multiplicador
        AffinityMultiplier multiplier = bestiaryManager.GetDiscoveredAffinityMultiplier(enemy.id, affinity);

        string multiplierText = multiplier switch
        {
            AffinityMultiplier.Weak => "DEBIL (x1.5)",
            AffinityMultiplier.Neutral => "NEUTRAL (x1.0)",
            AffinityMultiplier.Strong => "RESISTENTE (x0.5)",
            AffinityMultiplier.Immune => "INMUNE (x0)",
            _ => "DESCONOCIDO"
        };

        text.text = multiplierText;

        // Colorear
        Color affinityColor = multiplier switch
        {
            AffinityMultiplier.Weak => Color.green,
            AffinityMultiplier.Neutral => Color.white,
            AffinityMultiplier.Strong => Color.red,
            AffinityMultiplier.Immune => Color.black,
            _ => Color.gray
        };

        text.color = affinityColor;
    }

    /// <summary>
    /// Actualiza el sprite del enemigo segun tier seleccionado
    /// </summary>
    void UpdateEnemySprite(EnemyData enemy)
    {
        if (enemySprite == null) return;

        bool tierDiscovered = bestiaryManager.IsTierDiscovered(enemy.id, currentSelectedTier);

        if (!tierDiscovered)
        {
            // Sprite de silueta o placeholder
            enemySprite.sprite = null;
            enemySprite.color = Color.black;
            return;
        }

        // Obtener sprite del tier
        EnemyTierData tierData = GetTierData(enemy, currentSelectedTier);
        if (tierData != null && tierData.sprite != null)
        {
            enemySprite.sprite = tierData.sprite;
            enemySprite.color = Color.white;
        }
    }

    EnemyTierData GetTierData(EnemyData enemy, EnemyTier tier)
    {
        foreach (var tierData in enemy.enemyTierData)
        {
            if (tierData.enemyTier == tier)
                return tierData;
        }
        return null;
    }

    // ========== NAVEGACION ==========

    void OnPreviousPressed()
    {
        if (enemyDatabase == null || enemyDatabase.allEnemies.Count == 0) return;

        currentEnemyIndex--;
        if (currentEnemyIndex < 0)
            currentEnemyIndex = enemyDatabase.allEnemies.Count - 1;

        currentSelectedTier = EnemyTier.Tier_1;
        RefreshUI();
    }

    void OnNextPressed()
    {
        if (enemyDatabase == null || enemyDatabase.allEnemies.Count == 0) return;

        currentEnemyIndex++;
        if (currentEnemyIndex >= enemyDatabase.allEnemies.Count)
            currentEnemyIndex = 0;

        currentSelectedTier = EnemyTier.Tier_1;
        RefreshUI();
    }

    void SelectTier(EnemyTier tier)
    {
        currentSelectedTier = tier;
        RefreshUI();
    }

    void OnBackPressed()
    {
        // Volver al menu principal
        bestiaryPanel.SetActive(false);
    }
}
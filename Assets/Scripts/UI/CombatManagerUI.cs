using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.EventSystems;

public class CombatUIManager : MonoBehaviour
{
    [Header("Referencias")]
    public BossRushManager bossRushManager; // NUEVO: Para el flujo entre combates
    public CombatManager combatManager;
    public AbilityManager abilityManager;
    public CurseManager curseManager;
    
    [Header("Canvas Groups para ocultar durante maldiciones")]
    public CanvasGroup combatUICanvasGroup; // Asignar el panel padre de toda la UI de combate

    [Header("Enemy Display")]
    public Image enemySprite;
    public TextMeshProUGUI enemyNameText;
    public TextMeshProUGUI enemyTierText;

    [Header("Stats Display")]
    public TextMeshProUGUI vidaText;
    public TextMeshProUGUI escaladoText;
    public TextMeshProUGUI intentosText;
    public TextMeshProUGUI dadosText;
    public TextMeshProUGUI vidaActualText;

    [Header("Player Extra Stats (Armor, Lifesteal, Crit)")]
    public TextMeshProUGUI armaduraJugadorText;
    public Image armaduraJugadorFill;
    public TextMeshProUGUI roboVidaText;

    [Header("Buttons - Modo Passive")]
    public GameObject passiveModePanel;
    public Button attackButton;
    public TextMeshProUGUI cartasText;

    [Header("Buttons - Modo PlayerChooses/RPG")]
    public GameObject playerChoosePanel;
    public Button fuerzaMainButton;
    public Button agilidadMainButton;
    public Button destrezaMainButton;
    public Button backButton; // Boton Volver
    public TextMeshProUGUI fuerzaMainText;
    public TextMeshProUGUI agilidadMainText;
    public TextMeshProUGUI destrezaMainText;
    public GameObject fuerzaAbilitiesPanel;
    public GameObject agilidadAbilitiesPanel;
    public GameObject destrezaAbilitiesPanel;
    
    [Header("Fuerza Abilities")]
    public Button fuerzaAbility1Button;
    public Button fuerzaAbility2Button;
    public Button fuerzaAbility3Button;
    public TextMeshProUGUI fuerzaAbility1Text;
    public TextMeshProUGUI fuerzaAbility2Text;
    public TextMeshProUGUI fuerzaAbility3Text;
    
    [Header("Agilidad Abilities")]
    public Button agilidadAbility1Button;
    public Button agilidadAbility2Button;
    public Button agilidadAbility3Button;
    public TextMeshProUGUI agilidadAbility1Text;
    public TextMeshProUGUI agilidadAbility2Text;
    public TextMeshProUGUI agilidadAbility3Text;
    
    [Header("Destreza Abilities")]
    public Button destrezaAbility1Button;
    public Button destrezaAbility2Button;
    public Button destrezaAbility3Button;
    public TextMeshProUGUI destrezaAbility1Text;
    public TextMeshProUGUI destrezaAbility2Text;
    public TextMeshProUGUI destrezaAbility3Text;

    [Header("Colors")]
    public Color colorDesconocido = Color.gray;
    public Color colorDebilidad = Color.green;
    public Color colorResistencia = Color.red;
    public Color colorInmunidad = Color.black;
    public Color colorNeutral = Color.white;
    public Color lockedAbilityColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Results Display")]
    public TextMeshProUGUI resultadoDadosText;
    public TextMeshProUGUI resultadoAtaqueText;

    [Header("Victory/Defeat Panels")]
    public GameObject passiveEndPanel;
    public TextMeshProUGUI passiveEndMessageText;
    public Button passiveNextEnemyButton;

    public GameObject playerChoosesVictoryPanel;
    public TextMeshProUGUI playerChoosesVictoryText;
    public Button selectFuerzaButton;
    public Button selectAgilidadButton;
    public Button selectDestrezaButton;

    public GameObject defeatPanel;
    public TextMeshProUGUI defeatMessageText;
    public Button defeatNextEnemyButton;

    // NUEVO: Panel de notificación de maldición
    [Header("Curse Notification Panel")]
    public GameObject curseNotificationPanel;
    public TextMeshProUGUI curseNotificationText;
    public Button curseNotificationButton;

    [Header("Ajustes de Interfaz")]
    public Toggle easyModeToggle; // NUEVO: Alternar el modo fácil (mostrar afinidades)

    private AffinityType? currentExpandedType = null;
    private bool isEasyModeEnabled = false;

    // --- VARIABLES DE TILT 3D ---
    private Dictionary<Button, bool> buttonHoverStates = new Dictionary<Button, bool>();
    private Dictionary<Button, Vector3> buttonOriginalRotations = new Dictionary<Button, Vector3>();
    public float tiltAmount = 12f;
    public float smoothSpeed = 15f;
    private Canvas parentCanvas;

void Start()
{
    parentCanvas = GetComponentInParent<Canvas>();
    attackButton.onClick.AddListener(() => {
        AffinityType attackType = currentExpandedType.HasValue ? currentExpandedType.Value : AffinityType.Fuerza;
        combatManager.PlayerAttempt(new BasicAttackAction(attackType));
    });

    fuerzaMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Fuerza));
    agilidadMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Agilidad));
    destrezaMainButton.onClick.AddListener(() => ToggleAbilityPanel(AffinityType.Destreza));

    fuerzaAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 0));
    fuerzaAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 1));
    fuerzaAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Fuerza, 2));
    
    agilidadAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 0));
    agilidadAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 1));
    agilidadAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Agilidad, 2));
    
    destrezaAbility1Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 0));
    destrezaAbility2Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 1));
    destrezaAbility3Button.onClick.AddListener(() => UseAbility(AffinityType.Destreza, 2));

    // MODIFICADO: Ya no va directo al siguiente enemigo
    passiveNextEnemyButton.onClick.AddListener(() => OnVictoryPanelContinue());
    defeatNextEnemyButton.onClick.AddListener(() => OnDefeatPanelContinue());

    selectFuerzaButton.onClick.AddListener(() => SelectCard(AffinityType.Fuerza));
    selectAgilidadButton.onClick.AddListener(() => SelectCard(AffinityType.Agilidad));
    selectDestrezaButton.onClick.AddListener(() => SelectCard(AffinityType.Destreza));

    // NUEVO: Botón de notificación de maldición
    if (curseNotificationButton != null)
    {
        curseNotificationButton.onClick.AddListener(() => OnCurseNotificationContinue());
    }

    // NUEVO: Botón Volver
    if (backButton != null)
    {
        backButton.onClick.AddListener(() => OnBackButtonPressed());
    }

    // NUEVO: Toggle de Modo Fácil
    if (easyModeToggle != null)
    {
        easyModeToggle.onValueChanged.AddListener((isOn) => 
        {
            isEasyModeEnabled = isOn;
            UpdateAffinitiesUI();
        });
        isEasyModeEnabled = easyModeToggle.isOn;
    }

    HideAllAbilityPanels();
    HideBackButton();

    SetupButtonAnimations();
}

void SetupButtonAnimations()
{
    // Botones Principales
    AnimateButton(fuerzaMainButton);
    AnimateButton(agilidadMainButton);
    AnimateButton(destrezaMainButton);

    // Botones de Habilidades
    AnimateButton(fuerzaAbility1Button);
    AnimateButton(fuerzaAbility2Button);
    AnimateButton(fuerzaAbility3Button);

    AnimateButton(agilidadAbility1Button);
    AnimateButton(agilidadAbility2Button);
    AnimateButton(agilidadAbility3Button);

    AnimateButton(destrezaAbility1Button);
    AnimateButton(destrezaAbility2Button);
    AnimateButton(destrezaAbility3Button);

    // Botones de recompensa
    AnimateButton(selectFuerzaButton);
    AnimateButton(selectAgilidadButton);
    AnimateButton(selectDestrezaButton);

    // Botones simples
    AnimateButton(attackButton);
    AnimateButton(backButton);
    AnimateButton(passiveNextEnemyButton);
    AnimateButton(defeatNextEnemyButton);
    AnimateButton(curseNotificationButton);
}

void AnimateButton(Button btn)
{
    if (btn == null) return;

    EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
    if (trigger == null)
    {
        trigger = btn.gameObject.AddComponent<EventTrigger>();
    }

    Vector3 originalScale = btn.transform.localScale;
    Vector3 originalRotation = btn.transform.localEulerAngles;
    
    // Registrar el botón para su trackeo en el Update
    if (!buttonHoverStates.ContainsKey(btn)) 
    {
        buttonHoverStates.Add(btn, false);
        buttonOriginalRotations.Add(btn, originalRotation);
    }

    // Hover Enter
    EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
    entryEnter.callback.AddListener((data) => { 
        if (!btn.interactable) return;
        btn.transform.DOScale(originalScale * 1.05f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true); 
        buttonHoverStates[btn] = true;
    });
    trigger.triggers.Add(entryEnter);

    // Hover Exit
    EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
    entryExit.callback.AddListener((data) => { 
        if (!btn.interactable) return;
        btn.transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true); 
        buttonHoverStates[btn] = false;
        
        // Restaurar rotación suavemente
        btn.transform.DOLocalRotate(originalRotation, 0.3f).SetEase(Ease.OutQuad).SetUpdate(true);
    });
    trigger.triggers.Add(entryExit);

    // Click Down
    EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
    entryDown.callback.AddListener((data) => { 
        if (btn.interactable) btn.transform.DOScale(originalScale * 0.95f, 0.1f).SetEase(Ease.InQuad).SetUpdate(true); 
    });
    trigger.triggers.Add(entryDown);

    // Click Up
    EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
    entryUp.callback.AddListener((data) => { 
        if (btn.interactable) btn.transform.DOScale(originalScale * 1.05f, 0.1f).SetEase(Ease.OutQuad).SetUpdate(true); 
    });
    trigger.triggers.Add(entryUp);
}

    void ToggleAbilityPanel(AffinityType type)
    {
        // Ocultar todos los paneles de habilidades
        HideAllAbilityPanels();

        // Mostrar el panel del tipo seleccionado
        GameObject panelToShow = type switch
        {
            AffinityType.Fuerza => fuerzaAbilitiesPanel,
            AffinityType.Agilidad => agilidadAbilitiesPanel,
            AffinityType.Destreza => destrezaAbilitiesPanel,
            _ => null
        };

        if (panelToShow != null)
        {
            panelToShow.SetActive(true);
            currentExpandedType = type;
            UpdateAbilityButtons(type);
            // combatManager.SelectAttackType(type); // OBSOLETO
            
            // NUEVO: Ocultar botones principales y mostrar botón Volver
            HideMainButtons();
            ShowBackButton();
        }
    }

    void HideAllAbilityPanels()
    {
        fuerzaAbilitiesPanel.SetActive(false);
        agilidadAbilitiesPanel.SetActive(false);
        destrezaAbilitiesPanel.SetActive(false);
    }

    void UpdateAbilityButtons(AffinityType type)
    {
        if (abilityManager == null) return;

        List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);

        Button[] buttons = type switch
        {
            AffinityType.Fuerza => new[] { fuerzaAbility1Button, fuerzaAbility2Button, fuerzaAbility3Button },
            AffinityType.Agilidad => new[] { agilidadAbility1Button, agilidadAbility2Button, agilidadAbility3Button },
            AffinityType.Destreza => new[] { destrezaAbility1Button, destrezaAbility2Button, destrezaAbility3Button },
            _ => null
        };

        TextMeshProUGUI[] texts = type switch
        {
            AffinityType.Fuerza => new[] { fuerzaAbility1Text, fuerzaAbility2Text, fuerzaAbility3Text },
            AffinityType.Agilidad => new[] { agilidadAbility1Text, agilidadAbility2Text, agilidadAbility3Text },
            AffinityType.Destreza => new[] { destrezaAbility1Text, destrezaAbility2Text, destrezaAbility3Text },
            _ => null
        };

        if (buttons == null || texts == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (i < abilities.Count)
            {
                AbilityData ability = abilities[i];
                
                string costInfo = GetAbilityCostString(ability);
                texts[i].text = $"{ability.abilityName}\n{costInfo}";

                bool canUse = abilityManager.CanUseAbility(
                    ability, 
                    combatManager.GetPlayerLife(), 
                    combatManager.GetCurrentEnemy().attemptsRemaining
                );

                buttons[i].interactable = canUse;
                
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = canUse ? Color.white : lockedAbilityColor;
                }
            }
            else
            {
                texts[i].text = "???";
                buttons[i].interactable = false;
                
                Image buttonImage = buttons[i].GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = lockedAbilityColor;
                }
            }
        }
    }

    string GetAbilityCostString(AbilityData ability)
    {
        List<string> costs = new List<string>();
        
        if (ability.cardCost > 0)
            costs.Add($"🎴{ability.cardCost}");
        
        if (ability.healthCost > 0)
            costs.Add($"❤️{ability.healthCost}");
        
        if (ability.turnCost > 1)
            costs.Add($"⏱️{ability.turnCost}");

        return costs.Count > 0 ? string.Join(" ", costs) : "Gratis";
    }

    void UseAbility(AffinityType type, int abilityIndex)
    {
        if (abilityManager == null) return;

        List<AbilityData> abilities = abilityManager.GetAvailableAbilities(type);
        
        if (abilityIndex < abilities.Count)
        {
            AbilityData ability = abilities[abilityIndex];
            combatManager.PlayerAttempt(new AbilityAction(ability));
            UpdateAffinitiesUI();
            UpdateAbilityButtons(type);
        }
    }

private void OnEnable()
{
    if (combatManager == null) return;

    combatManager.OnCombatStart += HandleCombatStart;
    combatManager.OnAttackResult += HandleAttackResult;
    combatManager.OnCombatEnd += HandleCombatEnd;
    combatManager.OnAttemptsChanged += HandleAttemptsChanged;
    combatManager.OnWaitingForCardSelection += HandleWaitingForCardSelection;
    
    // Nuevos eventos del CombatManager refactorizado
    combatManager.OnTurnStartEvent += HandleTurnStartEvent;
    combatManager.OnEnemyTurnEvent += HandleEnemyTurnEvent;
    combatManager.OnHitReceivedEvent += HandleHitReceivedEvent;

    passiveEndPanel.SetActive(false);
    playerChoosesVictoryPanel.SetActive(false);
    defeatPanel.SetActive(false);
    
    if (curseNotificationPanel != null)
    {
        curseNotificationPanel.SetActive(false);
    }

    HideAllAbilityPanels();

    if (combatManager.HasActiveEnemy())
    {
        HandleCombatStart(combatManager.GetCurrentEnemy());
    }
}

private void OnDisable()
{
    if (combatManager == null) return;

    combatManager.OnCombatStart -= HandleCombatStart;
    combatManager.OnAttackResult -= HandleAttackResult;
    combatManager.OnCombatEnd -= HandleCombatEnd;
    combatManager.OnAttemptsChanged -= HandleAttemptsChanged;
    combatManager.OnWaitingForCardSelection -= HandleWaitingForCardSelection;
    
    combatManager.OnTurnStartEvent -= HandleTurnStartEvent;
    combatManager.OnEnemyTurnEvent -= HandleEnemyTurnEvent;
    combatManager.OnHitReceivedEvent -= HandleHitReceivedEvent;
}

    void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatStart -= HandleCombatStart;
            combatManager.OnAttackResult -= HandleAttackResult;
            combatManager.OnCombatEnd -= HandleCombatEnd;
            combatManager.OnAttemptsChanged -= HandleAttemptsChanged;
            combatManager.OnWaitingForCardSelection -= HandleWaitingForCardSelection;
            
            combatManager.OnTurnStartEvent -= HandleTurnStartEvent;
            combatManager.OnEnemyTurnEvent -= HandleEnemyTurnEvent;
            combatManager.OnHitReceivedEvent -= HandleHitReceivedEvent;
        }
    }

    void HandleCombatStart(EnemyInstance enemy)
    {
        // Restaurar UI de combate
        if (combatUICanvasGroup != null)
        {
            combatUICanvasGroup.alpha = 1f;
            combatUICanvasGroup.interactable = true;
            combatUICanvasGroup.blocksRaycasts = true;
        }
        // Asegurar que el enemigo aparezca visible (limpiar restos de animaciones anteriores)
        if (enemySprite != null)
        {
            enemySprite.transform.DOKill();
            enemySprite.DOKill();
            
            if (enemy.enemyTierData.sprite == null)
            {
                Debug.LogWarning($"El enemigo {enemy.enemyData.displayName} no tiene un sprite asignado!");
                enemySprite.color = new Color(1, 1, 1, 0); // Ocultar si no hay imagen
            }
            else
            {
                enemySprite.sprite = enemy.enemyTierData.sprite;
                enemySprite.color = Color.white;
            }
            
            enemySprite.transform.localScale = Vector3.one;
            enemySprite.gameObject.SetActive(true);
        }

        enemyNameText.text = enemy.enemyData.displayName;
        enemyTierText.text = enemy.enemyTierData.GetEnemyTier();

        vidaText.text = enemy.currentRPGHealth.ToString();

        intentosText.text = enemy.attemptsRemaining.ToString();
        dadosText.text = enemy.currentRPGDiceCount.ToString();

        // Mostrar nombre + buffs activos en la UI
        UpdateEnemyNameWithBuffs();

        UpdateModeUI();
        UpdatePlayerLifeUI();

        resultadoDadosText.text = "-";
        resultadoAtaqueText.text = "-";

        UpdateCardsDisplay();
        UpdateAffinitiesUI();
        UpdatePlayerExtraStatsUI();

        // NUEVO: Resetear estado de botones
        HideAllAbilityPanels();
        ShowMainButtons();
        HideBackButton();
        currentExpandedType = null;
    }

    // ========== EVENTOS DEL NUEVO COMBAT MANAGER ==========
    
    void HandleTurnStartEvent(TurnContext context)
    {
        UpdateCardsDisplay();
        if (currentExpandedType.HasValue) UpdateAbilityButtons(currentExpandedType.Value);
        
        UpdateEnemyNameWithBuffs();
    }

    void HandleEnemyTurnEvent()
    {
        Debug.Log("CombatManagerUI: El enemigo ha tomado su turno.");
        UpdateEnemyNameWithBuffs();
    }
    
    void HandleHitReceivedEvent(int damage)
    {
        // Shake screen ya existe en OnCombatEnd para derrota final, pero aquí es por perder intento sin morir
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.DOComplete();
            mainCam.transform.DOShakePosition(0.3f, 0.4f, 25, 90f);
        }
    }

    void UpdateEnemyNameWithBuffs()
    {
        if (combatManager == null || combatManager.GetCurrentEnemy() == null) return;
        
        EnemyInstance enemy = combatManager.GetCurrentEnemy();
        string extraText = "";
        
        if (enemy.activeArmor > 0) extraText += $" [Armadura: {enemy.activeArmor}]";
        if (enemy.activeThorns > 0) extraText += $" [Espinas: {enemy.activeThorns * 100}%]";
        if (enemy.hasSpeedEvasion) extraText += $" [Velocidad Activa]";
        
        enemyNameText.text = enemy.enemyData.displayName + extraText;
    }

    string GetEscaladoText(EnemyInstance enemy)
    {
        return " ";
    }

    void HandleAttackResult(int roll, int bonus, int total, float multiplier, bool isCritical, float affinityMultiplier, bool isFirstStrike)
    {
        resultadoDadosText.text = roll.ToString();
        
        string colorTag = multiplier > 1.1f ? "<color=green>" : (multiplier < 0.9f ? "<color=red>" : "<color=white>");
        string multText = multiplier != 1f ? $" ({colorTag}x{multiplier}</color>)" : "";
        
        resultadoAtaqueText.text = $"{total}{multText}";
        
        EnemyInstance currentEnemy = combatManager.GetCurrentEnemy();
        vidaText.text = Mathf.Max(0, currentEnemy.currentRPGHealth).ToString();
        
        // --- GAME FEEL: Animación de impacto ---
        // 1. Efecto en la cámara/pantalla general
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.DOComplete();
            mainCam.transform.DOShakePosition(0.2f, 0.3f, 20, 90, false, true);
        }

        // 2. Efecto en el enemigo (Flash de daño y contracción)
        if (enemySprite != null)
        {
            enemySprite.transform.DOComplete();
            enemySprite.DOComplete();

            enemySprite.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0), 0.2f, 5, 1f);
            
            // Si el daño fue fuerte (multiplicador debilidad), destello más intenso
            if (multiplier > 1.1f)
            {
                enemySprite.DOColor(Color.red, 0.1f).OnComplete(() => enemySprite.DOColor(Color.white, 0.1f));
            }
        }
        
        UpdateAffinitiesUI();

        if (currentExpandedType.HasValue)
        {
            UpdateAbilityButtons(currentExpandedType.Value);
        }
    }

    void HandleCombatEnd(bool victory, int finalScore, AffinityType rewardCard, int lifeLost)
    {
        UpdatePlayerLifeUI();
        HideAllAbilityPanels();
        currentExpandedType = null;

        if (victory)
        {
            // --- GAME FEEL: Animación de Muerte del Enemigo ---
            if (enemySprite != null)
            {
                enemySprite.transform.DOComplete();
                enemySprite.DOComplete();
                enemySprite.DOFade(0f, 0.4f).SetUpdate(true);
                enemySprite.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).SetUpdate(true);
            }

            // Panel de victoria pasiva - Suavizado: Fade-in + Slide Up
            passiveEndPanel.SetActive(true);
            CanvasGroup passiveCG = passiveEndPanel.GetComponent<CanvasGroup>();
            if (passiveCG == null) passiveCG = passiveEndPanel.AddComponent<CanvasGroup>();
            
            // Resetear estado inicial
            passiveCG.alpha = 0f;
            RectTransform passiveRT = passiveEndPanel.GetComponent<RectTransform>();
            float originalY = passiveRT.anchoredPosition.y;
            passiveRT.anchoredPosition = new Vector2(passiveRT.anchoredPosition.x, originalY - 20f);
            passiveEndPanel.transform.localScale = Vector3.one;

            // Animación suave
            passiveCG.DOFade(1f, 0.6f).SetUpdate(true);
            passiveRT.DOAnchorPosY(originalY, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);
            
            if (rewardCard == default)
            {
                 passiveEndMessageText.text = $"¡VICTORIA!\n\nPuntuación: {finalScore}\n\nNo explotaste la debilidad y no hubo suerte con el botín.";
            }
            else
            {
                passiveEndMessageText.text = $"¡VICTORIA!\n\nPuntuación: {finalScore}\n\nObtienes 1 carta de:\n{rewardCard}";
            }
        }
        else
        {
            // --- GAME FEEL: Animación de Daño al Jugador ---
            if (lifeLost > 0)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.transform.DOComplete();
                    mainCam.transform.DOShakePosition(0.5f, 0.5f, 30, 90f);
                }
            }

            defeatPanel.SetActive(true);
            CanvasGroup defeatCG = defeatPanel.GetComponent<CanvasGroup>();
            if (defeatCG == null) defeatCG = defeatPanel.AddComponent<CanvasGroup>();
            
            // Resetear estado inicial (igual que victoria)
            defeatCG.alpha = 0f;
            RectTransform defeatRT = defeatPanel.GetComponent<RectTransform>();
            float dOriginalY = defeatRT.anchoredPosition.y;
            defeatRT.anchoredPosition = new Vector2(defeatRT.anchoredPosition.x, dOriginalY - 20f);
            defeatPanel.transform.localScale = Vector3.one;

            // Animación suave (igual que victoria)
            defeatCG.DOFade(1f, 0.6f).SetUpdate(true);
            defeatRT.DOAnchorPosY(dOriginalY, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);

            defeatMessageText.text = $"<size=120%>DERROTA</size>\n\nPuntuación: {finalScore}";

            if (lifeLost > 0)
            {
                defeatMessageText.text += $"\n\nPerdiste <color=red>{lifeLost}</color> vida.";
            }
            else if (lifeLost == -1)
            {
                defeatMessageText.text += "\n\n<b><color=green>¡DAÑO DENEGADO POR\nEFECTO DE CARTA!</color></b>";
            }
            else if (lifeLost == -2)
            {
                defeatMessageText.text += "\n\n<b><color=orange>¡MUERTE DENEGADA POR\nEFECTO DE CARTA!</color></b>\n<size=80%>Tu vida se ha fijado en 1.</size>";
            }
        }
        
        UpdateCardsDisplay();
    }

    void HandleWaitingForCardSelection(int finalScore)
    {
        // --- GAME FEEL: Muerte del enemigo aquí también ---
        if (enemySprite != null)
        {
            enemySprite.transform.DOComplete();
            enemySprite.DOComplete();
            enemySprite.DOFade(0f, 0.4f).SetUpdate(true);
            enemySprite.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack).SetUpdate(true);
        }

        playerChoosesVictoryPanel.SetActive(true);
        
        CanvasGroup choiceCG = playerChoosesVictoryPanel.GetComponent<CanvasGroup>();
        if (choiceCG == null) choiceCG = playerChoosesVictoryPanel.AddComponent<CanvasGroup>();
        
        // Resetear estado inicial
        choiceCG.alpha = 0f;
        RectTransform choiceRT = playerChoosesVictoryPanel.GetComponent<RectTransform>();
        float cOriginalY = choiceRT.anchoredPosition.y;
        choiceRT.anchoredPosition = new Vector2(choiceRT.anchoredPosition.x, cOriginalY - 20f);
        playerChoosesVictoryPanel.transform.localScale = Vector3.one;

        // Animación suave
        choiceCG.DOFade(1f, 0.6f).SetUpdate(true);
        choiceRT.DOAnchorPosY(cOriginalY, 0.6f).SetEase(Ease.OutCubic).SetUpdate(true);
        
        playerChoosesVictoryText.text = $"¡VICTORIA!\n\nPuntuación: {finalScore}\n\n Elige tu recompensa:";
    }

    void SelectCard(AffinityType selectedType)
    {
        combatManager.SelectRewardCard(selectedType);
        playerChoosesVictoryPanel.SetActive(false);
        passiveEndPanel.SetActive(true);
        passiveEndMessageText.text = $"¡WOW! ¡VICTORIA!\n\n Obtuviste 1 carta de:\n{selectedType}";
        UpdateCardsDisplay();
    }

    void HandleAttemptsChanged(int remainingAttempts)
    {
        intentosText.text = remainingAttempts.ToString();
    }

    void UpdateModeUI()
    {
        passiveModePanel.SetActive(false);
        playerChoosePanel.SetActive(true);

        UpdateCardsDisplay();
        UpdateAffinitiesUI();
        UpdatePlayerLifeUI();
        UpdatePlayerExtraStatsUI();
    }

    void UpdateCardsDisplay()
    {
        int cardsF = combatManager.GetCardsOfType(AffinityType.Fuerza);
        float itemF = combatManager.statsManager != null ? combatManager.statsManager.GetFinalStat(StatType.Fuerza, null) - cardsF : 0;
        
        int cardsA = combatManager.GetCardsOfType(AffinityType.Agilidad);
        float itemA = combatManager.statsManager != null ? combatManager.statsManager.GetFinalStat(StatType.Velocidad, null) - cardsA : 0;
        
        int cardsD = combatManager.GetCardsOfType(AffinityType.Destreza);
        float itemD = combatManager.statsManager != null ? combatManager.statsManager.GetFinalStat(StatType.Destreza, null) - cardsD : 0;

        fuerzaMainText.text = $"ATACAR CON FUERZA\nPoder: {cardsF + itemF} (Cartas: {cardsF} + Items: {itemF})";
        agilidadMainText.text = $"ATACAR CON AGILIDAD\nPoder: {cardsA + itemA} (Cartas: {cardsA} + Items: {itemA})";
        destrezaMainText.text = $"ATACAR CON DESTREZA\nPoder: {cardsD + itemD} (Cartas: {cardsD} + Items: {itemD})";
    }

    void UpdateButtonColor(Button button, AffinityType type, EnemyData enemy)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) return;

        if (!isEasyModeEnabled)
        {
            buttonImage.color = colorNeutral;
            return;
        }

        bool isDiscovered = false;
        if (BestiaryManager.Instance != null)
        {
            isDiscovered = BestiaryManager.Instance.IsAffinityDiscovered(enemy.name, type);
        }

        if (!isDiscovered)
        {
            buttonImage.color = colorDesconocido;
            return;
        }

        AffinityMultiplier multiplier = AffinityMultiplier.Neutral;

        foreach (var relation in enemy.affinityRelations)
        {
            if (relation.type == type)
            {
                multiplier = relation.multiplier;
                break;
            }
        }

        buttonImage.color = multiplier switch
        {
            AffinityMultiplier.Weak => colorDebilidad,
            AffinityMultiplier.Strong => colorResistencia,
            AffinityMultiplier.Immune => colorInmunidad,
            _ => colorNeutral
        };
    }

    public void UpdateAffinitiesUI()
    {
        EnemyData enemy = combatManager.GetCurrentEnemy().enemyData;
        UpdateButtonColor(fuerzaMainButton, AffinityType.Fuerza, enemy);
        UpdateButtonColor(agilidadMainButton, AffinityType.Agilidad, enemy);
        UpdateButtonColor(destrezaMainButton, AffinityType.Destreza, enemy);
    }

    void UpdatePlayerLifeUI()
    {
        if (vidaActualText != null && combatManager.playerManager != null)
        {
            int currentLife = combatManager.GetPlayerLife();
            int maxLife = combatManager.GetPlayerMaxLife();
            vidaActualText.text = $"Vida: {currentLife}/{maxLife}";
        }
    }

    void UpdatePlayerExtraStatsUI()
    {
        if (combatManager.playerManager == null || combatManager.statsManager == null) return;

        if (armaduraJugadorText != null)
        {
            armaduraJugadorText.text = combatManager.playerManager.ActiveArmor.ToString();
        }
        
        // Asumiendo que Fill es de 0 a 1 basado en algo. Para la armadura podemos poner max 50 por ejemplo.
        if (armaduraJugadorFill != null)
        {
            armaduraJugadorFill.fillAmount = Mathf.Clamp01(combatManager.playerManager.ActiveArmor / 50f);
        }

        if (roboVidaText != null)
        {
            roboVidaText.text = $"Robo de Vida: {combatManager.statsManager.GetFinalStat(StatType.RoboVida, null)}";
        }
    }

    // ========== NUEVO: SISTEMA DE FLUJO SECUENCIAL ==========

    /// <summary>
    /// Cuando el jugador presiona "Continuar" en el panel de victoria
    /// </summary>
    void OnVictoryPanelContinue()
    {
        passiveEndPanel.SetActive(false);
        
        // Verificar si hay evento de maldición
        if (combatManager.ShouldShowCurseEvent())
        {
            ShowCurseNotification();
        }
        else
        {
            // No hay maldición, ir directo al siguiente enemigo
            if (bossRushManager != null)
            {
                combatManager.ResetPostCombatFlag();
                bossRushManager.ContinueToNextCombat();
            }
            else
            {
                Debug.LogError("BossRushManager no asignado en CombatManagerUI");
            }
        }
    }

    /// <summary>
    /// Cuando el jugador presiona "Continuar" en el panel de derrota
    /// </summary>
    void OnDefeatPanelContinue()
    {
        defeatPanel.SetActive(false);
        
        if (bossRushManager != null)
        {
            combatManager.ResetPostCombatFlag();
            bossRushManager.ContinueToNextCombat();
        }
        else
        {
            Debug.LogError("BossRushManager no asignado en CombatManagerUI");
        }
    }

    /// <summary>
    /// Muestra el panel de notificación "¡Has sido maldecido!"
    /// </summary>
    void ShowCurseNotification()
    {
        // Ocultar UI de combate
        if (combatUICanvasGroup != null)
        {
            combatUICanvasGroup.alpha = 0f;
            combatUICanvasGroup.interactable = false;
            combatUICanvasGroup.blocksRaycasts = false;
        }
        
        if (curseNotificationPanel != null)
        {
            curseNotificationPanel.SetActive(true);
            if (curseNotificationText != null)
            {
                curseNotificationText.text = "¡Has sido MALDECIDO!\n\nElige una carta...";
            }
        }
        else
        {
            // Fallback: ir directo al evento de selección
            OnCurseNotificationContinue();
        }
    }

    /// <summary>
    /// Cuando el jugador presiona "Continuar" en el panel de notificación
    /// </summary>
    void OnCurseNotificationContinue()
    {
        if (curseNotificationPanel != null)
        {
            curseNotificationPanel.SetActive(false);
        }
        
        // Activar el evento de selección de 3 cartas
        combatManager.TriggerCurseEventFromUI();
        
        // La secuencia continuará cuando el jugador seleccione una carta en CurseChoiceUI
    }

    // ========== NUEVO: SISTEMA DE BOTON VOLVER ==========

    /// <summary>
    /// Cuando el jugador presiona el botón Volver
    /// </summary>
    void OnBackButtonPressed()
    {
        // Ocultar paneles de habilidades
        HideAllAbilityPanels();
        currentExpandedType = null;
        
        // Mostrar botones principales
        ShowMainButtons();
        
        // Ocultar botón Volver
        HideBackButton();
    }

    /// <summary>
    /// Oculta los botones principales de selección de tipo
    /// </summary>
    void HideMainButtons()
    {
        if (fuerzaMainButton != null)
            fuerzaMainButton.gameObject.SetActive(false);
        
        if (agilidadMainButton != null)
            agilidadMainButton.gameObject.SetActive(false);
        
        if (destrezaMainButton != null)
            destrezaMainButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Muestra los botones principales de selección de tipo
    /// </summary>
    void ShowMainButtons()
    {
        if (fuerzaMainButton != null)
            fuerzaMainButton.gameObject.SetActive(true);
        
        if (agilidadMainButton != null)
            agilidadMainButton.gameObject.SetActive(true);
        
        if (destrezaMainButton != null)
            destrezaMainButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Muestra el botón Volver
    /// </summary>
    void ShowBackButton()
    {
        if (backButton != null)
            backButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Oculta el botón Volver
    /// </summary>
    void HideBackButton()
    {
        if (backButton != null)
            backButton.gameObject.SetActive(false);
    }
}

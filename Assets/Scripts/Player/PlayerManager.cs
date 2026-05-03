using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Gestor centralizado de todos los datos y estado del jugador
/// Maneja: vida, cartas, score, estadisticas
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Vida del Jugador")]
    [SerializeField] private int maxLife = 100;
    [SerializeField] private int currentLife;

    public int CurrentLife => currentLife;
    public int MaxLife => maxLife;

    [Header("Inventario de Cartas")]
    [SerializeField] private int cartasFuerza = 0;
    [SerializeField] private int cartasAgilidad = 0;
    [SerializeField] private int cartasDestreza = 0;

    [Header("Puntuacion")]
    [SerializeField] private int score = 0;

    [Header("Tinta (recurso económico)")]
    [SerializeField] private int inkAmount = 0;

    [Header("Estadisticas")]
    [SerializeField] private int enemiesDefeated = 0;
    [SerializeField] private int combatsWon = 0;
    [SerializeField] private int combatsLost = 0;

    // Eventos
    public event Action<int, int> OnHealthChanged; // (currentLife, maxLife)
    public event Action<AffinityType, int> OnCardsChanged; // (type, newAmount)
    public event Action<int> OnScoreChanged; // (newScore)
    public event Action<int> OnInkChanged;   // (newInkAmount)
    public event Action OnPlayerDeath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Inicializa el jugador para una nueva run
    /// </summary>
    public void InitializeForNewRun(int initialCards = 5)
    {
        // Resetear vida
        currentLife = maxLife;

        // Resetear cartas
        cartasFuerza = 0;
        cartasAgilidad = 0;
        cartasDestreza = 0;

        // Dar cartas iniciales aleatorias
        for (int i = 0; i < initialCards; i++)
        {
            AffinityType randomType = GetRandomAffinityType();
            AddCards(randomType, 1);
            Debug.Log("Carta inicial " + (i + 1) + ": " + randomType);
        }

        // Resetear puntuacion
        score = 0;

        // Resetear tinta
        inkAmount = 0;

        // Resetear estadisticas
        enemiesDefeated = 0;
        combatsWon = 0;
        combatsLost = 0;

        // Notificar cambios
        OnHealthChanged?.Invoke(currentLife, maxLife);
        OnScoreChanged?.Invoke(score);
        OnInkChanged?.Invoke(inkAmount);

        Debug.Log("PlayerManager inicializado - Vida: " + currentLife + "/" + maxLife);
    }

    // ========== GESTION DE VIDA ==========

    /// <summary>
    /// Modifica la vida del jugador
    /// </summary>
    public void ModifyHealth(int amount)
    {
        int previousLife = currentLife;
        currentLife += amount;
        currentLife = Mathf.Clamp(currentLife, 0, maxLife);

        Debug.Log("Vida modificada: " + amount + " (de " + previousLife + " a " + currentLife + ")");

        OnHealthChanged?.Invoke(currentLife, maxLife);

        if (currentLife <= 0)
        {
            Debug.Log("Jugador muerto");
            OnPlayerDeath?.Invoke();
        }
    }

    /// <summary>
    /// Modifica la vida máxima del jugador
    /// </summary>
    public void ModifyMaxHealth(int amount)
    {
        int oldMax = maxLife;
        maxLife += amount;
        if (maxLife < 1) maxLife = 1; // Seguridad

        // Si la vida máxima baja, ajustar vida actual si es necesario
        if (currentLife > maxLife)
        {
            currentLife = maxLife;
        }

        Debug.Log($"Máximo de vida modificado: {oldMax} -> {maxLife}");
        OnHealthChanged?.Invoke(currentLife, maxLife);
    }

    /// <summary>
    /// Establece la vida del jugador directamente
    /// </summary>
    public void SetHealth(int value)
    {
        currentLife = Mathf.Clamp(value, 0, maxLife);
        OnHealthChanged?.Invoke(currentLife, maxLife);

        if (currentLife <= 0)
        {
            OnPlayerDeath?.Invoke();
        }
    }

    /// <summary>
    /// Cura al jugador completamente
    /// </summary>
    public void HealToFull()
    {
        ModifyHealth(maxLife);
    }

    // ========== GESTION DE CARTAS ==========

    /// <summary>
    /// Añade cartas de un tipo
    /// </summary>
    public void AddCards(AffinityType type, int amount)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case AffinityType.Fuerza: cartasFuerza += amount; break;
            case AffinityType.Agilidad: cartasAgilidad += amount; break;
            case AffinityType.Destreza: cartasDestreza += amount; break;
        }

        int total = GetCards(type);
        Debug.Log("+" + amount + " carta(s) de " + type + ". Total: " + total);
        OnCardsChanged?.Invoke(type, total);
    }

    /// <summary>
    /// Quita cartas de un tipo
    /// </summary>
    public bool RemoveCards(AffinityType type, int amount)
    {
        if (amount <= 0) return true;
        if (GetCards(type) < amount) return false;

        switch (type)
        {
            case AffinityType.Fuerza: cartasFuerza -= amount; break;
            case AffinityType.Agilidad: cartasAgilidad -= amount; break;
            case AffinityType.Destreza: cartasDestreza -= amount; break;
        }

        int total = GetCards(type);
        Debug.Log("-" + amount + " carta(s) de " + type + ". Total: " + total);
        OnCardsChanged?.Invoke(type, total);
        return true;
    }

    /// <summary>
    /// Obtiene la cantidad de cartas de un tipo
    /// </summary>
    public int GetCards(AffinityType type)
    {
        switch (type)
        {
            case AffinityType.Fuerza: return cartasFuerza;
            case AffinityType.Agilidad: return cartasAgilidad;
            case AffinityType.Destreza: return cartasDestreza;
            default: return 0;
        }
    }

    /// <summary>
    /// Obtiene todas las cartas
    /// </summary>
    public Dictionary<AffinityType, int> GetAllCards()
    {
        return new Dictionary<AffinityType, int>()
        {
            { AffinityType.Fuerza, cartasFuerza },
            { AffinityType.Agilidad, cartasAgilidad },
            { AffinityType.Destreza, cartasDestreza }
        };
    }

    /// <summary>
    /// Verifica si tiene suficientes cartas
    /// </summary>
    public bool HasCards(AffinityType type, int amount)
    {
        return GetCards(type) >= amount;
    }

    /// <summary>
    /// Quita una carta aleatoria (para maldiciones)
    /// </summary>
    public bool RemoveRandomCard(out AffinityType removedType)
    {
        removedType = default;

        // Obtener tipos con cartas disponibles
        List<AffinityType> availableTypes = new List<AffinityType>();
        if (cartasFuerza > 0) availableTypes.Add(AffinityType.Fuerza);
        if (cartasAgilidad > 0) availableTypes.Add(AffinityType.Agilidad);
        if (cartasDestreza > 0) availableTypes.Add(AffinityType.Destreza);

        if (availableTypes.Count == 0)
        {
            Debug.Log("No hay cartas para quitar");
            return false;
        }

        AffinityType randomType = availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];
        RemoveCards(randomType, 1);
        removedType = randomType;
        return true;
    }

    // ========== GESTION DE PUNTUACION ==========

    /// <summary>
    /// Añade puntos al score
    /// </summary>
    public void AddScore(int points)
    {
        if (points <= 0) return;

        score += points;
        Debug.Log("+" + points + " puntos. Score total: " + score);
        OnScoreChanged?.Invoke(score);
    }

    /// <summary>
    /// Establece el score directamente
    /// </summary>
    public void SetScore(int value)
    {
        score = Mathf.Max(0, value);
        OnScoreChanged?.Invoke(score);
    }

    // ========== ESTADISTICAS ==========

    /// <summary>
    /// Registra un enemigo derrotado
    /// </summary>
    public void RegisterEnemyDefeated()
    {
        enemiesDefeated++;
        Debug.Log("Enemigos derrotados: " + enemiesDefeated);
    }

    /// <summary>
    /// Registra un combate ganado
    /// </summary>
    public void RegisterCombatWon()
    {
        combatsWon++;
    }

    /// <summary>
    /// Registra un combate perdido
    /// </summary>
    public void RegisterCombatLost()
    {
        combatsLost++;
    }

    // ========== GETTERS ==========

    public int GetCurrentLife() => currentLife;
    public int GetMaxLife() => maxLife;
    public int GetScore() => score;
    public int GetEnemiesDefeated() => enemiesDefeated;
    public int GetCombatsWon() => combatsWon;
    public int GetCombatsLost() => combatsLost;
    public int GetInk() => inkAmount;

    public void AddInk(int amount)
    {
        if (amount <= 0) return;
        inkAmount += amount;
        Debug.Log($"+{amount} Tinta. Total: {inkAmount}");
        OnInkChanged?.Invoke(inkAmount);
    }

    public void SpendInk(int amount)
    {
        inkAmount = Mathf.Max(0, inkAmount - amount);
        Debug.Log($"-{amount} Tinta. Total: {inkAmount}");
        OnInkChanged?.Invoke(inkAmount);
    }

    public bool IsAlive() => currentLife > 0;

    // ========== HELPERS ==========

    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    /// <summary>
    /// Debug: Muestra el estado actual del jugador
    /// </summary>
    public void DebugPrintState()
    {
        Debug.Log("=== ESTADO DEL JUGADOR ===");
        Debug.Log("Vida: " + currentLife + "/" + maxLife);
        Debug.Log("Score: " + score);
        Debug.Log("Cartas - Fuerza: " + cartasFuerza);
        Debug.Log("Cartas - Agilidad: " + cartasAgilidad);
        Debug.Log("Cartas - Destreza: " + cartasDestreza);
        Debug.Log("Enemigos derrotados: " + enemiesDefeated);
        Debug.Log("========================");
    }
}
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Gestor del Historial de Partidas
/// Guarda los resultados de cada run en PlayerPrefs usando JSON
/// </summary>
public class MatchHistoryManager : MonoBehaviour
{
    public static MatchHistoryManager Instance { get; private set; }

    private const string HISTORY_KEY = "Sveliaty_MatchHistory";
    private const int MAX_HISTORY_ENTRIES = 50;

    [Serializable]
    public class MatchEntry
    {
        public string date;
        public int score;
        public int fuerza;
        public int agilidad;
        public int destreza;
        public bool isVictory;
        public string defeatedBy; // Nombre del enemigo que lo derrotó o el Boss final

        public MatchEntry(int score, int f, int a, int d, bool victory, string enemy)
        {
            this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            this.score = score;
            this.fuerza = f;
            this.agilidad = a;
            this.destreza = d;
            this.isVictory = victory;
            this.defeatedBy = enemy;
        }
    }

    [Serializable]
    private class MatchHistoryList
    {
        public List<MatchEntry> entries = new List<MatchEntry>();
    }

    private MatchHistoryList history = new MatchHistoryList();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHistory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Guarda una partida en el historial
    /// </summary>
    public void SaveMatch(int score, int f, int a, int d, bool victory, string enemy)
    {
        MatchEntry newEntry = new MatchEntry(score, f, a, d, victory, enemy);
        
        // Insertar al principio (para que el más reciente salga primero)
        history.entries.Insert(0, newEntry);

        // Limitar tamaño
        if (history.entries.Count > MAX_HISTORY_ENTRIES)
        {
            history.entries.RemoveAt(history.entries.Count - 1);
        }

        SaveToPrefs();
        Debug.Log("Partida guardada en el historial");
    }

    public List<MatchEntry> GetHistory()
    {
        return history.entries;
    }

    private void SaveToPrefs()
    {
        string json = JsonUtility.ToJson(history);
        PlayerPrefs.SetString(HISTORY_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadHistory()
    {
        if (PlayerPrefs.HasKey(HISTORY_KEY))
        {
            string json = PlayerPrefs.GetString(HISTORY_KEY);
            try
            {
                history = JsonUtility.FromJson<MatchHistoryList>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("Error cargando historial: " + e.Message);
                history = new MatchHistoryList();
            }
        }
        else
        {
            history = new MatchHistoryList();
        }
    }

    public void ClearHistory()
    {
        history.entries.Clear();
        PlayerPrefs.DeleteKey(HISTORY_KEY);
        PlayerPrefs.Save();
        Debug.Log("Historial borrado");
    }
}

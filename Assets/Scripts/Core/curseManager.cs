using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class CurseManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private CurseDatabase curseDatabase;

    [Header("Curse Event Settings")]
    [Range(0, 100)] public float baseCurseChance = 5f;
    [Range(0, 100)] public float curseChancePerTurn = 5f;
    [Range(0, 100)] public float spiritCurseChance = 80f;

    [Header("Debug Inspector")]
    [SerializeField] private List<CurseInstance> activeCurses = new List<CurseInstance>();
    private int combatsWithoutRewards = 0;

    public event Action<CurseData> OnCurseObtained;
    public event Action<CurseData> OnCurseActivated;
    public event Action<List<CurseData>> OnCurseChoiceEvent;

    void OnEnable()
    {
        if (playerManager != null)
            playerManager.OnPlayerDeath += OnDefeat;
    }

    void OnDisable()
    {
        if (playerManager != null)
            playerManager.OnPlayerDeath -= OnDefeat;
    }

    // =========================
    // PROBABILIDAD DE EVENTO
    // =========================

    public bool ShouldTriggerCurseEvent(int turnsUsed, bool isSpirit)
    {
        if (isSpirit)
            return UnityEngine.Random.Range(0f, 100f) < spiritCurseChance;

        float chance = baseCurseChance + (turnsUsed * curseChancePerTurn);
        return UnityEngine.Random.Range(0f, 100f) < chance;
    }

    public void TriggerCurseChoiceEvent()
    {
        if (curseDatabase == null)
        {
            Debug.LogError("CurseDatabase no asignada.");
            return;
        }

        List<CurseData> options = curseDatabase.GetThreeRandomCurses();

        if (OnCurseChoiceEvent == null)
        {
            Debug.LogWarning("OnCurseChoiceEvent no tiene suscriptores.");
            return;
        }

        OnCurseChoiceEvent.Invoke(options);
    }

    // =========================
    // OBTENER MALDICIÓN
    // =========================

    public string ObtainCurse(CurseData curse)
    {
        if (curse == null) return "";

        string executionDetails = "";

        Debug.Log($"Maldición obtenida: {curse.curseName}");

        // NegateDamage y NegateDeathBlow son escudos que deben persistir en
        // activeCurses para ser detectados al final del combate o al morir.
        // Aunque su activationType sea Instant, NO los consumimos aquí.
        if (curse.effectType == CurseEffect.NegateDamage ||
            curse.effectType == CurseEffect.NegateDeathBlow)
        {
            activeCurses.Add(new CurseInstance(curse));
        }
        else if (curse.activationType == CurseActivationType.Instant)
        {
            executionDetails = ApplyInstantEffect(curse);
        }
        else
        {
            activeCurses.Add(new CurseInstance(curse));
        }

        OnCurseObtained?.Invoke(curse);
        
        return executionDetails;
    }

    string ApplyInstantEffect(CurseData curse)
    {
        if (playerManager == null)
        {
            Debug.LogError("PlayerManager no asignado.");
            return "";
        }

        string details = "";

        switch (curse.effectType)
        {
            case CurseEffect.ModifyHealth:
                int oldLife = playerManager.CurrentLife;
                playerManager.ModifyHealth(curse.effectValue);
                int newLife = playerManager.CurrentLife;
                
                string lifeColor = curse.effectValue > 0 ? "green" : "red";
                details = $"\n\n<b><color={lifeColor}>Vida: {oldLife} → {newLife}</color></b>";
                break;

            case CurseEffect.ModifyMaxHealth:
                int oldMaxLife = playerManager.MaxLife;
                playerManager.ModifyMaxHealth(curse.effectValue);
                int newMaxLife = playerManager.MaxLife;
                
                string maxLifeColor = curse.effectValue > 0 ? "green" : "red";
                details = $"\n\n<b><color={maxLifeColor}>Vida Máxima: {oldMaxLife} → {newMaxLife}</color></b>";
                break;

            case CurseEffect.ModifyCards:
                if (curse.effectValue > 0)
                {
                    AffinityType randomType = GetRandomAffinityType();
                    playerManager.AddCards(randomType, curse.effectValue);
                    details = $"\n\n<b><color=green>Obtuviste {curse.effectValue} carta(s) de {randomType}</color></b>";
                }
                else
                {
                    int amountToRemove = Mathf.Abs(curse.effectValue);
                    List<string> lostCards = new List<string>();

                    for (int i = 0; i < amountToRemove; i++)
                    {
                        if (playerManager.RemoveRandomCard(out AffinityType removedType))
                        {
                            lostCards.Add(removedType.ToString());
                        }
                        else break;
                    }
                    
                    if (lostCards.Count > 0)
                    {
                        details = $"\n\n<b><color=red>Perdiste: {string.Join(", ", lostCards)}</color></b>";
                    }
                    else
                    {
                        details = "\n\n<b><color=gray>No tenías cartas para perder.</color></b>";
                    }
                }
                break;

            case CurseEffect.GamblingDice:
                int roll = UnityEngine.Random.Range(1, 13);
                int effect = (roll % 2 == 0) ? roll : -roll;

                int gOldLife = playerManager.CurrentLife;
                playerManager.ModifyHealth(effect);
                int gNewLife = playerManager.CurrentLife;

                string sign = effect > 0 ? "+" : "";
                string gColor = effect > 0 ? "green" : "red";
                
                details = $"\n\nLanzaste un {roll}.\n<b><color={gColor}>Vida: {gOldLife} {sign}{effect} → {gNewLife}</color></b>";

                Debug.Log($"Gambling: {roll} → {(effect > 0 ? "+" : "")}{effect} HP");
                break;
        }
        
        return details;
    }

    // =========================
    // FASES DE COMBATE
    // =========================

    public void OnPreCombat(EnemyInstance enemy)
    {
        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.PreCombat) continue;

            switch (curse.data.effectType)
            {
                case CurseEffect.WeakenEnemy:
                    enemy.currentRPGHealth = Mathf.RoundToInt(
                        enemy.currentRPGHealth * curse.data.enemyHealthMultiplier
                    );
                    break;

                case CurseEffect.EnemyStartsWithArmor:
                    enemy.activeArmor += curse.data.effectValue;
                    Debug.Log($"[Curse] Enemigo empieza con {curse.data.effectValue} armadura.");
                    break;

                case CurseEffect.InvertVictoryCondition:
                    Debug.Log("Condición de victoria invertida");
                    break;
            }

            ReduceDuration(curse);
        }
    }

    public void OnTurnStart()
    {
        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.TurnStart) continue;

            switch (curse.data.effectType)
            {
                case CurseEffect.NegateCards:
                    Debug.Log("Cartas negadas este turno");
                    break;
            }

            ReduceDuration(curse);
        }
    }

    public void OnPostCombat(bool victory)
    {
        if (!victory) return;

        foreach (var curse in activeCurses.ToList())
        {
            if (curse.data.activationType != CurseActivationType.PostCombat) continue;

            ReduceDuration(curse);
        }
    }

    void ReduceDuration(CurseInstance curse)
    {
        // duration = -1 → permanente (no se reduce)
        // duration =  0 → un solo uso: se aplica y se elimina de inmediato
        // duration >  0 → X combates/turnos: se decrementa y se elimina al llegar a 0
        if (curse.remainingDuration == -1) return;

        if (curse.remainingDuration == 0)
        {
            Debug.Log($"[Curse] Eliminando maldición de un solo uso: {curse.data.curseName}");
            activeCurses.Remove(curse);
            return;
        }

        curse.remainingDuration--;
        Debug.Log($"[Curse] Reduciendo duración de {curse.data.curseName}. Restante: {curse.remainingDuration}");
        
        if (curse.remainingDuration == 0)
        {
            Debug.Log($"[Curse] Duración agotada para: {curse.data.curseName}. Eliminando.");
            activeCurses.Remove(curse);
        }
    }

    // =========================
    // ACTIVACIÓN MANUAL
    // =========================

    public bool CanActivateCurse(CurseData curse, int currentTurn)
    {
        if (!curse.requiresPlayerActivation) return false;

        var instance = activeCurses.FirstOrDefault(c => c.data == curse);
        if (instance == null) return false;

        if (curse.mustActivateOnTurnOne && currentTurn != 1) return false;

        return !instance.isActivated;
    }

    public void ActivateCurse(CurseData curse)
    {
        var instance = activeCurses.FirstOrDefault(c => c.data == curse);
        if (instance == null) return;

        instance.isActivated = true;
        OnCurseActivated?.Invoke(curse);

        switch (curse.effectType)
        {
            case CurseEffect.EscapeCombat:
                activeCurses.Remove(instance);
                break;

            case CurseEffect.NegateDamage:
                break;
        }
    }

    // =========================
    // VERIFICADORES
    // =========================

    public bool HasInvertedVictoryCondition()
    {
        return activeCurses.Any(c =>
            c.data.effectType == CurseEffect.InvertVictoryCondition &&
            c.remainingDuration != 0);
    }

    public bool HasNegatedCards()
    {
        return activeCurses.Any(c =>
            c.data.effectType == CurseEffect.NegateCards &&
            c.remainingDuration != 0);
    }

    public bool HasDamageNegation()
    {
        // Basta con que la maldición esté en la lista activa.
        // isActivated era un requisito incorrecto: nunca se asignaba desde ObtainCurse.
        return activeCurses.Any(c =>
            c.data.effectType == CurseEffect.NegateDamage);
    }

    public void ConsumeNegateDamage()
    {
        var shield = activeCurses.FirstOrDefault(c =>
            c.data.effectType == CurseEffect.NegateDamage);

        if (shield != null)
        {
            Debug.Log($"[Curse] Escudo consumido: {shield.data.curseName}");
            activeCurses.Remove(shield);
        }
    }

    public bool HasRewardBlock()
    {
        return combatsWithoutRewards > 0;
    }

    // =========================
    // ESCUDO DE MUERTE
    // =========================

    public bool CheckAndConsumeDeathNegation()
    {
        var shield = activeCurses.FirstOrDefault(c =>
            c.data.effectType == CurseEffect.NegateDeathBlow);

        if (shield != null)
        {
            Debug.Log("[Curse] Escudo de muerte detectado y consumido.");
            activeCurses.Remove(shield);
            playerManager.SetHealth(1);
            return true;
        }
        return false;
    }

    public void OnDefeat()
    {
        // OnDefeat ahora se usa para otros efectos de post-derrota.
        // La negacion de muerte se maneja explicitamente en CombatManager solo si el daño es letal.
    }

    // =========================
    // HELPERS
    // =========================

    AffinityType GetRandomAffinityType()
    {
        AffinityType[] allTypes = (AffinityType[])Enum.GetValues(typeof(AffinityType));
        return allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
    }

    public List<CurseInstance> GetActiveCurses() => activeCurses;

#if UNITY_EDITOR
    // ╔══════════════════════════════════════════════════════════════╗
    // ║  DEBUG REROLL — Devuelve 3 nuevas maldiciones aleatorias     ║
    // ║  Usado por CurseChoiceUI cuando se mantiene R en el Editor.  ║
    // ║  Eliminar este bloque junto con la lógica en CurseChoiceUI   ║
    // ║  antes de la build final.                                    ║
    // ╚══════════════════════════════════════════════════════════════╝
    public List<CurseData> DebugGetNewCurseOptions()
    {
        if (curseDatabase == null)
        {
            Debug.LogError("[DEBUG] CurseDatabase no asignada en CurseManager.");
            return new List<CurseData>();
        }
        return curseDatabase.GetThreeRandomCurses();
    }
#endif
}

using UnityEngine;

/// <summary>
/// Clase base para cualquier acción realizada por el jugador durante su turno de combate.
/// </summary>
public abstract class CombatAction
{
    public abstract string ActionName { get; }
    public abstract AffinityType ActionAffinity { get; }
    
    public abstract int CardCost { get; }
    public abstract int HealthCost { get; }
    public abstract int TurnCost { get; }

    /// <summary>
    /// Verifica si la acción puede ser ejecutada con los recursos actuales.
    /// </summary>
    public virtual bool CanExecute(CombatManager manager)
    {
        if (manager.playerManager.GetCurrentLife() <= HealthCost)
        {
            Debug.LogWarning($"[CombatAction] Vida insuficiente para {ActionName}");
            return false;
        }

        if (manager.abilityManager != null && CardCost > 0)
        {
            if (!manager.playerManager.HasCards(ActionAffinity, CardCost))
            {
                Debug.LogWarning($"[CombatAction] Cartas insuficientes para {ActionName}");
                return false;
            }
        }

        if (manager.GetCurrentEnemy().attemptsRemaining < TurnCost)
        {
            Debug.LogWarning($"[CombatAction] Turnos insuficientes para {ActionName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Realiza los cálculos y notifica al CombatManager el resultado del ataque.
    /// Retorna TRUE si la acción fue exitosa, FALSE si falló (ej. chance de fallo).
    /// </summary>
    public abstract bool Execute(CombatManager manager, TurnContext context);
    
    /// <summary>
    /// Ejecuta efectos posteriores al éxito de la acción (como OnKill/OnHit si aplica).
    /// </summary>
    public virtual void OnActionSuccess(CombatManager manager) { }

    /// <summary>
    /// Ejecuta efectos posteriores al fallo de la acción.
    /// </summary>
    public virtual void OnActionFail(CombatManager manager) { }
    
    /// <summary>
    /// Maneja el descuento de turnos o efectos que previenen el descuento.
    /// Retorna TRUE si se consumió al menos un turno.
    /// </summary>
    public virtual bool ConsumeTurn(CombatManager manager)
    {
        manager.GetCurrentEnemy().attemptsRemaining -= TurnCost;
        manager.NotifyAttemptsChanged();
        return TurnCost > 0;
    }
}

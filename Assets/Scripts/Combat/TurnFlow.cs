using System.Collections.Generic;

public enum TurnPhase
{
    TurnStart,
    ActionSelection,
    Resolution,
    TurnEnd,
    EnemyTurn // NUEVA FASE: El enemigo realiza sus acciones pasivas
}

public class TurnContext
{
    public int TurnNumber;
    public CombatAction UsedAction; 
    public AffinityType ActionAffinity;
    
    public Dictionary<AffinityType, int> InitialCards;

    public TurnContext(int turnNumber)
    {
        TurnNumber = turnNumber;
        InitialCards = new Dictionary<AffinityType, int>();
    }
}

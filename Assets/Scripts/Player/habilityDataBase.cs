using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Sveliaty/Data Base/Ability Database")]
public class AbilityDatabase : ScriptableObject
{
    public List<AbilityData> allAbilities;
    
    public List<AbilityData> GetAbilitiesByAffinity(AffinityType type)
    {
        return allAbilities.Where(a => a.affinityType == type).ToList();
    }
    
    public AbilityData GetAbilityByName(string abilityName)
    {
        return allAbilities.FirstOrDefault(a => a != null && a.name == abilityName);
    }
}
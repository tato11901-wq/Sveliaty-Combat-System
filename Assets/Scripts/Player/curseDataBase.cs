using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Sveliaty/Data Base/Curse Database")]
public class CurseDatabase : ScriptableObject
{
    public List<CurseData> allCurses;
    
    public CurseData GetRandomCurse()
    {
        return allCurses[Random.Range(0, allCurses.Count)];
    }
    
    public List<CurseData> GetThreeRandomCurses()
    {
        
        List<CurseData> result = new List<CurseData>();
        for (int i = 0; i < 3; i++)
        {
            result.Add(GetRandomCurse());
            Debug.Log("Curse added: " + result[i].curseName);
        }
        return result;
    }

    public CurseData GetCurseByName(string curseName)
    {
        foreach(var c in allCurses)
        {
            if (c != null && c.name == curseName) return c;
        }
        return null;
    }
}
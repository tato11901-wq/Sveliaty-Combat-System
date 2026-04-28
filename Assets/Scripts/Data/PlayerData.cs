using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contenedor puro de datos del jugador.
/// No incluye lógica de cálculos de stats finales ni aplica efectos,
/// solo almacena el estado según el GDD.
/// </summary>
[Serializable]
public class PlayerData
{
    [Header("Salud")]
    public int currentLife;
    public int maxLife;

    [Header("Economía")]
    public int inkAmount; // Recurso para comprar ítems en la tienda

    [Header("Cartas")]
    // Diccionario para almacenar las cartas por rama. 
    // Usamos el enum existente AffinityType que contiene (Fuerza, Agilidad, Destreza)
    public Dictionary<AffinityType, int> cardsPerBranch;

    [Header("Progresión y Combate")]
    public List<ItemData> obtainedItems;
    public List<ActiveSkillState> activeSkills;

    public PlayerData(int startingMaxLife = 100)
    {
        maxLife = startingMaxLife;
        currentLife = startingMaxLife;
        inkAmount = 0;

        cardsPerBranch = new Dictionary<AffinityType, int>()
        {
            { AffinityType.Fuerza, 0 },
            { AffinityType.Agilidad, 0 }, // Agilidad corresponde a Velocidad en el GDD
            { AffinityType.Destreza, 0 }
        };

        obtainedItems = new List<ItemData>();
        activeSkills = new List<ActiveSkillState>();
    }
}

/// <summary>
/// Estado de una habilidad activa, incluye su Tier (1 a 3) como especifica el GDD.
/// </summary>
[Serializable]
public class ActiveSkillState
{
    public AbilityData ability;
    [Range(1, 3)]
    public int tier = 1;
}

/// <summary>
/// Representación de un ítem de estadística de la tienda. 
/// Es solo una fuente de datos limpia para que otros sistemas apliquen los cálculos.
/// </summary>
[CreateAssetMenu(fileName = "NewItemData", menuName = "Sveliaty/Data/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea] public string description;

    [Header("Modificadores de Estadísticas (Principales)")]
    public int bonusFuerza;
    public int bonusVelocidad; // Corresponde a Agilidad en AffinityType
    public int bonusDestreza;

    [Header("Modificadores de Estadísticas (Secundarias)")]
    public int bonusArmadura;
    public int bonusProbCritico;
    public int bonusRoboVida;
}

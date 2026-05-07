using UnityEngine;
using System.Collections.Generic;

public class TestCharacterVisual : MonoBehaviour
{
    public CharacterVisualController controller;

    [Header("Habilidades de prueba (no-básicas)")]
    public AbilityData habilidadFuerza;   // Fuerza → activa Brazos
    public AbilityData habilidadAgilidad; // Agilidad → activa Torso
    public AbilityData habilidadDestreza; // Destreza → activa Mascara

    [Range(1, 3)] public int tier = 1;

    [Header("Cartas de prueba")]
    public int cartas = 10;

    [ContextMenu("▶ Probar Visual")]
    void ProbarVisual()
    {
        var skills = new List<ActiveSkillState>();
        if (habilidadFuerza   != null) skills.Add(new ActiveSkillState { ability = habilidadFuerza,   tier = tier });
        if (habilidadAgilidad != null) skills.Add(new ActiveSkillState { ability = habilidadAgilidad, tier = tier });
        if (habilidadDestreza != null) skills.Add(new ActiveSkillState { ability = habilidadDestreza, tier = tier });

        controller.RefreshCharacterVisual(skills, cartas, cartas, cartas);
    }

    [ContextMenu("✖ Limpiar todo")]
    void Limpiar() => controller.ClearAll();
}

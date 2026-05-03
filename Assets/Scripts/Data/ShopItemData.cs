using UnityEngine;

/// <summary>
/// ScriptableObject para un ítem de estadísticas vendible en la tienda.
/// Sigue la regla de penalización circular del GDD:
///   Fuerza → resta Destreza
///   Velocidad → resta Fuerza
///   Destreza → resta Velocidad
/// </summary>
[CreateAssetMenu(fileName = "NewShopItem", menuName = "Sveliaty/Shop/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Identificación")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Precio (Tinta)")]
    [Tooltip("Precio base. Escala según ShopSettings.inkPricePerVisit.")]
    public int baseInkPrice = 50;

    [Header("Modificadores de Estadísticas Principales")]
    public int deltaFuerza;
    public int deltaVelocidad;
    public int deltaDestreza;

    [Header("Modificadores de Estadísticas Secundarias")]
    public int deltaArmadura;
    public int deltaProbCritico;
    public int deltaRoboVida;

    [Header("Control de Aparición")]
    [Range(0f, 1f)]
    [Tooltip("Peso relativo de aparición en la tienda (mayor = más frecuente).")]
    public float appearanceWeight = 1f;

    [Tooltip("Si se define, el ítem solo aparece a partir de esta visita a la tienda (0 = siempre).")]
    public int minShopVisit = 0;

    /// <summary>Calcula el precio final según la cantidad de visitas a la tienda.</summary>
    public int GetScaledPrice(int visitCount, int inkPriceScalePerVisit)
    {
        return baseInkPrice + visitCount * inkPriceScalePerVisit;
    }
}

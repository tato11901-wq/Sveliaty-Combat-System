using System.Collections.Generic;

namespace Sveliaty.Core
{
    /// <summary>
    /// Datos globales que persisten entre recargas de escenas de forma estática,
    /// sin necesidad de GameObjects ni DontDestroyOnLoad.
    /// </summary>
    public static class GlobalData
    {
        /// <summary>
        /// Determina si al cargar la escena de combate se debe leer el archivo
        /// de guardado (true) o iniciar una run desde cero (false).
        /// </summary>
        public static bool ShouldLoadSave = false;

        /// <summary>
        /// Datos de victoria que se pasan a la escena del personaje.
        /// Se rellena justo antes de cargar la escena del personaje y se consume al llegar.
        /// </summary>
        public static VictoryData PendingVictoryData = null;
    }

    /// <summary>
    /// Paquete de datos que viaja desde la escena de combate a la escena del personaje.
    /// Contiene todo lo necesario para reconstruir el visual del personaje sin
    /// depender de singletons de la escena anterior.
    /// </summary>
    [System.Serializable]
    public class VictoryData
    {
        public List<ActiveSkillState> activeSkills = new List<ActiveSkillState>();
        public int cardsFuerza;
        public int cardsAgilidad;
        public int cardsDestreza;
        public int finalScore;
    }
}

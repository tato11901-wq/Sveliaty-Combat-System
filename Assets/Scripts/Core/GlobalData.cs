namespace Sveliaty.Core
{
    /// <summary>
    /// Datos globales que persisten entre recargas de escenas de forma estática,
    /// sin necesidad de GameObjects ni DontDestroyOnLoad.
    /// Útil para pasar parámetros de la escena de menú a la de combate.
    /// </summary>
    public static class GlobalData
    {
        /// <summary>
        /// Determina si al cargar la escena de combate se debe leer el archivo
        /// de guardado (true) o iniciar una run desde cero (false).
        /// </summary>
        public static bool ShouldLoadSave = false;
    }
}

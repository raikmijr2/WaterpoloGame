using UnityEngine;

namespace Waterpulse.Core
{
    // Controles elegidos en Opcions durante esta sesión. OptionsMenu lo carga de lo guardado al abrir cada escena y lo
    // cambia al pulsar el botón; HumanBrain y la guía de controles lo leen
    public static class ControlPreferences
    {
        // Teclado + ratón (clic izquierdo tira, derecho pasa, doble clic súper xut). Si no, solo teclado
        public static bool UseMouse { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => UseMouse = true;   // por defecto, ratón
    }
}

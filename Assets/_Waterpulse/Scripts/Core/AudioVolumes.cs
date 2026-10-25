using System;
using UnityEngine;

namespace Waterpulse.Core
{
    // Volúmenes de música y efectos de esta sesión (0-1). Las opciones los cargan de lo guardado y los cambian;
    // el sonido (AudioDirector) los lee. El volumen general es AudioListener.volume
    public static class AudioVolumes
    {
        public static float Music { get; private set; } = 1f;
        public static float Effects { get; private set; } = 1f;

        public static event Action Changed;

        public static void Set(float music, float effects)
        {
            Music = Mathf.Clamp01(music);
            Effects = Mathf.Clamp01(effects);
            Changed?.Invoke();
        }

        // Valores por defecto al entrar en Play aunque el "domain reload" esté desactivado
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Music = 1f;
            Effects = 1f;
            Changed = null;
        }
    }
}

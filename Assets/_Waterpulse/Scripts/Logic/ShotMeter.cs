using UnityEngine;

namespace Waterpulse.Logic
{
    // Barra de tiro estilo NBA: se llena de 0 a 1 mientras se carga y hay una zona verde cerca de arriba.
    // La calidad del tiro es 1 en el verde y baja cuanto más lejos se suelta; si la barra se llena entera,
    // tira solo con calidad 0
    public class ShotMeter
    {
        readonly float duration;
        readonly float greenStart;
        readonly float greenEnd;
        readonly float offGreenMaxQuality;

        public float Fill { get; private set; }
        public bool IsCharging { get; private set; }
        public float GreenStart => greenStart;
        public float GreenEnd => greenEnd;

        public ShotMeter(float duration, float greenStart, float greenWidth, float offGreenMaxQuality)
        {
            this.duration = Mathf.Max(0.01f, duration);
            this.greenStart = Mathf.Clamp01(greenStart);
            greenEnd = Mathf.Clamp(greenStart + greenWidth, this.greenStart, 1f);
            this.offGreenMaxQuality = Mathf.Clamp01(offGreenMaxQuality);
        }

        public void Begin()
        {
            Fill = 0f;
            IsCharging = true;
        }

        public void Cancel()
        {
            Fill = 0f;
            IsCharging = false;
        }

        // Avanza la carga. Devuelve true si la barra se ha llenado entera (hay que tirar ya)
        public bool Tick(float deltaTime)
        {
            if (!IsCharging) return false;
            Fill = Mathf.Min(1f, Fill + deltaTime / duration);
            return Fill >= 1f;
        }

        // Suelta el tiro: devuelve su calidad (0-1) y deja de cargar
        public float Release()
        {
            float quality = Fill >= 1f ? 0f : Quality(Fill);
            Cancel();
            return quality;
        }

        public bool InGreen(float fill) => fill >= greenStart && fill <= greenEnd;

        // 1 dentro del verde; fuera, como mucho offGreenMaxQuality al lado de la zona y 0 en los extremos (0 y 1)
        public float Quality(float fill)
        {
            fill = Mathf.Clamp01(fill);
            if (InGreen(fill)) return 1f;
            if (fill < greenStart)
                return greenStart <= 0f ? 0f : offGreenMaxQuality * fill / greenStart;
            float above = 1f - greenEnd;
            return above <= 0f ? 0f : offGreenMaxQuality * (1f - fill) / above;
        }

        // Potencia del tiro respecto a la normal: minFactor con calidad 0, 1 con calidad 1
        public static float PowerFactor(float quality, float minFactor) => Mathf.Lerp(minFactor, 1f, Mathf.Clamp01(quality));

        // Desvío del tiro (grados): 0 con calidad 1, maxAngle con calidad 0
        public static float ErrorAngle(float quality, float maxAngle) => (1f - Mathf.Clamp01(quality)) * maxAngle;
    }
}

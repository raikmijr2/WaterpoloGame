using UnityEngine;

namespace Waterpulse.Logic
{
    // Multiplicador de combo (GDD sección 8): sube con cada acción positiva encadenada
    // sin perder la pelota y vuelve a ×1 al perder la posesión
    public class ComboMultiplier
    {
        readonly float step;
        readonly float max;

        public int Steps { get; private set; }
        public float Value => Mathf.Min(max, 1f + step * Steps);

        public ComboMultiplier(float step, float max)
        {
            this.step = Mathf.Max(0f, step);
            this.max = Mathf.Max(1f, max);
        }

        public void Advance() => Steps++;
        public void Reset() => Steps = 0;
    }
}

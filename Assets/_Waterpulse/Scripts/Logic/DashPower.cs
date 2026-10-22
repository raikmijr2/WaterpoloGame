using UnityEngine;

namespace Waterpulse.Logic
{
    // Fuerza del dash según la velocidad actual: el dash sirve para recuperar velocidad.
    // Parado es máximo (1); a velocidad máxima no se nota (0)
    public static class DashPower
    {
        public static float Strength(float currentSpeed, float maxSpeed)
        {
            if (maxSpeed <= 0f) return 0f;
            return Mathf.Clamp01(1f - currentSpeed / maxSpeed);
        }

        // Multiplicador de la velocidad máxima durante el dash: de la velocidad actual (fuerza 0)
        // a peakMultiplier (fuerza 1)
        public static float SpeedMultiplier(float currentSpeed, float maxSpeed, float peakMultiplier)
        {
            if (maxSpeed <= 0f) return 0f;
            float current = Mathf.Clamp01(currentSpeed / maxSpeed);
            return Mathf.Lerp(current, peakMultiplier, Strength(currentSpeed, maxSpeed));
        }
    }
}

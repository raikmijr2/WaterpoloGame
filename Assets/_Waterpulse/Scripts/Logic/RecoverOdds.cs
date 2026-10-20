using UnityEngine;

namespace Waterpulse.Logic
{
    // Probabilidad de recuperar la pelota al poseedor según la distancia
    public static class RecoverOdds
    {
        // Máxima a "close" o menos, baja linealmente hasta "chanceFar" en el alcance y es 0 fuera de él
        public static float Chance(float distance, float close, float range, float chanceClose, float chanceFar)
        {
            if (distance > range) return 0f;
            if (distance <= close || range <= close) return Mathf.Clamp01(chanceClose);
            float t = (distance - close) / (range - close);
            return Mathf.Clamp01(Mathf.Lerp(chanceClose, chanceFar, t));
        }
    }
}

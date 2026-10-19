using UnityEngine;

namespace Waterpulse.Logic
{
    // Probabilidad de que el portero pare un tiro según la distancia entre él y la pelota al llegarle
    public static class SaveOdds
    {
        // Máxima si la pelota le llega al cuerpo; baja linealmente hasta 0 a "reach" de su centro
        public static float Chance(float distance, float bodyRadius, float reach, float chanceAtBody)
        {
            if (distance <= bodyRadius) return Mathf.Clamp01(chanceAtBody);
            if (reach <= bodyRadius) return 0f;
            float t = Mathf.Clamp01((distance - bodyRadius) / (reach - bodyRadius));
            return Mathf.Clamp01(chanceAtBody * (1f - t));
        }
    }
}

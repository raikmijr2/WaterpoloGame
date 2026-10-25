using UnityEngine;

namespace Waterpulse.Logic
{
    // Probabilidad de interceptar un pase según lo cerca que pasa la pelota del defensor:
    // máxima si le llega al cuerpo, baja hasta el borde del alcance y es 0 fuera de él
    public static class InterceptOdds
    {
        public static float Chance(float distance, float bodyDistance, float reach, float chanceAtBody, float chanceAtEdge)
        {
            if (distance > reach) return 0f;
            if (distance <= bodyDistance || reach <= bodyDistance) return Mathf.Clamp01(chanceAtBody);
            float t = (distance - bodyDistance) / (reach - bodyDistance);
            return Mathf.Clamp01(Mathf.Lerp(chanceAtBody, chanceAtEdge, t));
        }
    }
}

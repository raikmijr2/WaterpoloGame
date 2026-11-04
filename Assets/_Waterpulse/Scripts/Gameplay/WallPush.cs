using UnityEngine;

namespace Waterpulse.Gameplay
{
    // Empujón de la onada del Mur de Ferro: a quien está dentro del radio alrededor de la portería lo saca hasta
    // el radio más un margen, alejándolo de la portería. Cálculo puro (lo usa WaterWall)
    public static class WallPush
    {
        // Velocidad inicial del empujón (frena hasta 0 en "duration": recorre velocidad × duration / 2).
        // Vector3.zero si ya está fuera. "inward" es la dirección hacia el campo, por si está justo en el centro
        public static Vector3 VelocityFor(Vector3 player, Vector3 goalCenter, Vector3 inward, float radius, float margin, float duration)
        {
            Vector3 away = player - goalCenter;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance >= radius || duration <= 0f) return Vector3.zero;
            // Nunca hacia dentro de la portería: como mínimo, hacia el campo
            Vector3 dir = distance > 0.01f ? away / distance : inward.normalized;
            if (Vector3.Dot(dir, inward) < 0f) dir = (dir - Vector3.Project(dir, inward)).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = inward.normalized;
            float travel = radius + margin - distance;
            return dir * (2f * travel / duration);
        }
    }
}

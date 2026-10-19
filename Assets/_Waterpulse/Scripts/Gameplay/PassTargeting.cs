using System.Collections.Generic;
using UnityEngine;

namespace Waterpulse.Gameplay
{
    // Elige receptor de pase: el candidato más cercano dentro del cono alrededor de la dirección de apuntado
    public static class PassTargeting
    {
        // Devuelve el índice del candidato elegido o -1 si no hay ninguno dentro del cono
        public static int SelectReceiver(Vector3 origin, Vector3 aim, IReadOnlyList<Vector3> candidates, float coneHalfAngle)
        {
            Vector3 flatAim = new Vector3(aim.x, 0f, aim.z);
            if (flatAim.sqrMagnitude < 0.0001f) return -1;
            flatAim.Normalize();

            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 to = candidates[i] - origin;
                to.y = 0f;
                float distance = to.magnitude;
                if (distance < 0.0001f) continue;
                if (Vector3.Angle(flatAim, to) > coneHalfAngle) continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }
    }
}

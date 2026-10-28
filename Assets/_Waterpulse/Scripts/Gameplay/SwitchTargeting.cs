using System.Collections.Generic;
using UnityEngine;

namespace Waterpulse.Gameplay
{
    // Elige a quién pasa el control: el candidato más cercano a la pelota. Lo usan el cambio de jugador (ControlSwitcher)
    // y el HUD, que enseña de antemano a quién irá el control al pulsar Q
    public static class SwitchTargeting
    {
        // Devuelve el índice del candidato más cercano a la pelota sin contar "exclude" (-1 = no excluir), o -1 si no hay
        public static int Nearest(Vector3 ball, IReadOnlyList<Vector3> candidates, int exclude = -1)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (i == exclude) continue;
                float distance = Vector3.Distance(candidates[i], ball);
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

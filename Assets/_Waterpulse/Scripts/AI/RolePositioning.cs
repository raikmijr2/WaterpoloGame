using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.AI
{
    // Dónde debe colocarse un jugador de campo según su rol, la pelota y la posesión (GDD sección 7).
    // Cálculo puro: no mueve nada, solo devuelve el punto objetivo
    public static class RolePositioning
    {
        public struct Pitch
        {
            public Vector3 Center;
            public float HalfLength;
            public float HalfWidth;
        }

        public static Vector3 TargetFor(PlayerRole role, TeamSide team, bool teamHasBall, Vector3 ball,
                                        IReadOnlyList<Vector3> opponents, Pitch pitch, GameConfigSO cfg)
        {
            float sign = team.AttackSign();
            Vector2 offset = Offset(role, teamHasBall, cfg);

            // x: avance respecto a la pelota en el sentido de ataque. y: desplazamiento hacia el centro del campo
            float ballLateral = ball.z - pitch.Center.z;
            float towardCenter = ballLateral > 0f ? -1f : 1f;
            Vector3 target = new Vector3(
                ball.x + sign * offset.x,
                pitch.Center.y,
                pitch.Center.z + ballLateral * cfg.formationLateralFollow + towardCenter * offset.y);

            // Sin posesión: marca al rival más cercano a su posición si está cerca
            if (!teamHasBall && TryNearest(opponents, target, cfg.markRadius, out Vector3 rival))
            {
                Vector3 ownGoal = pitch.Center - Vector3.right * (sign * pitch.HalfLength);
                Vector3 toGoal = ownGoal - rival;
                toGoal.y = 0f;
                target = rival + toGoal.normalized * cfg.markDistance;
                target.y = pitch.Center.y;
            }

            return Clamp(target, sign, pitch, cfg);
        }

        static Vector2 Offset(PlayerRole role, bool teamHasBall, GameConfigSO cfg)
        {
            switch (role)
            {
                case PlayerRole.Attacker: return teamHasBall ? cfg.attackerWithBall : cfg.attackerWithoutBall;
                case PlayerRole.Playmaker: return teamHasBall ? cfg.playmakerWithBall : cfg.playmakerWithoutBall;
                default: return teamHasBall ? cfg.defenderWithBall : cfg.defenderWithoutBall;
            }
        }

        static bool TryNearest(IReadOnlyList<Vector3> points, Vector3 from, float radius, out Vector3 nearest)
        {
            nearest = default;
            float best = radius;
            bool found = false;
            foreach (var p in points)
            {
                float dx = p.x - from.x, dz = p.z - from.z;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > best) continue;
                best = d;
                nearest = p;
                found = true;
            }
            return found;
        }

        // Dentro del campo, sin pegarse a las bandas ni a la línea propia, y sin acercarse demasiado a la portería rival
        static Vector3 Clamp(Vector3 target, float sign, Pitch pitch, GameConfigSO cfg)
        {
            float ownLimit = pitch.HalfLength - cfg.formationEdgeMargin;
            float rivalLimit = pitch.HalfLength - cfg.formationMinGoalDistance;
            float along = (target.x - pitch.Center.x) * sign;   // positivo = hacia la portería rival
            along = Mathf.Clamp(along, -ownLimit, rivalLimit);
            target.x = pitch.Center.x + along * sign;

            float halfW = pitch.HalfWidth - cfg.formationEdgeMargin;
            target.z = Mathf.Clamp(target.z, pitch.Center.z - halfW, pitch.Center.z + halfW);
            return target;
        }
    }
}

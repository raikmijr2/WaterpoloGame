using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.AI
{
    // Dónde debe colocarse un jugador de campo según su rol, la pelota y la posesión (GDD sección 7).
    // Cálculo puro: no mueve nada, solo devuelve el punto objetivo. A quién marca cada uno lo decide TeamAI
    public static class RolePositioning
    {
        public struct Pitch
        {
            public Vector3 Center;
            public float HalfLength;
            public float HalfWidth;
        }

        // Posición de formación por rol
        public static Vector3 TargetFor(PlayerRole role, TeamSide team, bool teamHasBall, Vector3 ball, Pitch pitch, GameConfigSO cfg)
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

            return Clamp(target, sign, pitch, cfg);
        }

        // Posición de marcaje: entre el rival marcado y la portería propia
        public static Vector3 MarkPosition(Vector3 rival, TeamSide team, Pitch pitch, GameConfigSO cfg)
        {
            float sign = team.AttackSign();
            Vector3 ownGoal = pitch.Center - Vector3.right * (sign * pitch.HalfLength);
            Vector3 toGoal = ownGoal - rival;
            toGoal.y = 0f;
            Vector3 target = rival + toGoal.normalized * cfg.markDistance;
            target.y = pitch.Center.y;
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

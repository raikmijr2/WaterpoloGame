using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.AI
{
    public enum SupportSlot { Point, Wing }

    // Juego en triángulo: dónde se colocan los dos compañeros del que lleva la pelota y cómo se desmarcan.
    // - Punta: por delante del poseedor, en el lado del campo con más sitio (el contrario al de la pelota).
    // - Suport: un poco por detrás del poseedor y abierto hacia ese mismo lado: siempre hay un pase seguro.
    // Cada uno prueba puntos alrededor de su sitio y se queda con el mejor: línea de pase limpia, lejos de su
    // defensor, con línea de tiro y sin alejarse demasiado del triángulo. Cálculo puro: no mueve nada
    public static class SupportSpots
    {
        // Sitio base de cada hueco del triángulo
        public static Vector3 SlotPosition(SupportSlot slot, Vector3 holder, TeamSide team, RolePositioning.Pitch pitch, GameConfigSO cfg)
        {
            float sign = team.AttackSign();
            float openSide = OpenSide(holder, pitch);
            Vector3 target = slot == SupportSlot.Point
                ? new Vector3(holder.x + sign * cfg.aiPointAhead, pitch.Center.y, pitch.Center.z + openSide * cfg.aiPointSide)
                : new Vector3(holder.x - sign * cfg.aiWingBack, pitch.Center.y, holder.z + openSide * cfg.aiWingSide);
            return RolePositioning.Clamp(target, sign, pitch, cfg);
        }

        // Lado del campo (en Z) con más sitio: el contrario al del poseedor; en el centro, +Z
        public static float OpenSide(Vector3 holder, RolePositioning.Pitch pitch) => holder.z > pitch.Center.z ? -1f : 1f;

        // El mejor punto alrededor del hueco. "current" es donde iba hasta ahora: solo lo cambia si otro es
        // claramente mejor (aiSupportSwitchMargin), para que no tiemble
        public static Vector3 BestSpot(Vector3 slot, Vector3 current, Vector3 holder, Vector3 goal, TeamSide team,
                                       IReadOnlyList<Vector3> defenders, RolePositioning.Pitch pitch, GameConfigSO cfg)
        {
            float sign = team.AttackSign();
            Vector3 best = slot;
            float bestScore = Score(slot, slot, holder, goal, defenders, cfg);

            int count = Mathf.Max(1, cfg.aiSupportCandidates);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                var candidate = slot + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * cfg.aiSupportSearchRadius;
                candidate = RolePositioning.Clamp(candidate, sign, pitch, cfg);
                float score = Score(candidate, slot, holder, goal, defenders, cfg);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            // Sigue donde iba si sigue cerca del hueco y no hay nada claramente mejor
            if (Flat(current - slot).magnitude <= cfg.aiSupportSearchRadius * 1.5f &&
                Score(current, slot, holder, goal, defenders, cfg) + cfg.aiSupportSwitchMargin >= bestScore)
                return current;
            return best;
        }

        // Lo bueno que es un punto para recibir: cuanto más alto, mejor
        public static float Score(Vector3 spot, Vector3 slot, Vector3 holder, Vector3 goal, IReadOnlyList<Vector3> defenders, GameConfigSO cfg)
        {
            float score = Mathf.Min(Openness(spot, defenders), cfg.aiOpenRadius * 2f);
            if (!AttackDecision.IsLaneClear(holder, spot, defenders, cfg.aiLaneWidth)) score -= cfg.aiSupportBlockedPenalty;
            if (Flat(goal - spot).magnitude <= cfg.aiShootRange && AttackDecision.IsLaneClear(spot, goal, defenders, cfg.aiLaneWidth))
                score += cfg.aiSupportShotBonus;
            if (Flat(spot - holder).magnitude < cfg.aiSupportMinSpacing) score -= cfg.aiSupportBlockedPenalty;
            score -= Flat(spot - slot).magnitude * cfg.aiSupportShapeWeight;
            return score;
        }

        // Distancia al defensor más cercano (sin defensores, muy abierto)
        public static float Openness(Vector3 spot, IReadOnlyList<Vector3> defenders)
        {
            float best = float.MaxValue;
            foreach (var d in defenders) best = Mathf.Min(best, Flat(d - spot).magnitude);
            return best;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}

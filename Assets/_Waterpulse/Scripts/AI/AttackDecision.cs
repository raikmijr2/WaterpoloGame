using System.Collections.Generic;
using UnityEngine;

namespace Waterpulse.AI
{
    public enum AttackAction { Advance, Pass, Shoot, Reposition }

    public struct AttackChoice
    {
        public AttackAction Action;
        public int PassIndex;      // índice del compañero elegido si Action == Pass
        public float LateralSign;  // hacia dónde moverse en Z si Action == Reposition (+1 / -1)
    }

    // Qué hace un jugador IA con la pelota. Por orden:
    // 1. Tirar si está cerca y con la línea libre.
    // 2. Pasar si le presionan y hay un compañero con línea limpia.
    // 3. Si lleva demasiado tiempo con la pelota: tirar si está en zona de tiro; si no, pasar al mejor compañero.
    // 4. Pasar sin presión si un compañero está claramente más adelantado y con línea limpia.
    // 5. Si está en zona de tiro pero le tapan la línea, moverse en lateral buscando hueco.
    // 6. Si no, avanzar. Así nunca se queda parado con la pelota
    public static class AttackDecision
    {
        public struct Settings
        {
            public float ShootRange;
            public float PressureRadius;
            public float LaneWidth;
            public float MaxHoldTime;
            public float PassAdvanceGain;
        }

        // fieldOpponents tapan la línea de tiro (el portero no cuenta); allOpponents tapan las de pase
        public static AttackChoice Decide(Vector3 self, float attackSign, Vector3 goalCenter,
                                          IReadOnlyList<Vector3> teammates, IReadOnlyList<Vector3> fieldOpponents,
                                          IReadOnlyList<Vector3> allOpponents, float holdTime, Settings s)
        {
            bool inRange = FlatDistance(self, goalCenter) <= s.ShootRange;
            bool shotClear = IsLaneClear(self, goalCenter, fieldOpponents, s.LaneWidth);
            if (inRange && shotClear) return Choice(AttackAction.Shoot);

            int bestClear = BestTeammate(self, attackSign, teammates, allOpponents, s.LaneWidth, true, out float bestClearAdvance);

            if (NearestDistance(self, allOpponents) <= s.PressureRadius && bestClear >= 0)
                return Pass(bestClear);

            if (holdTime >= s.MaxHoldTime)
            {
                if (inRange) return Choice(AttackAction.Shoot);
                int any = bestClear >= 0 ? bestClear : BestTeammate(self, attackSign, teammates, allOpponents, s.LaneWidth, false, out _);
                if (any >= 0) return Pass(any);
            }

            if (bestClear >= 0 && bestClearAdvance >= s.PassAdvanceGain) return Pass(bestClear);

            if (inRange) return new AttackChoice { Action = AttackAction.Reposition, PassIndex = -1, LateralSign = SideAwayFromBlocker(self, goalCenter, fieldOpponents, s.LaneWidth) };

            return Choice(AttackAction.Advance);
        }

        // Compañero más adelantado (opcionalmente solo con línea limpia). Devuelve -1 si no hay
        static int BestTeammate(Vector3 self, float attackSign, IReadOnlyList<Vector3> teammates, IReadOnlyList<Vector3> opponents,
                                float laneWidth, bool requireClearLane, out float bestAdvance)
        {
            int best = -1;
            bestAdvance = float.MinValue;
            for (int i = 0; i < teammates.Count; i++)
            {
                if (requireClearLane && !IsLaneClear(self, teammates[i], opponents, laneWidth)) continue;
                float advance = (teammates[i].x - self.x) * attackSign;
                if (advance > bestAdvance)
                {
                    bestAdvance = advance;
                    best = i;
                }
            }
            return best;
        }

        // Hacia el lado contrario al defensor que tapa la línea de tiro (si no hay ninguno, hacia el centro)
        static float SideAwayFromBlocker(Vector3 self, Vector3 goal, IReadOnlyList<Vector3> blockers, float width)
        {
            float closestT = float.MaxValue;
            float side = goal.z >= self.z ? 1f : -1f;
            Vector2 a = new Vector2(self.x, self.z), b = new Vector2(goal.x, goal.z), ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return side;
            foreach (var blocker in blockers)
            {
                Vector2 p = new Vector2(blocker.x, blocker.z);
                float t = Vector2.Dot(p - a, ab) / len2;
                if (t <= 0f || t >= 1f || Vector2.Distance(p, a + ab * t) >= width || t >= closestT) continue;
                closestT = t;
                side = blocker.z >= self.z ? -1f : 1f;
            }
            return side;
        }

        static AttackChoice Choice(AttackAction action) => new AttackChoice { Action = action, PassIndex = -1 };
        static AttackChoice Pass(int index) => new AttackChoice { Action = AttackAction.Pass, PassIndex = index };

        // Ningún rival a menos de "width" del segmento (sin contar los extremos)
        public static bool IsLaneClear(Vector3 from, Vector3 to, IReadOnlyList<Vector3> blockers, float width)
        {
            Vector2 a = new Vector2(from.x, from.z), b = new Vector2(to.x, to.z);
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return true;
            foreach (var blocker in blockers)
            {
                Vector2 p = new Vector2(blocker.x, blocker.z);
                float t = Vector2.Dot(p - a, ab) / len2;
                if (t <= 0f || t >= 1f) continue;
                if (Vector2.Distance(p, a + ab * t) < width) return false;
            }
            return true;
        }

        static float NearestDistance(Vector3 from, IReadOnlyList<Vector3> points)
        {
            float best = float.MaxValue;
            foreach (var p in points) best = Mathf.Min(best, FlatDistance(from, p));
            return best;
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}

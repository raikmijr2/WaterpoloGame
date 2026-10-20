using System.Collections.Generic;
using UnityEngine;

namespace Waterpulse.AI
{
    public enum AttackAction { Advance, Pass, Shoot }

    public struct AttackChoice
    {
        public AttackAction Action;
        public int PassIndex;   // índice del compañero elegido si Action == Pass
    }

    // Qué hace un jugador IA con la pelota: tirar si está cerca y con línea libre,
    // pasar si le presionan y hay un compañero con línea limpia, y si no, avanzar
    public static class AttackDecision
    {
        public struct Settings
        {
            public float ShootRange;
            public float PressureRadius;
            public float LaneWidth;
        }

        // fieldOpponents tapan la línea de tiro (el portero no cuenta); allOpponents tapan las de pase
        public static AttackChoice Decide(Vector3 self, float attackSign, Vector3 goalCenter,
                                          IReadOnlyList<Vector3> teammates, IReadOnlyList<Vector3> fieldOpponents,
                                          IReadOnlyList<Vector3> allOpponents, Settings s)
        {
            if (FlatDistance(self, goalCenter) <= s.ShootRange && IsLaneClear(self, goalCenter, fieldOpponents, s.LaneWidth))
                return new AttackChoice { Action = AttackAction.Shoot, PassIndex = -1 };

            if (NearestDistance(self, allOpponents) <= s.PressureRadius)
            {
                int best = -1;
                float bestAdvance = float.MinValue;
                for (int i = 0; i < teammates.Count; i++)
                {
                    if (!IsLaneClear(self, teammates[i], allOpponents, s.LaneWidth)) continue;
                    float advance = (teammates[i].x - self.x) * attackSign;   // prefiere al más adelantado
                    if (advance > bestAdvance)
                    {
                        bestAdvance = advance;
                        best = i;
                    }
                }
                if (best >= 0) return new AttackChoice { Action = AttackAction.Pass, PassIndex = best };
            }

            return new AttackChoice { Action = AttackAction.Advance, PassIndex = -1 };
        }

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

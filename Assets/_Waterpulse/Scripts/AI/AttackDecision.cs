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

    // Qué hace un jugador IA con la pelota. Juego en triángulo: pasar hasta que alguien quede desmarcado y tirar.
    // Por orden:
    // 1. Tirar si está desmarcado en zona de tiro con la línea libre, y su equipo ya ha hecho los pases mínimos
    //    (o va solo, sin ningún defensor más cerca de la portería: contraataque).
    // 2. Pasar si le presionan y hay un compañero con línea limpia.
    // 3. Si lleva demasiado tiempo con la pelota: tirar si está en zona de tiro; si no, pasar al mejor compañero.
    // 4. Tirar "porque sí" en zona de tiro, si al recibir le tocó (WantsSpeculativeShot).
    // 5. Pasar (tras un momento con la pelota) a un compañero claramente más desmarcado o mejor colocado.
    // 6. En zona de tiro sin hueco, moverse en lateral buscándolo.
    // 7. Si no, avanzar. Así nunca se queda parado con la pelota
    public static class AttackDecision
    {
        public struct Settings
        {
            public float ShootRange;
            public float PressureRadius;
            public float LaneWidth;
            public float MaxHoldTime;
            public float PassAdvanceGain;
            public float OpenRadius;          // desmarcado = ningún defensor de campo a menos de esto
            public float MinHoldBeforePass;   // sin presión, no pasa antes de llevarla esto (s)
            public float PassOpenGain;        // pasa a un compañero si está al menos esto más libre que él (m)
            public int MinPassesBeforeShot;   // pases del equipo en la posesión antes de buscar el tiro
            public float AdvanceWeight;       // al elegir a quién pasar, cuánto vale cada metro que el compañero está más adelantado
        }

        // Situación de la jugada que no son posiciones
        public struct Context
        {
            public float HoldTime;            // tiempo que lleva con la pelota (s)
            public int TeamPasses;            // pases completados por su equipo en esta posesión
            public bool WantsSpeculativeShot; // al recibir le tocó tirar aunque no esté libre
        }

        // fieldOpponents tapan la línea de tiro y marcan (el portero no cuenta); allOpponents tapan las de pase
        public static AttackChoice Decide(Vector3 self, float attackSign, Vector3 goalCenter,
                                          IReadOnlyList<Vector3> teammates, IReadOnlyList<Vector3> fieldOpponents,
                                          IReadOnlyList<Vector3> allOpponents, Context ctx, Settings s)
        {
            bool inRange = FlatDistance(self, goalCenter) <= s.ShootRange;
            bool shotClear = IsLaneClear(self, goalCenter, fieldOpponents, s.LaneWidth);
            float selfOpen = SupportSpots.Openness(self, fieldOpponents);
            bool open = selfOpen >= s.OpenRadius;
            bool breakaway = !AnyDefenderGoalSide(self, attackSign, fieldOpponents);
            bool enoughPasses = ctx.TeamPasses >= s.MinPassesBeforeShot || breakaway;

            if (inRange && shotClear && open && enoughPasses) return Choice(AttackAction.Shoot);

            int bestClear = BestTeammate(self, attackSign, goalCenter, teammates, fieldOpponents, allOpponents, s, true, out float bestValue);

            if (NearestDistance(self, allOpponents) <= s.PressureRadius && bestClear >= 0)
                return Pass(bestClear);

            if (ctx.HoldTime >= s.MaxHoldTime)
            {
                if (inRange) return Choice(AttackAction.Shoot);
                int any = bestClear >= 0 ? bestClear : BestTeammate(self, attackSign, goalCenter, teammates, fieldOpponents, allOpponents, s, false, out _);
                if (any >= 0) return Pass(any);
            }

            if (inRange && ctx.WantsSpeculativeShot) return Choice(AttackAction.Shoot);

            if (bestClear >= 0 && ctx.HoldTime >= s.MinHoldBeforePass)
            {
                Vector3 mate = teammates[bestClear];
                float mateOpen = SupportSpots.Openness(mate, fieldOpponents);
                float advance = (mate.x - self.x) * attackSign;
                bool mateCanShoot = FlatDistance(mate, goalCenter) <= s.ShootRange && mateOpen >= s.OpenRadius
                                    && IsLaneClear(mate, goalCenter, fieldOpponents, s.LaneWidth);
                bool selfCanShoot = inRange && shotClear && open;
                if (mateCanShoot && !selfCanShoot) return Pass(bestClear);
                if (!open && mateOpen >= Mathf.Min(selfOpen, s.OpenRadius * 2f) + s.PassOpenGain) return Pass(bestClear);
                if (mateOpen >= s.OpenRadius && advance >= s.PassAdvanceGain) return Pass(bestClear);
                // Faltan pases para tirar: mueve la pelota por el triángulo
                if (inRange && !enoughPasses && mateOpen >= s.OpenRadius) return Pass(bestClear);
            }

            if (inRange) return new AttackChoice { Action = AttackAction.Reposition, PassIndex = -1, LateralSign = SideAwayFromBlocker(self, goalCenter, fieldOpponents, s.LaneWidth) };

            return Choice(AttackAction.Advance);
        }

        // Compañero con mejor ocasión: libre, adelantado y con tiro (opcionalmente solo con línea limpia). -1 si no hay
        static int BestTeammate(Vector3 self, float attackSign, Vector3 goal, IReadOnlyList<Vector3> teammates,
                                IReadOnlyList<Vector3> fieldOpponents, IReadOnlyList<Vector3> allOpponents,
                                Settings s, bool requireClearLane, out float bestValue)
        {
            int best = -1;
            bestValue = float.MinValue;
            for (int i = 0; i < teammates.Count; i++)
            {
                if (requireClearLane && !IsLaneClear(self, teammates[i], allOpponents, s.LaneWidth)) continue;
                float open = Mathf.Min(SupportSpots.Openness(teammates[i], fieldOpponents), s.OpenRadius * 2f);
                float advance = (teammates[i].x - self.x) * attackSign;
                float value = open + advance * s.AdvanceWeight;
                if (FlatDistance(teammates[i], goal) <= s.ShootRange && IsLaneClear(teammates[i], goal, fieldOpponents, s.LaneWidth))
                    value += s.OpenRadius;
                if (value > bestValue)
                {
                    bestValue = value;
                    best = i;
                }
            }
            return best;
        }

        // Algún defensor de campo entre él y la portería (más cerca de la portería en el sentido de ataque)
        static bool AnyDefenderGoalSide(Vector3 self, float attackSign, IReadOnlyList<Vector3> defenders)
        {
            foreach (var d in defenders)
                if ((d.x - self.x) * attackSign > 0f) return true;
            return false;
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

using System.Collections.Generic;
using UnityEngine;

namespace Waterpulse.Logic
{
    // Puntos por jugador (GDD sección 8). Los puntos miden quién juega mejor; nunca deciden el resultado.
    // Jugadores = identificadores enteros; equipos = 0 (local) y 1 (visitante)
    public class ScoreTracker
    {
        public struct Rules
        {
            public int Goal;
            public int AbilityGoalBonus;
            public int ComboPassesRequired;
            public int ComboPassesBonus;
            public int Save;
            public int Interception;
            public int Recovery;
            public int Pass;
            public float ComboStep;
            public float ComboMax;
        }

        const int TeamCount = 2;

        readonly Rules rules;
        readonly Dictionary<int, int> points = new Dictionary<int, int>();
        readonly Dictionary<int, int> teamOf = new Dictionary<int, int>();
        readonly List<int> order = new List<int>();
        readonly ComboMultiplier[] combos = new ComboMultiplier[TeamCount];
        readonly int[] passStreak = new int[TeamCount];
        int possessionTeam = -1;

        public ScoreTracker(Rules rules)
        {
            this.rules = rules;
            for (int i = 0; i < TeamCount; i++) combos[i] = new ComboMultiplier(rules.ComboStep, rules.ComboMax);
        }

        public void Register(int player, int team)
        {
            if (teamOf.ContainsKey(player)) return;
            teamOf[player] = team;
            points[player] = 0;
            order.Add(player);
        }

        public int PointsOf(int player) => points.TryGetValue(player, out int p) ? p : 0;
        public float ComboOf(int team) => combos[team].Value;
        public int PassStreakOf(int team) => passStreak[team];

        public int TeamPoints(int team)
        {
            int total = 0;
            foreach (var pair in teamOf)
                if (pair.Value == team) total += points[pair.Key];
            return total;
        }

        // Jugador con más puntos (en empate, el primero registrado). -1 si no hay jugadores
        public int Mvp()
        {
            int best = -1, bestPoints = int.MinValue;
            foreach (int player in order)
            {
                if (points[player] <= bestPoints) continue;
                bestPoints = points[player];
                best = player;
            }
            return best;
        }

        // Al cambiar de equipo la posesión, el combo y la racha de pases empiezan de cero
        public void OnPossessionChanged(int team)
        {
            if (team == possessionTeam) return;
            possessionTeam = team;
            for (int i = 0; i < TeamCount; i++)
            {
                combos[i].Reset();
                passStreak[i] = 0;
            }
        }

        public void OnPassCompleted(int passer)
        {
            if (!teamOf.TryGetValue(passer, out int team)) return;
            passStreak[team]++;
            AwardPositive(passer, team, rules.Pass);
        }

        public void OnInterception(int player) => AwardPositive(player, rules.Interception);
        public void OnRecovery(int player) => AwardPositive(player, rules.Recovery);
        public void OnSave(int goalkeeper) => AwardPositive(goalkeeper, rules.Save);

        // Gol (no en propia puerta): base, extra si fue con habilidad y bonus si hubo racha de pases
        public void OnGoal(int scorer, bool withAbility)
        {
            if (!teamOf.TryGetValue(scorer, out int team)) return;
            int basePoints = rules.Goal;
            if (withAbility) basePoints += rules.AbilityGoalBonus;
            if (passStreak[team] >= rules.ComboPassesRequired) basePoints += rules.ComboPassesBonus;
            AwardPositive(scorer, team, basePoints);
        }

        void AwardPositive(int player, int basePoints)
        {
            if (teamOf.TryGetValue(player, out int team)) AwardPositive(player, team, basePoints);
        }

        // Se aplica el multiplicador actual del equipo y después sube el combo
        void AwardPositive(int player, int team, int basePoints)
        {
            points[player] += Mathf.RoundToInt(basePoints * combos[team].Value);
            combos[team].Advance();
        }
    }
}

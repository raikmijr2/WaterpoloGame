using System.Collections.Generic;

namespace Waterpulse.Logic
{
    // Estadísticas del partido para la pantalla final: pases completados y habilidades (usadas y con éxito)
    public class MatchStats
    {
        class Line
        {
            public int Team;
            public int Passes;
            public int AbilitiesUsed;
            public int AbilitiesSucceeded;
        }

        readonly Dictionary<int, Line> lines = new Dictionary<int, Line>();

        public void Register(int player, int team)
        {
            if (!lines.ContainsKey(player)) lines[player] = new Line { Team = team };
        }

        public void OnPassCompleted(int player) { if (lines.TryGetValue(player, out var l)) l.Passes++; }
        public void OnAbilityUsed(int player) { if (lines.TryGetValue(player, out var l)) l.AbilitiesUsed++; }
        public void OnAbilitySucceeded(int player) { if (lines.TryGetValue(player, out var l)) l.AbilitiesSucceeded++; }

        public int PassesOf(int player) => lines.TryGetValue(player, out var l) ? l.Passes : 0;
        public int AbilitiesUsedOf(int player) => lines.TryGetValue(player, out var l) ? l.AbilitiesUsed : 0;
        public int AbilitiesSucceededOf(int player) => lines.TryGetValue(player, out var l) ? l.AbilitiesSucceeded : 0;

        public int TeamPasses(int team) => Sum(team, l => l.Passes);
        public int TeamAbilitiesUsed(int team) => Sum(team, l => l.AbilitiesUsed);
        public int TeamAbilitiesSucceeded(int team) => Sum(team, l => l.AbilitiesSucceeded);

        int Sum(int team, System.Func<Line, int> value)
        {
            int total = 0;
            foreach (var l in lines.Values)
                if (l.Team == team) total += value(l);
            return total;
        }
    }
}

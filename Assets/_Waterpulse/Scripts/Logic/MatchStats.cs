using System.Collections.Generic;

namespace Waterpulse.Logic
{
    // Estadísticas del partido para el resumen final: goles, pases completados, recuperaciones (intercepciones y robos),
    // paradas y habilidades (usadas y con éxito)
    public class MatchStats
    {
        class Line
        {
            public int Team;
            public int Goals;
            public int Passes;
            public int Recoveries;
            public int Saves;
            public int AbilitiesUsed;
            public int AbilitiesSucceeded;
        }

        readonly Dictionary<int, Line> lines = new Dictionary<int, Line>();

        public void Register(int player, int team)
        {
            if (!lines.ContainsKey(player)) lines[player] = new Line { Team = team };
        }

        public void OnGoal(int player) { if (lines.TryGetValue(player, out var l)) l.Goals++; }
        public void OnPassCompleted(int player) { if (lines.TryGetValue(player, out var l)) l.Passes++; }
        public void OnRecovery(int player) { if (lines.TryGetValue(player, out var l)) l.Recoveries++; }
        public void OnSave(int player) { if (lines.TryGetValue(player, out var l)) l.Saves++; }
        public void OnAbilityUsed(int player) { if (lines.TryGetValue(player, out var l)) l.AbilitiesUsed++; }
        public void OnAbilitySucceeded(int player) { if (lines.TryGetValue(player, out var l)) l.AbilitiesSucceeded++; }

        public int GoalsOf(int player) => lines.TryGetValue(player, out var l) ? l.Goals : 0;
        public int PassesOf(int player) => lines.TryGetValue(player, out var l) ? l.Passes : 0;
        public int RecoveriesOf(int player) => lines.TryGetValue(player, out var l) ? l.Recoveries : 0;
        public int SavesOf(int player) => lines.TryGetValue(player, out var l) ? l.Saves : 0;
        public int AbilitiesUsedOf(int player) => lines.TryGetValue(player, out var l) ? l.AbilitiesUsed : 0;
        public int AbilitiesSucceededOf(int player) => lines.TryGetValue(player, out var l) ? l.AbilitiesSucceeded : 0;

        public int TeamGoals(int team) => Sum(team, l => l.Goals);
        public int TeamPasses(int team) => Sum(team, l => l.Passes);
        public int TeamRecoveries(int team) => Sum(team, l => l.Recoveries);
        public int TeamSaves(int team) => Sum(team, l => l.Saves);
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

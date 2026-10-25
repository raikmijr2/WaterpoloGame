using System;
using System.Collections.Generic;

namespace Waterpulse.Logic
{
    // Una línea del rànquing: la mejor puntuación de un usuario
    public class RankingEntry
    {
        public int UserId { get; }
        public string Name { get; }
        public int Points { get; }
        public int Position { get; internal set; }

        public RankingEntry(int userId, string name, int points)
        {
            UserId = userId;
            Name = name;
            Points = points;
        }
    }

    // Rànquing de mejores puntuaciones, de mayor a menor. Los empates comparten posición (1, 1, 3)
    // y, dentro del empate, van por orden alfabético
    public static class Leaderboard
    {
        public static List<RankingEntry> Rank(IEnumerable<RankingEntry> entries)
        {
            var ranking = new List<RankingEntry>(entries);
            ranking.Sort((a, b) => a.Points != b.Points
                ? b.Points.CompareTo(a.Points)
                : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            for (int i = 0; i < ranking.Count; i++)
                ranking[i].Position = i > 0 && ranking[i].Points == ranking[i - 1].Points ? ranking[i - 1].Position : i + 1;
            return ranking;
        }

        // Posición del usuario en el rànquing; 0 si no está (todavía no tiene récord)
        public static int PositionOf(IReadOnlyList<RankingEntry> ranking, int userId)
        {
            foreach (var entry in ranking)
                if (entry.UserId == userId) return entry.Position;
            return 0;
        }
    }
}

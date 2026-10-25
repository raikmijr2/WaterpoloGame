using System.Collections.Generic;
using Waterpulse.Config;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Usuarios y sus récords, guardados en el navegador. Lo usan el menú (lista, rànquing, borrar)
    // y el partido (guardar el récord del usuario de la sesión y su posición en el rànquing)
    public class UserRecords
    {
        readonly ScoringConfigSO scoring;
        readonly PlayerPrefsRecordStore recordStore = new PlayerPrefsRecordStore();

        public UserRegistry Registry { get; }

        public UserRecords(UsersConfigSO users, ScoringConfigSO scoring)
        {
            this.scoring = scoring;
            Registry = new UserRegistry(new PlayerPrefsTextStore(), users.usersKey, users.maxUsers, users.maxNameLength);
        }

        public RecordStorage RecordOf(int userId) => new RecordStorage(recordStore, string.Format(scoring.userRecordKeyFormat, userId));

        // Mejor puntuación de cada usuario que ya tiene récord
        public List<RankingEntry> Ranking()
        {
            var entries = new List<RankingEntry>();
            foreach (var user in Registry.Users)
            {
                var record = RecordOf(user.Id);
                if (record.HasRecord) entries.Add(new RankingEntry(user.Id, user.Name, record.Best));
            }
            return Leaderboard.Rank(entries);
        }

        // Borra el usuario y su récord
        public void Remove(int userId)
        {
            RecordOf(userId).Clear();
            Registry.Remove(userId);
        }
    }
}

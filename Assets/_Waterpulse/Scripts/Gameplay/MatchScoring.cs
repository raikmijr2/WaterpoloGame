using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Escucha los eventos del partido y alimenta la puntuación (ScoreTracker), las estadísticas (MatchStats)
    // y el récord del usuario de la sesión (UserRecords). La pantalla final lee de aquí
    public class MatchScoring : MonoBehaviour
    {
        [SerializeField] ScoringConfigSO config;
        [SerializeField] UsersConfigSO users;
        [SerializeField] BallController ball;

        // Torpede Blau en curso: es un éxito si el jugador consigue la pelota o, si ya la tenía, su equipo no la pierde
        struct BoostWatch
        {
            public PlayerController Player;
            public float Deadline;
            public bool HadBall;
        }

        readonly List<PlayerController> players = new List<PlayerController>();
        readonly Dictionary<GameObject, int> ids = new Dictionary<GameObject, int>();
        readonly List<BoostWatch> boosts = new List<BoostWatch>();
        ScoreTracker tracker;
        MatchStats stats;

        public IReadOnlyList<PlayerController> Players => players;

        // Récord (se calcula al acabar el tiempo). Sin usuario (convidat, o el partido abierto directamente en el editor)
        // no se guarda nada
        public bool HasUser { get; private set; }
        public string UserName { get; private set; }
        public bool IsNewRecord { get; private set; }
        public int Record { get; private set; }
        public int RankingPosition { get; private set; }

        public int PointsOf(PlayerController p) => tracker.PointsOf(IdOf(p));
        public int GoalsOf(PlayerController p) => stats.GoalsOf(IdOf(p));
        public int PassesOf(PlayerController p) => stats.PassesOf(IdOf(p));
        public int RecoveriesOf(PlayerController p) => stats.RecoveriesOf(IdOf(p));
        public int SavesOf(PlayerController p) => stats.SavesOf(IdOf(p));
        public int TeamPoints(TeamSide team) => tracker.TeamPoints((int)team);
        public int TeamGoals(TeamSide team) => stats.TeamGoals((int)team);
        public int TeamPasses(TeamSide team) => stats.TeamPasses((int)team);
        public int TeamRecoveries(TeamSide team) => stats.TeamRecoveries((int)team);
        public int TeamSaves(TeamSide team) => stats.TeamSaves((int)team);
        public int TeamAbilitiesUsed(TeamSide team) => stats.TeamAbilitiesUsed((int)team);
        public int TeamAbilitiesSucceeded(TeamSide team) => stats.TeamAbilitiesSucceeded((int)team);
        public PlayerController Mvp => tracker.Mvp() is int id && id >= 0 ? players[id] : null;
        // Lo que vale ahora el próximo gol del equipo (×1 a ×3)
        public float MultiplierOf(TeamSide team) => tracker.MultiplierOf((int)team);

        // Puntos ganados o multiplicador cambiado (jugador null en MultiplierLost). Lo escuchan los avisos del HUD
        public event System.Action<PlayerController, ScoreEvent> Scored;
        // Cada gol con los puntos de partido que vale (equipo que marca, puntos). Lo escucha MatchFlow para el marcador
        public event System.Action<TeamSide, int> GoalScored;

        void Awake()
        {
            tracker = new ScoreTracker(new ScoreTracker.Rules
            {
                Goal = config.goalPoints,
                AbilityGoalBonus = config.abilityGoalBonus,
                Save = config.savePoints,
                Interception = config.interceptionPoints,
                Recovery = config.recoveryPoints,
                Pass = config.passPoints,
                PassMultiplier = config.passMultiplier,
                RecoveryMultiplier = config.recoveryMultiplier,
                MultiplierMax = config.multiplierMax,
                ShortStreak = config.shortStreak,
                ShortStreakBonus = config.shortStreakBonus,
                LongStreak = config.longStreak,
                LongStreakBonus = config.longStreakBonus,
                CounterAttackWindow = config.counterAttackWindow,
                CounterAttackBonus = config.counterAttackBonus,
            });
            tracker.Scored += e => Scored?.Invoke(e.Player >= 0 ? players[e.Player] : null, e);
            stats = new MatchStats();

            // Orden fijo para la pantalla final: equipo y después rol
            players.AddRange(FindObjectsByType<PlayerController>(FindObjectsSortMode.None));
            players.Sort((a, b) => a.Team != b.Team ? a.Team.CompareTo(b.Team) : a.Role.CompareTo(b.Role));
            for (int i = 0; i < players.Count; i++)
            {
                ids[players[i].gameObject] = i;
                tracker.Register(i, (int)players[i].Team);
                stats.Register(i, (int)players[i].Team);
            }
        }

        void OnEnable()
        {
            GameEvents.PossessionChanged += OnPossessionChanged;
            GameEvents.PassCompleted += OnPassCompleted;
            GameEvents.Interception += OnInterception;
            GameEvents.BallRecovered += OnBallRecovered;
            GameEvents.Save += OnSave;
            GameEvents.Goal += OnGoal;
            GameEvents.AbilityUsed += OnAbilityUsed;
            GameEvents.MatchStateChanged += OnMatchStateChanged;
        }

        void OnDisable()
        {
            GameEvents.PossessionChanged -= OnPossessionChanged;
            GameEvents.PassCompleted -= OnPassCompleted;
            GameEvents.Interception -= OnInterception;
            GameEvents.BallRecovered -= OnBallRecovered;
            GameEvents.Save -= OnSave;
            GameEvents.Goal -= OnGoal;
            GameEvents.AbilityUsed -= OnAbilityUsed;
            GameEvents.MatchStateChanged -= OnMatchStateChanged;
        }

        void Update()
        {
            for (int i = boosts.Count - 1; i >= 0; i--)
            {
                var watch = boosts[i];
                bool gained = !watch.HadBall && watch.Player.HasBall;
                bool expired = Time.time >= watch.Deadline;
                if (!gained && !expired) continue;
                if (gained || (watch.HadBall && ball.HasPossession(watch.Player.Team))) stats.OnAbilitySucceeded(IdOf(watch.Player));
                boosts.RemoveAt(i);
            }
        }

        void OnPossessionChanged(TeamSide team) => tracker.OnPossessionChanged((int)team);

        void OnPassCompleted(GameObject passer, GameObject receiver)
        {
            if (!TryId(passer, out int id)) return;
            tracker.OnPassCompleted(id);
            stats.OnPassCompleted(id);
        }

        // Intercepciones y robos con J cuentan como recuperaciones en el resumen
        void OnInterception(GameObject interceptor, GameObject passer)
        {
            if (!TryId(interceptor, out int id)) return;
            tracker.OnInterception(id, Time.time);
            stats.OnRecovery(id);
        }

        void OnBallRecovered(GameObject player)
        {
            if (!TryId(player, out int id)) return;
            tracker.OnRecovery(id, Time.time);
            stats.OnRecovery(id);
        }

        void OnSave(GameObject goalkeeper)
        {
            if (!TryId(goalkeeper, out int id)) return;
            tracker.OnSave(id, Time.time);
            stats.OnSave(id);
        }

        // En propia puerta no hay puntos para ningún jugador, y al equipo le vale el gol base (sin multiplicador).
        // Con el Xut del Tràngol: extra y habilidad con éxito
        void OnGoal(TeamSide scoringTeam, GameObject scorer)
        {
            if (!TryId(scorer, out int id) || players[id].Team != scoringTeam)
            {
                GoalScored?.Invoke(scoringTeam, config.goalPoints);
                return;
            }
            bool withAbility = ball.IsSpecialShot && ball.LastToucher == players[id];
            int matchPoints = tracker.OnGoal(id, withAbility, Time.time);
            stats.OnGoal(id);
            if (withAbility) stats.OnAbilitySucceeded(id);
            GoalScored?.Invoke(scoringTeam, matchPoints);
        }

        void OnAbilityUsed(GameObject player, string abilityName)
        {
            if (!TryId(player, out int id)) return;
            stats.OnAbilityUsed(id);
            // Solo el Torpede se vigila para saber si sale bien (el Xut sale bien si es gol)
            var ability = players[id].Boost;
            if (ability == null || ability.displayName != abilityName) return;
            boosts.Add(new BoostWatch
            {
                Player = players[id],
                Deadline = Time.time + ability.duration + config.torpedeSuccessGrace,
                HadBall = players[id].HasBall,
            });
        }

        // El récord del usuario de la sesión se guarda al final del tiempo, antes de que se abra la pantalla de resultados
        void OnMatchStateChanged(MatchState state)
        {
            if (state != MatchState.FullTime) return;
            var records = new UserRecords(users, config);
            var user = PlayerSession.HasChosen && !PlayerSession.IsGuest ? records.Registry.Get(PlayerSession.UserId) : null;
            HasUser = user != null;
            if (!HasUser) return;

            var record = records.RecordOf(user.Id);
            UserName = user.Name;
            IsNewRecord = record.Submit(TeamPoints(TeamSide.Home));
            Record = record.Best;
            RankingPosition = Leaderboard.PositionOf(records.Ranking(), user.Id);
        }

        int IdOf(PlayerController p) => p != null && ids.TryGetValue(p.gameObject, out int id) ? id : -1;

        bool TryId(GameObject go, out int id)
        {
            id = -1;
            return go != null && ids.TryGetValue(go, out id);
        }
    }
}

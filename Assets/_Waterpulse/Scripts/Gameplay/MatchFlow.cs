using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Conecta la MatchStateMachine con la escena: recoloca en cada saque, congela la pelota tras gol
    // y al final, y avisa a los demás sistemas con GameEvents.MatchStateChanged
    public class MatchFlow : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] BallController ball;
        [SerializeField] PoolBounds pool;

        MatchStateMachine machine;
        PlayerController[] players;

        public MatchState State => machine.State;
        public float TimeRemaining => machine.Timer.Remaining;
        public bool IsGoldenGoal => machine.IsGoldenGoal;
        public int HomeGoals => machine.HomeGoals;
        public int AwayGoals => machine.AwayGoals;
        public bool IsDraw => machine.IsDraw;
        public TeamSide Winner => machine.Winner;

        public event Action GoldenGoalStarted;

        void Awake()
        {
            machine = new MatchStateMachine(new MatchStateMachine.Settings
            {
                MatchDuration = config.matchDuration,
                KickoffDelay = config.kickoffDelay,
                GoalDelay = config.goalRestartDelay,
                FullTimeDelay = config.fullTimeDelay,
            });
            machine.StateChanged += OnStateChanged;
            machine.GoldenGoalStarted += () =>
            {
                GoldenGoalStarted?.Invoke();
                GameEvents.RaiseGoldenGoalStarted();
            };
        }

        void OnEnable() => GameEvents.Goal += OnGoal;
        void OnDisable() => GameEvents.Goal -= OnGoal;

        void Start()
        {
            players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            machine.Begin(TeamSide.Home);   // el primer saque es del local
        }

        void Update() => machine.Tick(Time.deltaTime);

        // Vuelve a empezar el partido desde cero
        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        void OnGoal(TeamSide scoringTeam, GameObject scorer)
        {
            if (machine.RegisterGoal(scoringTeam)) ball.Freeze();
        }

        void OnStateChanged(MatchState state)
        {
            if (state == MatchState.Kickoff) PlaceForKickoff(machine.KickoffTeam);
            else if (state == MatchState.FullTime) ball.Freeze();
            GameEvents.RaiseMatchStateChanged(state);
        }

        // Todos a su posición de saque; la pelota, al jugador del equipo que saca más cercano al centro
        void PlaceForKickoff(TeamSide kickoffTeam)
        {
            if (players == null) players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            PlayerController receiver = null;
            float best = float.MaxValue;
            foreach (var p in players)
            {
                Vector3 position = KickoffLayout.Position(p.Role, p.Team, pool.Center, pool.HalfLength, config);
                p.Teleport(position, KickoffLayout.Facing(p.Team));
                if (p.Team != kickoffTeam || p.IsGoalkeeper) continue;
                float d = Vector3.Distance(position, pool.Center);
                if (d < best)
                {
                    best = d;
                    receiver = p;
                }
            }
            ball.PlaceForRestart(receiver);
        }
    }
}

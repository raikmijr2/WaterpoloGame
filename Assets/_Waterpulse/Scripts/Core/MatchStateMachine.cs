using System;

namespace Waterpulse.Core
{
    // Flujo del partido (lógica pura): Kickoff → Playing → GoalScored → Kickoff … → FullTime → Results.
    // El reloj solo corre en Playing. Si al acabar el tiempo hay empate, se juega a gol de oro
    public class MatchStateMachine
    {
        public struct Settings
        {
            public float MatchDuration;
            public float KickoffDelay;
            public float GoalDelay;
            public float FullTimeDelay;
        }

        readonly Settings settings;
        float stateTimer;

        public MatchState State { get; private set; }
        public MatchTimer Timer { get; }
        public bool IsGoldenGoal { get; private set; }
        public TeamSide KickoffTeam { get; private set; }
        public int HomeGoals { get; private set; }
        public int AwayGoals { get; private set; }
        public bool IsDraw => HomeGoals == AwayGoals;
        public TeamSide Winner => HomeGoals >= AwayGoals ? TeamSide.Home : TeamSide.Away;

        public event Action<MatchState> StateChanged;
        public event Action GoldenGoalStarted;

        public MatchStateMachine(Settings settings)
        {
            this.settings = settings;
            Timer = new MatchTimer(settings.MatchDuration);
        }

        public int GoalsOf(TeamSide team) => team == TeamSide.Home ? HomeGoals : AwayGoals;

        public void Begin(TeamSide firstKickoff) => EnterKickoff(firstKickoff);

        // Solo cuenta en Playing. Devuelve true si el gol se ha registrado
        public bool RegisterGoal(TeamSide scoringTeam)
        {
            if (State != MatchState.Playing) return false;
            if (scoringTeam == TeamSide.Home) HomeGoals++;
            else AwayGoals++;
            Timer.Pause();
            KickoffTeam = scoringTeam.Opponent();
            Enter(MatchState.GoalScored, settings.GoalDelay);
            return true;
        }

        public void Tick(float dt)
        {
            switch (State)
            {
                case MatchState.Kickoff:
                    if (CountDown(dt))
                    {
                        Enter(MatchState.Playing, 0f);
                        if (!IsGoldenGoal) Timer.Start();
                    }
                    break;

                case MatchState.Playing:
                    if (!IsGoldenGoal && Timer.Tick(dt)) OnTimeUp();
                    break;

                case MatchState.GoalScored:
                    if (!CountDown(dt)) break;
                    // Un gol en el gol de oro, o justo al agotarse el tiempo, cierra el partido
                    if (IsGoldenGoal || Timer.IsExpired) OnTimeUp();
                    else EnterKickoff(KickoffTeam);
                    break;

                case MatchState.FullTime:
                    if (CountDown(dt)) Enter(MatchState.Results, 0f);
                    break;
            }
        }

        void OnTimeUp()
        {
            if (IsDraw)
            {
                // Empate: se sigue jugando hasta el próximo gol
                if (!IsGoldenGoal)
                {
                    IsGoldenGoal = true;
                    GoldenGoalStarted?.Invoke();
                }
                if (State != MatchState.Playing) EnterKickoff(KickoffTeam);
                return;
            }
            Enter(MatchState.FullTime, settings.FullTimeDelay);
        }

        void EnterKickoff(TeamSide team)
        {
            KickoffTeam = team;
            Enter(MatchState.Kickoff, settings.KickoffDelay);
        }

        void Enter(MatchState state, float duration)
        {
            State = state;
            stateTimer = duration;
            StateChanged?.Invoke(state);
        }

        bool CountDown(float dt)
        {
            stateTimer -= dt;
            return stateTimer <= 0f;
        }
    }
}

namespace Waterpulse.Core
{
    // Estados del partido: Kickoff → Playing → GoalScored → Kickoff … → FullTime → Results
    public enum MatchState { Kickoff, Playing, GoalScored, FullTime, Results }
}

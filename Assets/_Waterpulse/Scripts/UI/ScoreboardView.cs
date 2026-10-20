using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Marcador básico con el tiempo restante y los avisos del partido. Textos visibles en catalán
    public class ScoreboardView : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] MatchFlow match;
        [SerializeField] Text scoreLabel;
        [SerializeField] Text messageLabel;
        [SerializeField] string kickoffMessage = "Preparats…";
        [SerializeField] string goalMessage = "GOL!";
        [SerializeField] string fullTimeMessage = "Final del partit";
        [SerializeField] string goldenGoalLabel = "GOL D'OR";

        void OnEnable() => GameEvents.MatchStateChanged += OnStateChanged;
        void OnDisable() => GameEvents.MatchStateChanged -= OnStateChanged;

        void Update()
        {
            string time = match.IsGoldenGoal ? goldenGoalLabel : FormatTime(match.TimeRemaining);
            scoreLabel.text = $"{config.homeTeamName}  {match.HomeGoals} - {match.AwayGoals}  {config.awayTeamName}     {time}";
        }

        void OnStateChanged(MatchState state)
        {
            switch (state)
            {
                case MatchState.Kickoff: messageLabel.text = kickoffMessage; break;
                case MatchState.GoalScored: messageLabel.text = goalMessage; break;
                case MatchState.FullTime: messageLabel.text = fullTimeMessage; break;
                default: messageLabel.text = string.Empty; break;
            }
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}

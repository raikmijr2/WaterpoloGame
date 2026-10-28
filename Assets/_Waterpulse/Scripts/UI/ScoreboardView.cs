using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Marcador de retransmisión (nombre y goles de cada equipo, tiempo o "GOL D'OR" en el centro y un punto bajo el equipo
    // con la posesión) y bàner con los avisos del partido. Textos visibles en catalán
    public class ScoreboardView : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] MatchFlow match;

        [Header("Marcador")]
        [SerializeField] Text homeNameLabel;
        [SerializeField] Text homeScoreLabel;
        [SerializeField] Text clockLabel;
        [SerializeField] Text awayScoreLabel;
        [SerializeField] Text awayNameLabel;
        [Tooltip("Punto bajo el equipo que tiene la posesión")]
        [SerializeField] GameObject homePossession;
        [SerializeField] GameObject awayPossession;

        [Header("Avisos")]
        [Tooltip("Bàner que se ve solo mientras hay aviso")]
        [SerializeField] GameObject messagePanel;
        [SerializeField] Text messageLabel;
        [SerializeField] string kickoffMessage = "Preparats…";
        [SerializeField] string goalMessage = "GOL!";
        [SerializeField] string fullTimeMessage = "Final del partit";
        [SerializeField] string goldenGoalLabel = "GOL D'OR";

        void Awake()
        {
            homeNameLabel.text = config.homeTeamName;
            awayNameLabel.text = config.awayTeamName;
            ShowPossession(null);
            ShowMessage(string.Empty);
        }

        void OnEnable()
        {
            GameEvents.MatchStateChanged += OnStateChanged;
            GameEvents.PossessionChanged += OnPossessionChanged;
        }

        void OnDisable()
        {
            GameEvents.MatchStateChanged -= OnStateChanged;
            GameEvents.PossessionChanged -= OnPossessionChanged;
        }

        void Update()
        {
            homeScoreLabel.text = match.HomeGoals.ToString();
            awayScoreLabel.text = match.AwayGoals.ToString();
            clockLabel.text = match.IsGoldenGoal ? goldenGoalLabel : FormatTime(match.TimeRemaining);
        }

        void OnStateChanged(MatchState state)
        {
            switch (state)
            {
                case MatchState.Kickoff: ShowMessage(kickoffMessage); break;
                case MatchState.GoalScored: ShowMessage(goalMessage); break;
                case MatchState.FullTime: ShowMessage(fullTimeMessage); break;
                default: ShowMessage(string.Empty); break;
            }
        }

        void OnPossessionChanged(TeamSide team) => ShowPossession(team);

        void ShowPossession(TeamSide? team)
        {
            homePossession.SetActive(team == TeamSide.Home);
            awayPossession.SetActive(team == TeamSide.Away);
        }

        void ShowMessage(string text)
        {
            messageLabel.text = text;
            messagePanel.SetActive(!string.IsNullOrEmpty(text));
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }
    }
}

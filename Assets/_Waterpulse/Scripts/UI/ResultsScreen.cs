using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Pantalla final (GDD sección 8): resultado, puntos por jugador con el MVP destacado, totales,
    // pases completados, habilidades con éxito, récord personal, "Tornar a jugar" (botón o J/Enter)
    // y "Menú principal" (botón o Esc/Start). Textos visibles en catalán
    public class ResultsScreen : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] MenuConfigSO menuConfig;
        [SerializeField] MatchFlow match;
        [SerializeField] MatchScoring scoring;
        [SerializeField] GameObject panel;
        [SerializeField] Text resultLabel;
        [SerializeField] Text winnerLabel;
        [SerializeField] Text homeColumn;
        [SerializeField] Text awayColumn;
        [SerializeField] Text recordLabel;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;

        [Header("Textos")]
        [SerializeField] string winsFormat = "Guanya {0}!";
        [SerializeField] string drawText = "Empat";
        [SerializeField] string teamHeaderFormat = "{0} — {1} punts";
        [SerializeField] string playerLineFormat = "{0}: {1}";
        [SerializeField] string mvpTag = "  ★ MVP";
        [SerializeField] string passesFormat = "Passades completades: {0}";
        [SerializeField] string abilitiesFormat = "Habilitats amb èxit: {0} de {1}";
        [SerializeField] string newRecordFormat = "Nou rècord! {0} punts";
        [SerializeField] string recordFormat = "Rècord personal: {0} punts";
        [Tooltip("Nombres de rol en el orden de PlayerRole: Attacker, Playmaker, Defender, Goalkeeper")]
        [SerializeField] string[] roleNames = { "Atacant", "Organitzador", "Defensor", "Porter" };

        InputAction restart;
        InputAction toMenu;
        readonly StringBuilder builder = new StringBuilder();

        void Awake()
        {
            restart = new InputAction("Restart", InputActionType.Button);
            restart.AddBinding("<Keyboard>/j");
            restart.AddBinding("<Keyboard>/enter");
            restart.AddBinding("<Gamepad>/buttonSouth");
            toMenu = new InputAction("Menu", InputActionType.Button);
            toMenu.AddBinding("<Keyboard>/escape");
            toMenu.AddBinding("<Gamepad>/start");
            restartButton.onClick.AddListener(match.Restart);
            menuButton.onClick.AddListener(GoToMenu);
            panel.SetActive(false);
        }

        void OnEnable() => GameEvents.MatchStateChanged += OnStateChanged;

        void OnDisable()
        {
            GameEvents.MatchStateChanged -= OnStateChanged;
            restart.Disable();
            toMenu.Disable();
        }

        void OnDestroy()
        {
            restart.Dispose();
            toMenu.Dispose();
        }

        void Update()
        {
            if (!panel.activeSelf) return;
            if (restart.WasPressedThisFrame()) match.Restart();
            else if (toMenu.WasPressedThisFrame()) GoToMenu();
        }

        void GoToMenu() => SceneManager.LoadScene(menuConfig.menuScene);

        void OnStateChanged(MatchState state)
        {
            if (state != MatchState.Results) return;

            resultLabel.text = $"{config.homeTeamName}  {match.HomeGoals} - {match.AwayGoals}  {config.awayTeamName}";
            string winner = match.Winner == TeamSide.Home ? config.homeTeamName : config.awayTeamName;
            winnerLabel.text = match.IsDraw ? drawText : string.Format(winsFormat, winner);

            homeColumn.text = TeamColumn(TeamSide.Home, config.homeTeamName);
            awayColumn.text = TeamColumn(TeamSide.Away, config.awayTeamName);

            int homePoints = scoring.TeamPoints(TeamSide.Home);
            recordLabel.text = scoring.IsNewRecord
                ? string.Format(newRecordFormat, homePoints)
                : string.Format(recordFormat, scoring.Record);

            panel.SetActive(true);
            restart.Enable();
            toMenu.Enable();
        }

        string TeamColumn(TeamSide team, string teamName)
        {
            var mvp = scoring.Mvp;
            builder.Clear();
            builder.AppendLine(string.Format(teamHeaderFormat, teamName, scoring.TeamPoints(team)));
            builder.AppendLine();
            foreach (var p in scoring.Players)
            {
                if (p.Team != team) continue;
                builder.Append(string.Format(playerLineFormat, RoleName(p.Role), scoring.PointsOf(p)));
                if (p == mvp) builder.Append(mvpTag);
                builder.AppendLine();
            }
            builder.AppendLine();
            builder.AppendLine(string.Format(passesFormat, scoring.TeamPasses(team)));
            builder.Append(string.Format(abilitiesFormat, scoring.TeamAbilitiesSucceeded(team), scoring.TeamAbilitiesUsed(team)));
            return builder.ToString();
        }

        string RoleName(PlayerRole role)
        {
            int index = (int)role;
            return index >= 0 && index < roleNames.Length ? roleNames[index] : role.ToString();
        }
    }
}

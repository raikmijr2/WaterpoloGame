using System;
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
    // Resumen del partido (GDD sección 8): marcador y ganador; por equipo, una tabla con goles, pases, recuperaciones,
    // paradas y puntos de cada jugador (el MVP resaltado) y el total; habilidades con éxito; récord del usuario y su
    // posición en el rànquing; "Tornar a jugar" (botón o J/Enter) y "Menú principal" (botón o Esc/Start).
    // Textos visibles en catalán
    public class ResultsScreen : MonoBehaviour
    {
        // Columnas de la tabla de un equipo: una línea por jugador (en el orden de MatchScoring) y el total
        [Serializable]
        public class TeamTable
        {
            public Text title;
            public Text players;
            public Text goals;
            public Text passes;
            public Text recoveries;
            public Text saves;
            public Text points;
            public Text abilities;
        }

        [SerializeField] GameConfigSO config;
        [SerializeField] MenuConfigSO menuConfig;
        [SerializeField] MatchFlow match;
        [SerializeField] MatchScoring scoring;
        [SerializeField] GameObject panel;
        [SerializeField] Text scoreLabel;
        [SerializeField] Text winnerLabel;
        [SerializeField] TeamTable homeTable = new TeamTable();
        [SerializeField] TeamTable awayTable = new TeamTable();
        [SerializeField] Text recordLabel;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;
        [SerializeField] Color mvpColor = new Color(1f, 0.85f, 0.2f);

        [Header("Textos")]
        [SerializeField] string winsFormat = "Guanya {0}!";
        [SerializeField] string drawText = "Empat";
        [SerializeField] string mvpTag = " · MVP";
        [SerializeField] string totalText = "Total";
        [SerializeField] string abilitiesFormat = "Habilitats amb èxit: {0} de {1}";
        [SerializeField] string newRecordFormat = "{0} · {1} punts · Nou rècord!";
        [SerializeField] string recordFormat = "{0} · {1} punts · El teu rècord: {2}";
        [SerializeField] string positionFormat = " · Posició al rànquing: {0}";
        [SerializeField] string guestFormat = "Convidat · {0} punts · no es guarda el rècord";
        [Tooltip("Nombres de rol en el orden de PlayerRole: Attacker, Playmaker, Defender, Goalkeeper")]
        [SerializeField] string[] roleNames = { "Atacant", "Organitzador", "Defensor", "Porter" };

        InputAction restart;
        InputAction toMenu;
        readonly StringBuilder builder = new StringBuilder();
        PlayerController mvp;
        string mvpHex;

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

            scoreLabel.text = $"{config.homeTeamName}  {match.HomeGoals} - {match.AwayGoals}  {config.awayTeamName}";
            string winner = match.Winner == TeamSide.Home ? config.homeTeamName : config.awayTeamName;
            winnerLabel.text = match.IsDraw ? drawText : string.Format(winsFormat, winner);

            mvp = scoring.Mvp;
            mvpHex = ColorUtility.ToHtmlStringRGB(mvpColor);
            FillTable(homeTable, TeamSide.Home, config.homeTeamName);
            FillTable(awayTable, TeamSide.Away, config.awayTeamName);
            recordLabel.text = RecordText();

            panel.SetActive(true);
            restart.Enable();
            toMenu.Enable();
        }

        void FillTable(TeamTable table, TeamSide team, string teamName)
        {
            table.title.text = teamName;
            table.players.text = Column(team, PlayerName, totalText);
            table.goals.text = Column(team, p => scoring.GoalsOf(p).ToString(), scoring.TeamGoals(team).ToString());
            table.passes.text = Column(team, p => scoring.PassesOf(p).ToString(), scoring.TeamPasses(team).ToString());
            table.recoveries.text = Column(team, p => scoring.RecoveriesOf(p).ToString(), scoring.TeamRecoveries(team).ToString());
            table.saves.text = Column(team, p => scoring.SavesOf(p).ToString(), scoring.TeamSaves(team).ToString());
            table.points.text = Column(team, p => scoring.PointsOf(p).ToString(), scoring.TeamPoints(team).ToString());
            table.abilities.text = string.Format(abilitiesFormat, scoring.TeamAbilitiesSucceeded(team), scoring.TeamAbilitiesUsed(team));
        }

        // Una columna de la tabla: una línea por jugador del equipo (la del MVP, resaltada) y el total en negrita.
        // Todas las columnas tienen las mismas líneas, así que quedan alineadas
        string Column(TeamSide team, Func<PlayerController, string> cell, string total)
        {
            builder.Clear();
            foreach (var p in scoring.Players)
            {
                if (p.Team != team) continue;
                if (p == mvp) builder.Append("<color=#").Append(mvpHex).Append('>').Append(cell(p)).Append("</color>");
                else builder.Append(cell(p));
                builder.Append('\n');
            }
            builder.Append("<b>").Append(total).Append("</b>");
            return builder.ToString();
        }

        string PlayerName(PlayerController p) => RoleName(p.Role) + (p == mvp ? mvpTag : string.Empty);

        // Récord del usuario de la sesión: los puntos de su equipo en este partido
        string RecordText()
        {
            int points = scoring.TeamPoints(TeamSide.Home);
            if (!scoring.HasUser) return string.Format(guestFormat, points);
            string text = scoring.IsNewRecord
                ? string.Format(newRecordFormat, scoring.UserName, points)
                : string.Format(recordFormat, scoring.UserName, points, scoring.Record);
            if (scoring.RankingPosition > 0) text += string.Format(positionFormat, scoring.RankingPosition);
            return text;
        }

        string RoleName(PlayerRole role)
        {
            int index = (int)role;
            return index >= 0 && index < roleNames.Length ? roleNames[index] : role.ToString();
        }
    }
}

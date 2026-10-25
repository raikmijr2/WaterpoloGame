using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Rànquing: la mejor puntuación de cada usuario, en tres columnas (posición, nombre y puntos).
    // El usuario de la sesión sale resaltado. Textos visibles en catalán
    public class RankingView : MonoBehaviour
    {
        [SerializeField] UsersConfigSO config;
        [SerializeField] ScoringConfigSO scoringConfig;
        [SerializeField] Text positionsColumn;
        [SerializeField] Text namesColumn;
        [SerializeField] Text pointsColumn;
        [SerializeField] Text emptyLabel;
        [SerializeField] Color currentUserColor = new Color(1f, 0.85f, 0.2f);

        [Header("Textos")]
        [SerializeField] string positionFormat = "{0}.";
        [SerializeField] string pointsFormat = "{0} punts";

        readonly StringBuilder positions = new StringBuilder();
        readonly StringBuilder names = new StringBuilder();
        readonly StringBuilder points = new StringBuilder();

        public void Refresh()
        {
            var ranking = new UserRecords(config, scoringConfig).Ranking();
            string highlight = ColorUtility.ToHtmlStringRGB(currentUserColor);
            positions.Clear();
            names.Clear();
            points.Clear();

            int lines = Mathf.Min(ranking.Count, config.rankingSize);
            for (int i = 0; i < lines; i++)
            {
                var entry = ranking[i];
                bool current = PlayerSession.HasChosen && !PlayerSession.IsGuest && entry.UserId == PlayerSession.UserId;
                AppendLine(positions, string.Format(positionFormat, entry.Position), current, highlight);
                AppendLine(names, entry.Name, current, highlight);
                AppendLine(points, string.Format(pointsFormat, entry.Points), current, highlight);
            }

            positionsColumn.text = positions.ToString();
            namesColumn.text = names.ToString();
            pointsColumn.text = points.ToString();
            emptyLabel.gameObject.SetActive(lines == 0);
        }

        // Los nombres no pueden llevar "<" ni ">" (UserRegistry), así que no rompen el formato de color
        static void AppendLine(StringBuilder column, string text, bool highlighted, string color)
        {
            if (column.Length > 0) column.Append('\n');
            if (highlighted) column.Append("<color=#").Append(color).Append('>').Append(text).Append("</color>");
            else column.Append(text);
        }
    }
}

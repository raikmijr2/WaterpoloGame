using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Marcador básico. Los nombres de equipo son textos visibles, en catalán
    public class ScoreboardView : MonoBehaviour
    {
        [SerializeField] MatchScore score;
        [SerializeField] Text label;
        [SerializeField] string homeName = "LOCAL";
        [SerializeField] string awayName = "VISITANT";

        void OnEnable()
        {
            score.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => score.Changed -= Refresh;

        void Refresh() => label.text = $"{homeName}  {score.HomeGoals} - {score.AwayGoals}  {awayName}";
    }
}

using System;
using UnityEngine;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Goles del partido. Escucha el evento Goal; el marcador de la UI lee de aquí
    public class MatchScore : MonoBehaviour
    {
        public int HomeGoals { get; private set; }
        public int AwayGoals { get; private set; }

        public event Action Changed;

        public int GoalsOf(TeamSide team) => team == TeamSide.Home ? HomeGoals : AwayGoals;

        void OnEnable() => GameEvents.Goal += OnGoal;
        void OnDisable() => GameEvents.Goal -= OnGoal;

        void OnGoal(TeamSide scoringTeam, GameObject scorer)
        {
            if (scoringTeam == TeamSide.Home) HomeGoals++;
            else AwayGoals++;
            Changed?.Invoke();
        }
    }
}

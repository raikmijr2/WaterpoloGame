using System;
using UnityEngine;

namespace Waterpulse.Core
{
    // Bus de eventos del partido. Los sistemas emiten y escuchan aquí sin referenciarse entre sí
    public static class GameEvents
    {
        public static event Action<TeamSide, GameObject> Goal;               // equipo que marca, autor (puede ser null)
        public static event Action<GameObject, GameObject> PassCompleted;    // pasador, receptor
        public static event Action<TeamSide> PossessionChanged;              // equipo que pasa a tener la pelota

        public static void RaiseGoal(TeamSide scoringTeam, GameObject scorer) => Goal?.Invoke(scoringTeam, scorer);
        public static void RaisePassCompleted(GameObject from, GameObject to) => PassCompleted?.Invoke(from, to);
        public static void RaisePossessionChanged(TeamSide team) => PossessionChanged?.Invoke(team);

        // Limpia suscripciones al entrar en Play aunque el "domain reload" esté desactivado
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSubscriptions()
        {
            Goal = null;
            PassCompleted = null;
            PossessionChanged = null;
        }
    }
}

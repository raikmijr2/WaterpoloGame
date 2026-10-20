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
        public static event Action<GameObject> Save;                         // portero que para el tiro
        public static event Action<GameObject, GameObject> Interception;     // interceptor, pasador
        public static event Action<MatchState> MatchStateChanged;
        public static event Action<GameObject> BallRecovered;                // jugador que roba con J
        public static event Action<GameObject, string> AbilityUsed;          // jugador, nombre visible de la habilidad
        public static event Action<GameObject> ControlledPlayerChanged;      // jugador que pasa a controlar el humano

        public static void RaiseGoal(TeamSide scoringTeam, GameObject scorer) => Goal?.Invoke(scoringTeam, scorer);
        public static void RaiseSave(GameObject goalkeeper) => Save?.Invoke(goalkeeper);
        public static void RaiseInterception(GameObject interceptor, GameObject passer) => Interception?.Invoke(interceptor, passer);
        public static void RaiseMatchStateChanged(MatchState state) => MatchStateChanged?.Invoke(state);
        public static void RaiseBallRecovered(GameObject player) => BallRecovered?.Invoke(player);
        public static void RaiseAbilityUsed(GameObject player, string ability) => AbilityUsed?.Invoke(player, ability);
        public static void RaiseControlledPlayerChanged(GameObject player) => ControlledPlayerChanged?.Invoke(player);
        public static void RaisePassCompleted(GameObject from, GameObject to) => PassCompleted?.Invoke(from, to);
        public static void RaisePossessionChanged(TeamSide team) => PossessionChanged?.Invoke(team);

        // Limpia suscripciones al entrar en Play aunque el "domain reload" esté desactivado
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSubscriptions()
        {
            Goal = null;
            PassCompleted = null;
            PossessionChanged = null;
            Save = null;
            Interception = null;
            MatchStateChanged = null;
            BallRecovered = null;
            AbilityUsed = null;
            ControlledPlayerChanged = null;
        }
    }
}

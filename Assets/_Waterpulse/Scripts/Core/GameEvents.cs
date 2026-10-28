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
        public static event Action<GameObject> RecoverFailed;                // jugador que intenta robar con J y falla
        public static event Action<bool> PauseChanged;                       // true al pausar el partido, false al continuar
        public static event Action<GameObject> PassThrown;                   // pasador (al soltar la pelota; PassCompleted es al recibirla)
        public static event Action<GameObject> ShotTaken;                    // tirador
        public static event Action<GameObject> DashUsed;                     // jugador que hace el dash
        public static event Action BallBounced;                              // la pelota en vuelo choca con un palo, una pared o un jugador
        public static event Action GoldenGoalStarted;                        // se acaba el tiempo con empate: gol de oro
        public static event Action<GameObject> AbilityNoEnergy;              // jugador que pide su habilidad sin energía suficiente

        public static void RaiseGoal(TeamSide scoringTeam, GameObject scorer) => Goal?.Invoke(scoringTeam, scorer);
        public static void RaiseSave(GameObject goalkeeper) => Save?.Invoke(goalkeeper);
        public static void RaiseInterception(GameObject interceptor, GameObject passer) => Interception?.Invoke(interceptor, passer);
        public static void RaiseMatchStateChanged(MatchState state) => MatchStateChanged?.Invoke(state);
        public static void RaiseBallRecovered(GameObject player) => BallRecovered?.Invoke(player);
        public static void RaiseAbilityUsed(GameObject player, string ability) => AbilityUsed?.Invoke(player, ability);
        public static void RaiseControlledPlayerChanged(GameObject player) => ControlledPlayerChanged?.Invoke(player);
        public static void RaiseRecoverFailed(GameObject player) => RecoverFailed?.Invoke(player);
        public static void RaisePassCompleted(GameObject from, GameObject to) => PassCompleted?.Invoke(from, to);
        public static void RaisePossessionChanged(TeamSide team) => PossessionChanged?.Invoke(team);
        public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);
        public static void RaisePassThrown(GameObject passer) => PassThrown?.Invoke(passer);
        public static void RaiseShotTaken(GameObject shooter) => ShotTaken?.Invoke(shooter);
        public static void RaiseDashUsed(GameObject player) => DashUsed?.Invoke(player);
        public static void RaiseBallBounced() => BallBounced?.Invoke();
        public static void RaiseGoldenGoalStarted() => GoldenGoalStarted?.Invoke();
        public static void RaiseAbilityNoEnergy(GameObject player) => AbilityNoEnergy?.Invoke(player);

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
            RecoverFailed = null;
            PauseChanged = null;
            PassThrown = null;
            ShotTaken = null;
            DashUsed = null;
            BallBounced = null;
            GoldenGoalStarted = null;
            AbilityNoEnergy = null;
        }
    }
}

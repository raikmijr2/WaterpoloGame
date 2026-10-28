using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Gameplay;

namespace Waterpulse.Abilities
{
    // Xut del Tràngol: tiro muy potente. El portero solo lo para si le llega al cuerpo (si lo anticipa)
    public class XutTragolAbility : AbilityBehaviour
    {
        [Tooltip("Estela de la pelota (se enciende mientras vuela el tiro)")]
        [SerializeField] TrailRenderer ballTrail;

        int trailFlightId = -1;

        protected override AbilityDefinitionSO Definition => Player.PowerShot;
        protected override bool CanActivate() => Player.HasBall;

        protected override void Activate()
        {
            var ball = Player.Ball;
            if (!ball.Shoot(Player, Player.CurrentIntent.Move, Definition.power, Definition.goalkeeperReach, true)) return;
            if (ballTrail == null) return;
            trailFlightId = ball.FlightId;
            ballTrail.Clear();
            ballTrail.emitting = true;
        }

        // La estela se apaga cuando el tiro termina (gol, parada, rebote)
        void Update()
        {
            if (trailFlightId < 0) return;
            var ball = Player.Ball;
            if (ball.State == BallState.InFlight && ball.FlightId == trailFlightId) return;
            ballTrail.emitting = false;
            trailFlightId = -1;
        }
    }
}

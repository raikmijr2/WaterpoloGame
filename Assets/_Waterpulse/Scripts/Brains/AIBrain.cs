using System.Collections.Generic;
using UnityEngine;
using Waterpulse.AI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Brains
{
    // Cerebro de los jugadores de campo no controlados. Fase 3: se colocan según su rol.
    // La Fase 4 añadirá perseguir, presionar, interceptar, pasar y tirar
    [RequireComponent(typeof(PlayerController))]
    public class AIBrain : MonoBehaviour, IPlayerBrain
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] BallController ball;
        [SerializeField] PoolBounds pool;

        PlayerController self;
        readonly List<PlayerController> opponents = new List<PlayerController>();
        readonly List<Vector3> opponentPositions = new List<Vector3>();

        // Último objetivo calculado (útil para depurar)
        public Vector3 Target { get; private set; }

        void Awake() => self = GetComponent<PlayerController>();

        void Start()
        {
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (p.Team != self.Team && !p.IsGoalkeeper) opponents.Add(p);
        }

        public PlayerIntent GetIntent()
        {
            opponentPositions.Clear();
            foreach (var p in opponents) opponentPositions.Add(p.Position);

            var pitch = new RolePositioning.Pitch { Center = pool.Center, HalfLength = pool.HalfLength, HalfWidth = pool.HalfWidth };
            Target = RolePositioning.TargetFor(self.Role, self.Team, ball.HasPossession(self.Team), ball.Position,
                                               opponentPositions, pitch, config);

            Vector3 to = Target - self.Position;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance <= config.aiArriveRadius) return PlayerIntent.None;

            Vector3 move = to / distance * Mathf.Clamp01(distance / config.aiSlowRadius);
            return new PlayerIntent { Move = new Vector2(move.x, move.z) };
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, Target);
            Gizmos.DrawWireSphere(Target, 0.3f);
        }
    }
}

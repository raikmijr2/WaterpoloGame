using UnityEngine;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Partículas de agua de la pelota, solo visuales: una esquitxada pequeña cuando rebota (palos, paredes, jugadores)
    // y cuando un compañero recibe un pase, y una grande en la red al marcar. Las partículas se quedan donde salen
    // (espacio de mundo), aunque la pelota se mueva
    public class BallWaterFx : MonoBehaviour
    {
        [SerializeField] BallController ball;
        [SerializeField] ParticleSystem splash;
        [Tooltip("Esquitxada grande del gol")]
        [SerializeField] ParticleSystem goalSplash;
        [Tooltip("Gotas de la esquitxada al rebotar")]
        [SerializeField] int bounceDrops = 14;
        [Tooltip("Gotas de la esquitxada al recibir un pase")]
        [SerializeField] int catchDrops = 8;
        [Tooltip("Gotas de la esquitxada del gol")]
        [SerializeField] int goalDrops = 180;

        void OnEnable()
        {
            GameEvents.BallBounced += OnBounced;
            GameEvents.PassCompleted += OnPassCompleted;
            GameEvents.Goal += OnGoal;
        }

        void OnDisable()
        {
            GameEvents.BallBounced -= OnBounced;
            GameEvents.PassCompleted -= OnPassCompleted;
            GameEvents.Goal -= OnGoal;
        }

        void OnBounced() => Burst(splash, bounceDrops);
        void OnPassCompleted(GameObject from, GameObject to) => Burst(splash, catchDrops);
        void OnGoal(TeamSide team, GameObject scorer) => Burst(goalSplash, goalDrops);

        // Gotas en la posición de la pelota (la forma del sistema reparte las direcciones)
        void Burst(ParticleSystem system, int drops)
        {
            var emit = new ParticleSystem.EmitParams { position = ball.Position, applyShapeToPosition = true };
            system.Emit(emit, drops);
        }
    }
}

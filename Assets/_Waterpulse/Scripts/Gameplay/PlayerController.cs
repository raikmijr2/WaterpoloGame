using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Ejecuta las intenciones de su IPlayerBrain (humano o IA) moviendo el Rigidbody por el plano del agua
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;

        Rigidbody body;
        IPlayerBrain brain;
        PlayerIntent intent;

        public PlayerIntent CurrentIntent => intent;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            brain = GetComponent<IPlayerBrain>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void Update()
        {
            intent = brain != null ? brain.GetIntent() : PlayerIntent.None;
        }

        void FixedUpdate()
        {
            Vector2 move = Vector2.ClampMagnitude(intent.Move, 1f);
            Vector3 direction = new Vector3(move.x, 0f, move.y);
            bool hasInput = direction.sqrMagnitude > 0.0001f;

            // Con input acelera hacia la velocidad deseada; sin input solo frena el damping (deriva en el agua)
            body.linearDamping = hasInput ? 0f : config.linearDamping;
            if (hasInput)
            {
                Vector3 current = body.linearVelocity;
                current.y = 0f;
                Vector3 target = direction * config.maxSpeed;
                body.linearVelocity = Vector3.MoveTowards(current, target, config.acceleration * Time.fixedDeltaTime);

                Quaternion look = Quaternion.LookRotation(direction);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, look, config.turnSpeed * Time.fixedDeltaTime));
            }

            // Los choques no deben hacer girar al jugador
            body.angularVelocity = Vector3.zero;
        }
    }
}

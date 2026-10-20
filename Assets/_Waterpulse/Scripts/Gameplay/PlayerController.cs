using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Ejecuta las intenciones de su IPlayerBrain (humano o IA): nadar, pasar y tirar. No sabe quién lo controla
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] TeamSide team;
        [SerializeField] PlayerRole role;
        [SerializeField] BallController ball;

        Rigidbody body;
        IPlayerBrain defaultBrain;
        IPlayerBrain brain;
        PlayerIntent intent;
        float recoverCooldownTimer;
        bool canAct = true;   // solo se obedece al cerebro mientras se juega (no en saques, goles ni final)

        public TeamSide Team => team;
        public PlayerRole Role => role;
        public bool IsGoalkeeper => role == PlayerRole.Goalkeeper;
        public bool CanTryRecover => recoverCooldownTimer <= 0f;
        public bool HasBall => ball != null && ball.Holder == this;
        public PlayerIntent CurrentIntent => intent;
        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            defaultBrain = GetComponent<IPlayerBrain>();
            brain = defaultBrain;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void OnEnable() => GameEvents.MatchStateChanged += OnMatchStateChanged;
        void OnDisable() => GameEvents.MatchStateChanged -= OnMatchStateChanged;
        void OnMatchStateChanged(MatchState state) => canAct = state == MatchState.Playing;

        // Asigna un cerebro externo (p. ej. el del humano). ResetBrain vuelve al propio del jugador
        public void SetBrain(IPlayerBrain newBrain) => brain = newBrain;
        public void ResetBrain() => brain = defaultBrain;

        void Update()
        {
            intent = canAct && brain != null ? brain.GetIntent() : PlayerIntent.None;
            if (recoverCooldownTimer > 0f) recoverCooldownTimer -= Time.deltaTime;

            // Las pulsaciones duran un frame: se ejecutan aquí y no en FixedUpdate
            if (HasBall)
            {
                if (intent.Pass) ball.Pass(this, AimDirection());
                else if (intent.Shoot) ball.Shoot(this, intent.Move);
            }
            else if (intent.Pass && !IsGoalkeeper && CanTryRecover)
            {
                // J sin pelota: intentar recuperarla. Si falla, espera antes de poder repetir
                if (ball.TryRecover(this) == RecoverResult.Failed) recoverCooldownTimer = config.recoverCooldown;
            }
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

        // Dirección del stick; sin stick, hacia donde mira
        Vector3 AimDirection()
        {
            Vector3 aim = new Vector3(intent.Move.x, 0f, intent.Move.y);
            return aim.sqrMagnitude > 0.04f ? aim.normalized : new Vector3(Forward.x, 0f, Forward.z).normalized;
        }

        // Recolocación instantánea (reanudaciones)
        public void Teleport(Vector3 position, Vector3 facing)
        {
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing) : transform.rotation;
            body.linearVelocity = Vector3.zero;
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}

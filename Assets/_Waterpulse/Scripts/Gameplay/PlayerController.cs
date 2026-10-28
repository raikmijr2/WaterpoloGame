using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Ejecuta las intenciones de su IPlayerBrain (humano o IA): nadar, pasar, tirar y pedir su habilidad.
    // No sabe quién lo controla ni cómo funciona la habilidad: solo avisa con AbilityRequested
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] TeamSide team;
        [SerializeField] PlayerRole role;
        [SerializeField] BallController ball;
        [Tooltip("Habilidad especial del jugador (vacío = sin habilidad)")]
        [SerializeField] AbilityDefinitionSO ability;

        Rigidbody body;
        IPlayerBrain defaultBrain;
        IPlayerBrain brain;
        PlayerIntent intent;
        float recoverCooldownTimer;
        bool canAct = true;   // solo se obedece al cerebro mientras se juega (no en saques, goles ni final)
        bool paused;          // ni en la pausa (el tiempo está parado, pero Update sigue leyendo el input)
        float boostTimer;
        Vector3 boostDirection;
        float boostMultiplier;
        PlayerStamina stamina;
        ShotMeter shotMeter;
        Transform visual;           // hijo "Visual": el saltito y la subida del tiro solo se ven, no mueven el cuerpo
        float visualBaseY;
        float freezeTimer;          // quieto tras pasar
        float hopTimer;             // saltito del pase
        // Lanzamiento pendiente (pase o tiro): la pelota sale con el evento ReleaseBall de la animación
        // o, si no hay animación o no llega, al acabar throwTimer
        enum PendingThrow { None, Pass, Shot }
        PendingThrow pendingThrow;
        float throwTimer;
        Vector3 passAim;
        Vector2 shotStick;
        float shotQuality;
        float shotRise;             // altura a la que se quedó al soltar la barra

        public TeamSide Team => team;
        public PlayerRole Role => role;
        public AbilityDefinitionSO Ability => ability;
        public BallController Ball => ball;
        public bool IsGoalkeeper => role == PlayerRole.Goalkeeper;
        public bool IsBoosting => boostTimer > 0f;
        public PlayerStamina Stamina => stamina;
        public bool CanTryRecover => recoverCooldownTimer <= 0f;
        public bool HasBall => ball != null && ball.Holder == this;
        // Barra de tiro: K empieza a cargar y K otra vez suelta el tiro
        public bool IsChargingShot => shotMeter != null && shotMeter.IsCharging;
        public float ShotFill => shotMeter != null ? shotMeter.Fill : 0f;
        public ShotMeter ShotMeter => shotMeter;
        public float ShotGreenStart => shotMeter != null ? shotMeter.GreenStart : 0f;
        public float ShotGreenEnd => shotMeter != null ? shotMeter.GreenEnd : 0f;
        // Quieto mientras pasa (saltito) o carga el tiro
        public bool IsBusy => IsChargingShot || pendingThrow != PendingThrow.None || freezeTimer > 0f;
        // Velocidad respecto a la máxima (0 = quieto, 1 = máxima; más durante un acelerón)
        public float SpeedRatio => config.maxSpeed > 0f ? FlatSpeed() / config.maxSpeed : 0f;
        // Hay un modelo animado que avisará con ReleaseBall (lo registra PlayerAnimator)
        public bool HasThrowAnimation { get; set; }
        // Mano del modelo en la que se ve la pelota (null = sin modelo: la pelota se ve donde está). La pone PlayerAnimator
        public Transform BallHand { get; set; }
        public GameConfigSO Config => config;

        // Empieza un lanzamiento (pase o tiro) y un intento de robo al alcance: los usa la animación
        public event System.Action ThrowStarted;
        public event System.Action StealAttempted;

        // El jugador ha pulsado la habilidad (L). Lo ejecuta el componente de habilidad que tenga
        public event System.Action AbilityRequested;
        public PlayerIntent CurrentIntent => intent;
        public Vector3 Position => transform.position;
        // Pose física (la del último paso de física, sin interpolar): para colocar cosas que también se mueven en FixedUpdate
        public Vector3 BodyPosition => body != null ? body.position : transform.position;
        public Vector3 BodyForward => body != null ? body.rotation * Vector3.forward : transform.forward;
        public Vector3 Forward => transform.forward;
        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            defaultBrain = GetComponent<IPlayerBrain>();
            stamina = GetComponent<PlayerStamina>();   // los porteros no tienen
            brain = defaultBrain;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            shotMeter = new ShotMeter(config.shotMeterDuration, config.shotGreenStart, config.shotGreenWidth, config.shotOffGreenMaxQuality);
            visual = transform.Find("Visual");
            if (visual != null) visualBaseY = visual.localPosition.y;
        }

        void OnEnable()
        {
            GameEvents.MatchStateChanged += OnMatchStateChanged;
            GameEvents.PauseChanged += OnPauseChanged;
        }

        void OnDisable()
        {
            GameEvents.MatchStateChanged -= OnMatchStateChanged;
            GameEvents.PauseChanged -= OnPauseChanged;
        }

        void OnPauseChanged(bool isPaused) => paused = isPaused;

        void OnMatchStateChanged(MatchState state)
        {
            canAct = state == MatchState.Playing;
            if (!canAct)
            {
                boostTimer = 0f;
                CancelActions();
            }
        }

        // Corta el pase pendiente, la carga del tiro y la pausa de después del pase
        void CancelActions()
        {
            shotMeter.Cancel();
            pendingThrow = PendingThrow.None;
            freezeTimer = 0f;
            hopTimer = 0f;
        }

        // Asigna un cerebro externo (p. ej. el del humano). ResetBrain vuelve al propio del jugador
        public void SetBrain(IPlayerBrain newBrain) => brain = newBrain;
        public void ResetBrain() => brain = defaultBrain;

        void Update()
        {
            intent = canAct && !paused && brain != null ? brain.GetIntent() : PlayerIntent.None;
            float dt = Time.deltaTime;
            if (recoverCooldownTimer > 0f) recoverCooldownTimer -= dt;
            if (freezeTimer > 0f) freezeTimer -= dt;
            if (hopTimer > 0f) hopTimer -= dt;

            // Si le roban la pelota (o la suelta) mientras pasa o carga, se corta la acción
            if (!HasBall && (pendingThrow != PendingThrow.None || shotMeter.IsCharging))
            {
                pendingThrow = PendingThrow.None;
                shotMeter.Cancel();
            }

            if (pendingThrow != PendingThrow.None)
            {
                // Esperando a que la mano suelte la pelota (evento de la animación) o a que se acabe el tiempo
                throwTimer -= dt;
                if (throwTimer <= 0f) ReleaseBall();
                return;
            }

            if (shotMeter.IsCharging)
            {
                // Segunda K: la calidad es la de donde está la barra. Si se llena, tira solo (con la peor).
                // Apunta con el stick del momento de soltar
                bool full = shotMeter.Tick(dt);
                if (intent.Shoot || full)
                {
                    shotRise = shotMeter.Fill * config.shotRiseHeight;
                    shotStick = intent.Move;
                    shotQuality = shotMeter.Release();
                    // Sin animación sale al momento, como antes
                    BeginThrow(PendingThrow.Shot, HasThrowAnimation ? config.throwReleaseTimeout : 0f);
                }
                return;
            }

            // Las pulsaciones duran un frame: se ejecutan aquí y no en FixedUpdate
            if (intent.Ability) AbilityRequested?.Invoke();

            if (intent.Dash) TryDash();

            if (HasBall)
            {
                if (intent.Pass) StartPass();
                else if (intent.Shoot)
                {
                    // Primera K: empieza a cargar; se queda quieto y va subiendo
                    boostTimer = 0f;
                    shotMeter.Begin();
                }
            }
            else if (intent.Pass && !IsGoalkeeper && CanTryRecover)
            {
                // J sin pelota: intentar recuperarla. Si falla, espera antes de poder repetir
                var result = ball.TryRecover(this);
                if (result != RecoverResult.OutOfRange) StealAttempted?.Invoke();
                if (result == RecoverResult.Failed)
                {
                    recoverCooldownTimer = config.recoverCooldown;
                    GameEvents.RaiseRecoverFailed(gameObject);
                }
            }
        }

        void FixedUpdate()
        {
            if (boostTimer > 0f)
            {
                // Acelerón: dirección fija, no se puede girar hasta que acaba
                boostTimer -= Time.fixedDeltaTime;
                body.linearDamping = 0f;
                body.linearVelocity = boostDirection * (config.maxSpeed * boostMultiplier);
                body.MoveRotation(Quaternion.LookRotation(boostDirection));
                body.angularVelocity = Vector3.zero;
                return;
            }

            if (IsBusy)
            {
                // Pasando o cargando el tiro: completamente quieto
                body.linearDamping = 0f;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                return;
            }

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
                // Si va más rápido de lo que pide el stick (nadar lento, joystick poco inclinado) frena casi en seco;
                // si no, acelera suave como en el agua
                bool braking = target.magnitude < current.magnitude;
                float rate = braking ? config.slowBrakeDeceleration : config.acceleration;
                body.linearVelocity = Vector3.MoveTowards(current, target, rate * Time.fixedDeltaTime);

                Quaternion look = Quaternion.LookRotation(direction);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, look, config.turnSpeed * Time.fixedDeltaTime));
            }

            // Los choques no deben hacer girar al jugador
            body.angularVelocity = Vector3.zero;
        }

        // Pase: se para en seco y da un saltito. Con animación, la pelota sale con su evento ReleaseBall
        // (como mucho a los throwReleaseTimeout); sin animación, al momento
        void StartPass()
        {
            freezeTimer = config.passFreezeDuration;
            hopTimer = config.passFreezeDuration;
            passAim = AimDirection();
            BeginThrow(PendingThrow.Pass, HasThrowAnimation ? config.throwReleaseTimeout : 0f);
        }

        void BeginThrow(PendingThrow kind, float maxWait)
        {
            boostTimer = 0f;
            pendingThrow = kind;
            throwTimer = maxWait;
            ThrowStarted?.Invoke();
            if (maxWait <= 0f) ReleaseBall();
        }

        // La pelota sale de la mano. Lo llama el evento de la animación (a través de PlayerAnimator)
        // o el propio jugador si el evento no llega a tiempo
        public void ReleaseBall()
        {
            var kind = pendingThrow;
            pendingThrow = PendingThrow.None;
            if (kind == PendingThrow.Pass) ball.Pass(this, passAim);
            else if (kind == PendingThrow.Shot) ball.Shoot(this, shotStick, shotQuality);
        }

        // Saltito del pase y subida mientras carga el tiro: solo se mueve el hijo "Visual"
        void LateUpdate()
        {
            if (visual == null) return;
            float height = 0f;
            if (hopTimer > 0f && config.passFreezeDuration > 0f)
                height = Mathf.Sin(Mathf.PI * (1f - hopTimer / config.passFreezeDuration)) * config.passHopHeight;
            else if (shotMeter.IsCharging)
                height = shotMeter.Fill * config.shotRiseHeight;
            else if (pendingThrow == PendingThrow.Shot)
                height = shotRise;   // arriba hasta que suelta la pelota
            Vector3 local = visual.localPosition;
            local.y = visualBaseY + height;
            visual.localPosition = local;
        }

        // Dash: sirve para recuperar velocidad. Cuanto más lento vas, más fuerte; a velocidad máxima no se nota,
        // así que no se hace ni gasta estamina. No se encadena con otro acelerón
        public float DashStrength => DashPower.Strength(FlatSpeed(), config.maxSpeed);

        void TryDash()
        {
            if (IsBoosting || stamina == null || DashStrength < config.dashMinStrength) return;
            if (!stamina.TrySpendDash()) return;
            float multiplier = DashPower.SpeedMultiplier(FlatSpeed(), config.maxSpeed, config.dashSpeedMultiplier);
            StartBoost(AimDirection(), multiplier, config.dashDuration);
            GameEvents.RaiseDashUsed(gameObject);
        }

        float FlatSpeed()
        {
            Vector3 v = body.linearVelocity;
            v.y = 0f;
            return v.magnitude;
        }

        // Acelerón temporal en una dirección fija (lo usan el Torpede Blau y el dash)
        public void StartBoost(Vector3 direction, float speedMultiplier, float duration)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            boostDirection = direction.normalized;
            boostMultiplier = speedMultiplier;
            boostTimer = duration;
        }

        // Dirección del stick; sin stick, hacia donde mira
        public Vector3 AimDirection()
        {
            Vector3 aim = new Vector3(intent.Move.x, 0f, intent.Move.y);
            return aim.sqrMagnitude > 0.04f ? aim.normalized : new Vector3(Forward.x, 0f, Forward.z).normalized;
        }

        // Recolocación instantánea (reanudaciones)
        public void Teleport(Vector3 position, Vector3 facing)
        {
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing) : transform.rotation;
            boostTimer = 0f;
            CancelActions();
            body.linearVelocity = Vector3.zero;
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}

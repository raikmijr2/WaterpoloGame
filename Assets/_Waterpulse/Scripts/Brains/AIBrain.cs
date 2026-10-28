using System.Collections.Generic;
using UnityEngine;
using Waterpulse.AI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Brains
{
    // Cerebro de los jugadores de campo no controlados. Produce el mismo PlayerIntent que un humano.
    // Con un TeamAI pasivo (compañeros del humano) se colocan por rol y marcan; con uno agresivo (rivales)
    // además persiguen la pelota, presionan, cortan pases, pasan y tiran
    [RequireComponent(typeof(PlayerController))]
    public class AIBrain : MonoBehaviour, IPlayerBrain
    {
        public enum State { Position, Mark, Attack, Chase, Intercept, Press }

        [SerializeField] GameConfigSO config;
        [SerializeField] BallController ball;
        [SerializeField] PoolBounds pool;
        [Tooltip("Coordinación del equipo (reparte presión y marcajes)")]
        [SerializeField] TeamAI teamAI;

        // Magnitud mínima del stick para que PlayerController la tome como dirección de pase
        const float PassAimMagnitude = 0.25f;

        PlayerController self;
        PlayerEnergy energy;
        readonly List<PlayerController> teammates = new List<PlayerController>();
        readonly List<PlayerController> fieldOpponents = new List<PlayerController>();
        readonly List<PlayerController> allOpponents = new List<PlayerController>();
        readonly List<Vector3> teammatePositions = new List<Vector3>();
        readonly List<Vector3> fieldOpponentPositions = new List<Vector3>();
        readonly List<Vector3> allOpponentPositions = new List<Vector3>();
        float decisionTimer;
        float holdTimer;      // tiempo que lleva con la pelota
        float attackLaneZ;    // carril (Z) por el que avanza con la pelota
        float shotReleaseFill; // punto de la barra de tiro en el que soltará el tiro
        float shotPost;        // palo al que apunta (eje Y del stick)

        public State Current { get; private set; }
        public Vector3 Target { get; private set; }

        bool Aggressive => teamAI != null && teamAI.IsAggressive;
        float SpeedScale => Aggressive ? teamAI.Difficulty.rivalSpeedMultiplier : 1f;
        float ReactionTime => teamAI != null ? teamAI.Difficulty.reactionTime : 0f;

        void Awake()
        {
            self = GetComponent<PlayerController>();
            energy = GetComponent<PlayerEnergy>();
        }

        // La IA rival usa su habilidad si la dificultad lo permite, es del tipo pedido y tiene energía
        bool CanUseAbility(AbilityKind kind)
        {
            var ability = self.Ability;
            return Aggressive && teamAI.Difficulty.useAbilities && ability != null && ability.kind == kind
                   && energy != null && energy.CanSpend(ability.cost);
        }

        void Start()
        {
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (p == self) continue;
                if (p.Team == self.Team) { if (!p.IsGoalkeeper) teammates.Add(p); }
                else
                {
                    allOpponents.Add(p);
                    if (!p.IsGoalkeeper) fieldOpponents.Add(p);
                }
            }
        }

        public PlayerIntent GetIntent()
        {
            RefreshPositions();
            decisionTimer -= Time.deltaTime;
            bool decide = decisionTimer <= 0f;
            if (decide) decisionTimer = ReactionTime;

            // Tiempo con la pelota: al recibirla empieza por su carril actual
            if (self.HasBall)
            {
                if (holdTimer <= 0f) attackLaneZ = self.Position.z;
                holdTimer += Time.deltaTime;
            }
            else holdTimer = 0f;

            // Cargando el tiro: suelta (segunda pulsación) al llegar al punto de la barra elegido
            if (self.IsChargingShot)
                return new PlayerIntent { Move = new Vector2(0f, shotPost), Shoot = self.ShotFill >= shotReleaseFill };

            if (Aggressive)
            {
                // Persecución, corte y presión a máxima velocidad: si frenaran al llegar, no alcanzarían a nadie
                if (self.HasBall) return Attack(decide);
                if (teamAI.Chaser == self && ball.State == BallState.Free) return Chase();
                if (teamAI.Interceptor == self && ball.State == BallState.InFlight) return WithDash(Go(State.Intercept, teamAI.InterceptPoint, true));
                if (teamAI.Presser == self && ball.Holder != null) return Press(decide);
            }

            if (teamAI != null && teamAI.TryGetMark(self, out var rival))
            {
                var mark = RolePositioning.MarkPosition(rival.Position, self.Team, Pitch(), config);
                return Go(State.Mark, mark);
            }
            return Go(State.Position, FormationTarget());
        }

        // ---------- Con pelota ----------

        PlayerIntent Attack(bool decide)
        {
            Current = State.Attack;
            Vector3 goal = pool.AttackedGoalCenter(self.Team);
            float sign = self.Team.AttackSign();

            if (decide)
            {
                var settings = new AttackDecision.Settings
                {
                    ShootRange = config.aiShootRange,
                    PressureRadius = config.aiPressureRadius,
                    LaneWidth = config.aiLaneWidth,
                    MaxHoldTime = config.aiMaxHoldTime,
                    PassAdvanceGain = config.aiPassAdvanceGain,
                };
                var choice = AttackDecision.Decide(self.Position, sign, goal, teammatePositions, fieldOpponentPositions,
                                                   allOpponentPositions, holdTimer, settings);

                // Avanzar mantiene su carril; reposicionarse desplaza el carril en lateral para buscar hueco de tiro
                attackLaneZ = choice.Action == AttackAction.Reposition
                    ? Mathf.Clamp(self.Position.z + choice.LateralSign * config.aiRepositionStep,
                                  pool.Center.z - pool.HalfWidth + config.formationEdgeMargin,
                                  pool.Center.z + pool.HalfWidth - config.formationEdgeMargin)
                    : self.Position.z;
                if (choice.Action == AttackAction.Shoot)
                {
                    // Apunta a un palo al azar: el eje Y del stick elige el palo
                    float post = Random.value < 0.5f ? -1f : 1f;
                    // Si iba a tirar y tiene energía, usa el Xut del Tràngol en vez del tiro normal
                    bool power = CanUseAbility(AbilityKind.PowerShot);
                    if (!power)
                    {
                        shotPost = post;
                        shotReleaseFill = ChooseShotReleaseFill();
                    }
                    return new PlayerIntent { Move = new Vector2(0f, post), Shoot = !power, Ability = power };
                }
                if (choice.Action == AttackAction.Pass)
                {
                    Vector3 dir = Flat(teammates[choice.PassIndex].Position - self.Position).normalized * PassAimMagnitude;
                    return new PlayerIntent { Move = new Vector2(dir.x, dir.z), Pass = true };
                }
            }

            // Avanza hacia la zona de tiro por su carril
            Vector3 spot = goal - Vector3.right * (sign * config.aiAdvanceDistance);
            spot.z = attackLaneZ;
            return Go(State.Attack, spot);
        }

        // Dónde soltará la barra de tiro: en el verde con la probabilidad de la dificultad; si no, antes de llegar
        float ChooseShotReleaseFill()
        {
            float greenStart = self.ShotGreenStart;
            float greenEnd = self.ShotGreenEnd;
            var difficulty = teamAI.Difficulty;
            // Primera mitad del verde: la barra avanza a saltos de un frame y no debe pasarse de la zona
            if (Random.value < difficulty.shotGreenChance) return Random.Range(greenStart, (greenStart + greenEnd) * 0.5f);
            return Random.Range(Mathf.Min(difficulty.shotMissMinFill, greenStart), greenStart);
        }

        // ---------- Sin pelota ----------

        // A por la pelota suelta; si está lejos usa el Torpede Blau y, si no lo tiene o no le llega, el dash
        PlayerIntent Chase()
        {
            PlayerIntent intent = Go(State.Chase, ball.Position, true);
            float distance = Flat(ball.Position - self.Position).magnitude;
            if (distance > config.aiBoostDistance && !self.IsBoosting && CanUseAbility(AbilityKind.Boost))
            {
                intent.Ability = true;
                return intent;
            }
            return WithDash(intent);
        }

        // Dash de la IA rival si su objetivo está lejos, tiene estamina y va lo bastante lenta para que se note
        PlayerIntent WithDash(PlayerIntent intent)
        {
            var stamina = self.Stamina;
            if (stamina == null || self.IsBoosting || !stamina.CanDash || self.DashStrength < config.dashMinStrength) return intent;
            if (Flat(Target - self.Position).magnitude > config.aiDashDistance) intent.Dash = true;
            return intent;
        }

        // Se coloca entre el poseedor y su portería y, según la agresividad, intenta recuperar con J
        PlayerIntent Press(bool decide)
        {
            var holder = ball.Holder;
            Vector3 ownGoal = pool.AttackedGoalCenter(self.Team.Opponent());
            Vector3 spot = holder.Position + Flat(ownGoal - holder.Position).normalized * config.aiPressDistance;
            PlayerIntent intent = WithDash(Go(State.Press, spot, true));

            bool inRange = Flat(holder.Position - self.Position).magnitude <= config.recoverRange
                           && RecoverGeometry.IsInFront(holder.Position, holder.Forward, self.Position, config.recoverFrontAngle);
            if (decide && inRange && self.CanTryRecover && Random.value < teamAI.Difficulty.aggressiveness)
                intent.Pass = true;
            return intent;
        }

        Vector3 FormationTarget()
        {
            return RolePositioning.TargetFor(self.Role, self.Team, ball.HasPossession(self.Team), ball.Position, Pitch(), config);
        }

        RolePositioning.Pitch Pitch()
        {
            return new RolePositioning.Pitch { Center = pool.Center, HalfLength = pool.HalfLength, HalfWidth = pool.HalfWidth };
        }

        // ---------- Movimiento ----------

        // fullSpeed: no frena al acercarse (perseguir, presionar, cortar); si no, frena para no pasarse de su sitio
        PlayerIntent Go(State state, Vector3 target, bool fullSpeed = false)
        {
            Current = state;
            Target = target;
            Vector3 to = Flat(target - self.Position);
            float distance = to.magnitude;
            if (distance <= config.aiArriveRadius) return PlayerIntent.None;
            float brake = fullSpeed ? 1f : Mathf.Clamp01(distance / config.aiSlowRadius);
            Vector3 move = to / distance * brake * SpeedScale;
            return new PlayerIntent { Move = new Vector2(move.x, move.z) };
        }

        void RefreshPositions()
        {
            Fill(teammatePositions, teammates);
            Fill(fieldOpponentPositions, fieldOpponents);
            Fill(allOpponentPositions, allOpponents);
        }

        static void Fill(List<Vector3> positions, List<PlayerController> players)
        {
            positions.Clear();
            foreach (var p in players) positions.Add(p.Position);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, Target);
            Gizmos.DrawWireSphere(Target, 0.3f);
        }
    }
}

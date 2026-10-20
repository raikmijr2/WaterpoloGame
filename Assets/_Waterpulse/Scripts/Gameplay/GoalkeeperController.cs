using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Portero (IA): sigue la pelota delante de su portería, reacciona a los tiros, intenta pararlos
    // por probabilidad y, si se queda la pelota, saca en corto a un compañero libre
    [RequireComponent(typeof(PlayerController))]
    public class GoalkeeperController : MonoBehaviour, IPlayerBrain
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] BallController ball;
        [SerializeField] PoolBounds pool;

        // Magnitud mínima del stick para que PlayerController la tome como dirección de pase
        const float PassAimMagnitude = 0.25f;

        PlayerController self;
        PlayerController[] players;
        float holdTimer;
        int rolledFlightId = -1;

        // Signo hacia el campo desde su portería
        float Inward => self.Team.AttackSign();
        Vector3 OwnGoalCenter => pool.Center - Vector3.right * (Inward * pool.HalfLength);
        Vector3 HomePosition => OwnGoalCenter + Vector3.right * (Inward * config.goalkeeperDepth);
        float LateralLimit => config.goalWidth * 0.5f + config.goalkeeperLateralExtra;

        void Awake() => self = GetComponent<PlayerController>();

        void Start() => players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        void FixedUpdate() => TrySave();

        public PlayerIntent GetIntent()
        {
            if (self.HasBall) return Distribute();
            holdTimer = 0f;

            Vector3 target = HomePosition;
            float speed = config.goalkeeperSpeed;

            if (IsShotAtMyGoal())
            {
                // Se lanza hacia el punto por donde la pelota cruzará su línea
                target.z = Mathf.Clamp(PredictLateralAtMyLine(), -LateralLimit, LateralLimit) + pool.Center.z;
                speed = config.goalkeeperReactionSpeed;
            }
            else if (ball.State == BallState.Free && Flat(ball.Position - HomePosition).magnitude <= config.goalkeeperLooseBallRadius)
            {
                target = ClampToArea(ball.Position);
                speed = config.goalkeeperReactionSpeed;
            }
            else
            {
                float lateral = (ball.Position.z - pool.Center.z) * config.goalkeeperFollow;
                target.z = pool.Center.z + Mathf.Clamp(lateral, -LateralLimit, LateralLimit);
            }

            return Steer(target, speed);
        }

        // Con la pelota: espera un momento y la pasa al compañero libre más cercano
        PlayerIntent Distribute()
        {
            holdTimer += Time.deltaTime;
            if (holdTimer < config.goalkeeperHoldTime) return Steer(HomePosition, config.goalkeeperSpeed);

            PlayerController mate = PickOutlet();
            if (mate == null) return PlayerIntent.None;
            holdTimer = 0f;
            Vector3 dir = Flat(mate.Position - self.Position).normalized * PassAimMagnitude;
            return new PlayerIntent { Move = new Vector2(dir.x, dir.z), Pass = true };
        }

        PlayerController PickOutlet()
        {
            PlayerController nearestFree = null, nearest = null;
            float bestFree = float.MaxValue, best = float.MaxValue;
            foreach (var p in players)
            {
                if (p.Team != self.Team || p.IsGoalkeeper) continue;
                float d = Flat(p.Position - self.Position).magnitude;
                if (d < best) { best = d; nearest = p; }
                if (d < bestFree && IsFree(p)) { bestFree = d; nearestFree = p; }
            }
            return nearestFree != null ? nearestFree : nearest;
        }

        bool IsFree(PlayerController mate)
        {
            foreach (var p in players)
            {
                if (p.Team == self.Team) continue;
                if (Flat(p.Position - mate.Position).magnitude < config.goalkeeperOutletFreeDistance) return false;
            }
            return true;
        }

        // Una tirada por tiro, cuando la pelota llega a su altura
        void TrySave()
        {
            if (!IsShotAtMyGoal() || ball.FlightId == rolledFlightId) return;
            float ahead = (ball.Position.x - self.Position.x) * Inward;   // > 0 mientras la pelota aún viene
            if (ahead > config.ballRadius) return;

            rolledFlightId = ball.FlightId;
            float distance = Flat(ball.Position - self.Position).magnitude;
            float chance = SaveOdds.Chance(distance, config.playerRadius, ball.ShotSaveReach, config.saveChanceAtBody);
            if (Random.value < chance && ball.CatchByGoalkeeper(self)) GameEvents.RaiseSave(gameObject);
        }

        bool IsShotAtMyGoal()
        {
            return ball.State == BallState.InFlight && ball.FlightKind == FlightKind.Shot
                   && ball.FlightFrom != null && ball.FlightFrom.Team != self.Team;
        }

        // Coordenada lateral (relativa al centro) en la que la pelota cruzará la x del portero
        float PredictLateralAtMyLine()
        {
            Vector3 v = ball.Velocity;
            if (Mathf.Abs(v.x) < 0.01f) return ball.Position.z - pool.Center.z;
            float t = (self.Position.x - ball.Position.x) / v.x;
            if (t < 0f) t = 0f;
            return ball.Position.z + v.z * t - pool.Center.z;
        }

        Vector3 ClampToArea(Vector3 point)
        {
            float depth = Mathf.Clamp((point.x - OwnGoalCenter.x) * Inward, config.goalkeeperDepth, config.goalkeeperMaxDepth);
            point.x = OwnGoalCenter.x + depth * Inward;
            point.z = pool.Center.z + Mathf.Clamp(point.z - pool.Center.z, -LateralLimit, LateralLimit);
            return point;
        }

        // Convierte un punto objetivo en intención; la velocidad deseada se expresa como fracción de la máxima
        PlayerIntent Steer(Vector3 target, float desiredSpeed)
        {
            Vector3 to = Flat(target - self.Position);
            float distance = to.magnitude;
            if (distance <= config.goalkeeperArriveRadius) return PlayerIntent.None;
            float scale = Mathf.Clamp01(desiredSpeed / config.maxSpeed) * Mathf.Clamp01(distance / config.goalkeeperSlowRadius);
            Vector3 move = to / distance * scale;
            return new PlayerIntent { Move = new Vector2(move.x, move.z) };
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}

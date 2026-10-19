using System;
using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    public enum BallState { Free, Held, InFlight }
    public enum FlightKind { Pass, Shot }

    // Pelota con estados explícitos: Free, Held(owner) e InFlight(Pass|Shot, from, target).
    // Es el único sistema que cambia el estado de la pelota
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] PoolBounds pool;

        // Distancia extra que recorre un tiro más allá de la línea de gol (para entrar en la red)
        const float ShotOvershoot = 1f;

        Rigidbody body;
        readonly List<PlayerController> players = new List<PlayerController>();
        readonly List<PlayerController> candidates = new List<PlayerController>();
        readonly List<Vector3> candidatePositions = new List<Vector3>();
        Vector3 flightStart;
        float flightDistance;
        PlayerController releaser;
        float releaserTimer;
        bool hasPossessionTeam;
        TeamSide possessionTeam;

        public BallState State { get; private set; } = BallState.Free;
        public PlayerController Holder { get; private set; }
        public FlightKind FlightKind { get; private set; }
        public PlayerController FlightFrom { get; private set; }
        public PlayerController FlightTarget { get; private set; }
        public PlayerController LastToucher { get; private set; }
        public bool IsFrozen { get; private set; }
        public Vector3 Position => body != null ? body.position : transform.position;
        public Vector3 Velocity => body != null && !body.isKinematic ? body.linearVelocity : Vector3.zero;

        // Identificador del vuelo actual (cambia en cada pase o tiro)
        public int FlightId { get; private set; }

        // Equipo que tuvo la pelota por última vez (se mantiene mientras está libre o en vuelo)
        public bool HasPossession(TeamSide team) => hasPossessionTeam && possessionTeam == team;

        // Poseedor anterior, poseedor nuevo (null si la pelota queda libre o en vuelo)
        public event Action<PlayerController, PlayerController> HolderChanged;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            players.AddRange(FindObjectsByType<PlayerController>(FindObjectsSortMode.None));

            // Los jugadores recogen la pelota por distancia y los porteros paran por probabilidad:
            // ningún jugador debe empujarla ni hacerla rebotar físicamente
            var ballCollider = GetComponent<SphereCollider>();
            foreach (var p in players)
                foreach (var c in p.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(ballCollider, c);
        }

        void FixedUpdate()
        {
            if (releaserTimer > 0f) releaserTimer -= Time.fixedDeltaTime;
            if (IsFrozen) return;

            switch (State)
            {
                case BallState.Held:
                    body.MovePosition(HoldPoint(Holder));
                    break;
                case BallState.Free:
                    body.linearDamping = config.ballLinearDamping;
                    TryPickup();
                    break;
                case BallState.InFlight:
                    body.linearDamping = 0f;
                    if (FlightKind == FlightKind.Pass && TryCatchPass()) break;
                    if (FlatDistance(flightStart, body.position) >= flightDistance) BecomeFree();
                    break;
            }
        }

        // ---------- Acciones públicas (las piden los jugadores) ----------

        public bool Pass(PlayerController from, Vector3 aim)
        {
            if (Holder != from || IsFrozen) return false;

            candidates.Clear();
            candidatePositions.Clear();
            foreach (var p in players)
            {
                if (p == from || p.Team != from.Team || p.IsGoalkeeper) continue;
                candidates.Add(p);
                candidatePositions.Add(p.Position);
            }
            int index = PassTargeting.SelectReceiver(from.Position, aim, candidatePositions, config.passConeAngle);
            PlayerController target = index >= 0 ? candidates[index] : null;
            Vector3 point = target != null ? target.Position : from.Position + aim * config.passNoTargetDistance;

            Launch(FlightKind.Pass, from, target, point, config.passSpeed, 0f);
            return true;
        }

        public bool Shoot(PlayerController from, Vector2 stick)
        {
            if (Holder != from || IsFrozen) return false;

            // Sin stick al centro; con stick arriba/abajo hacia ese palo
            float lateral = Mathf.Clamp(stick.y, -1f, 1f) * (config.goalWidth * 0.5f - config.shotPostMargin);
            Vector3 point = pool.AttackedGoalCenter(from.Team) + Vector3.forward * lateral;
            Launch(FlightKind.Shot, from, null, point, config.shotSpeed, ShotOvershoot);
            return true;
        }

        // El portero ha ganado la tirada de parada: se queda la pelota
        public bool CatchByGoalkeeper(PlayerController goalkeeper)
        {
            if (State != BallState.InFlight || FlightKind != FlightKind.Shot || IsFrozen) return false;
            GiveTo(goalkeeper);
            return true;
        }

        // Tras un gol: la pelota se detiene donde está hasta la reanudación
        public void Freeze()
        {
            SetHolder(null);
            State = BallState.Free;
            StopAndHold();
            IsFrozen = true;
        }

        // Reanudación: el receptor la recibe en el centro; sin receptor queda libre en el centro
        public void PlaceForRestart(PlayerController receiver)
        {
            IsFrozen = false;
            if (receiver != null)
            {
                Vector3 facing = Vector3.right * receiver.Team.AttackSign();
                receiver.Teleport(pool.Center - facing * config.ballHoldDistance, facing);
                body.position = pool.Center;
                GiveTo(receiver);
                return;
            }
            SetHolder(null);
            State = BallState.Free;
            body.isKinematic = false;
            body.position = pool.Center;
            body.linearVelocity = Vector3.zero;
        }

        // ---------- Transiciones internas ----------

        void Launch(FlightKind kind, PlayerController from, PlayerController target, Vector3 point, float speed, float overshoot)
        {
            Vector3 start = pool.ClampInside(body.position, config.ballRadius);
            start.y = pool.WaterY;
            Vector3 direction = point - start;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = from.Forward;
            direction.y = 0f;
            direction.Normalize();

            SetHolder(null);
            State = BallState.InFlight;
            FlightId++;
            FlightKind = kind;
            FlightFrom = from;
            FlightTarget = target;
            LastToucher = from;
            releaser = from;
            releaserTimer = config.releaserCooldown;

            flightStart = start;
            flightDistance = FlatDistance(start, point) + overshoot;
            body.isKinematic = false;
            body.position = start;
            body.linearVelocity = direction * speed;
        }

        // Detiene la pelota y la pasa a cinemática (Unity avisa si se cambia la velocidad de un cuerpo cinemático)
        void StopAndHold()
        {
            if (!body.isKinematic) body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        void GiveTo(PlayerController player)
        {
            StopAndHold();
            State = BallState.Held;
            FlightFrom = null;
            FlightTarget = null;
            LastToucher = player;
            SetHolder(player);

            if (!hasPossessionTeam || possessionTeam != player.Team)
            {
                hasPossessionTeam = true;
                possessionTeam = player.Team;
                GameEvents.RaisePossessionChanged(player.Team);
            }
        }

        void BecomeFree()
        {
            State = BallState.Free;
            FlightFrom = null;
            FlightTarget = null;
        }

        void SetHolder(PlayerController player)
        {
            var previous = Holder;
            Holder = player;
            if (previous != player) HolderChanged?.Invoke(previous, player);
        }

        // En un pase lo recibe cualquier compañero del pasador que la toque (primero el receptor previsto)
        bool TryCatchPass()
        {
            var from = FlightFrom;
            if (FlightTarget != null && CanTouch(FlightTarget))
            {
                CompletePass(from, FlightTarget);
                return true;
            }
            foreach (var p in players)
            {
                if (p.Team != from.Team || p.IsGoalkeeper || !CanTouch(p)) continue;
                CompletePass(from, p);
                return true;
            }
            return false;
        }

        void CompletePass(PlayerController from, PlayerController to)
        {
            GiveTo(to);
            if (to != from) GameEvents.RaisePassCompleted(from.gameObject, to.gameObject);
        }

        // Pelota libre: la coge el jugador más cercano que la toque (también los porteros)
        void TryPickup()
        {
            PlayerController best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in players)
            {
                if (!CanTouch(p)) continue;
                float d = FlatDistance(p.Position, body.position);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }
            if (best != null) GiveTo(best);
        }

        bool CanTouch(PlayerController p)
        {
            if (!p.CanTakeBall) return false;
            if (p == releaser && releaserTimer > 0f) return false;
            return FlatDistance(p.Position, body.position) <= config.pickupRadius;
        }

        Vector3 HoldPoint(PlayerController owner)
        {
            Vector3 forward = owner.Forward;
            forward.y = 0f;
            Vector3 point = owner.Position + forward.normalized * config.ballHoldDistance;
            point.y = pool.WaterY;
            return point;
        }

        // Cualquier choque (pared, poste, red) corta el vuelo: la pelota rebota y queda libre
        void OnCollisionEnter(Collision collision)
        {
            if (State != BallState.InFlight) return;
            var touched = collision.collider.GetComponentInParent<PlayerController>();
            if (touched != null) LastToucher = touched;
            BecomeFree();
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}

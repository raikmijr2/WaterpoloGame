using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.AI
{
    // Coordina a un equipo. Al defender, uno se encarga del poseedor y los demás marcan cada uno
    // a un atacante distinto (nunca al poseedor). En modo agresivo (rival) además uno va a por la
    // pelota suelta y uno intenta cortar el pase. En modo pasivo (compañeros del humano) solo reparte marcajes
    public class TeamAI : MonoBehaviour
    {
        [SerializeField] TeamSide team;
        [SerializeField] BallController ball;
        [SerializeField] DifficultySO difficulty;
        [Tooltip("Agresivo = rival con IA completa. Pasivo = compañeros del humano: solo marcajes")]
        [SerializeField] bool aggressive = true;

        readonly List<PlayerController> fieldPlayers = new List<PlayerController>();
        readonly List<PlayerController> opponentFieldPlayers = new List<PlayerController>();
        readonly Dictionary<PlayerController, PlayerController> marks = new Dictionary<PlayerController, PlayerController>();
        readonly List<(float distance, PlayerController marker, PlayerController attacker)> pairs =
            new List<(float, PlayerController, PlayerController)>();
        readonly HashSet<PlayerController> markedAttackers = new HashSet<PlayerController>();
        float timer;

        public TeamSide Team => team;
        public bool IsAggressive => aggressive;
        public DifficultySO Difficulty => difficulty;
        public PlayerController Chaser { get; private set; }
        public PlayerController Presser { get; private set; }
        public PlayerController Interceptor { get; private set; }

        // Punto de la trayectoria del pase rival donde el interceptor intenta cortarlo
        public Vector3 InterceptPoint { get; private set; }

        public bool TryGetMark(PlayerController marker, out PlayerController attacker) => marks.TryGetValue(marker, out attacker);

        void Start()
        {
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (p.IsGoalkeeper) continue;
                if (p.Team == team) fieldPlayers.Add(p);
                else opponentFieldPlayers.Add(p);
            }
        }

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = difficulty.reactionTime;
            Assign();
        }

        void Assign()
        {
            Chaser = null;
            Presser = null;
            Interceptor = null;

            // Quién se encarga del poseedor rival (en modo pasivo suele ser el humano: no se le asigna marcaje)
            var holder = ball.Holder;
            if (holder != null && holder.Team != team && !holder.IsGoalkeeper) Presser = Nearest(holder.Position);

            if (aggressive)
            {
                if (ball.State == BallState.Free) Chaser = Nearest(ball.Position);
                else if (ball.State == BallState.InFlight && ball.FlightKind == FlightKind.Pass &&
                         ball.FlightFrom != null && ball.FlightFrom.Team != team)
                    AssignInterceptor();
            }

            AssignMarks();
        }

        // Solo marca el equipo que defiende (el rival tuvo la pelota por última vez). Un marcador por atacante,
        // empezando por las parejas más cercanas; el poseedor no se marca (ya va el presionador)
        void AssignMarks()
        {
            marks.Clear();
            if (!ball.HasPossession(team.Opponent())) return;

            pairs.Clear();
            foreach (var marker in fieldPlayers)
            {
                if (marker == Presser || marker == Chaser || marker == Interceptor) continue;
                foreach (var attacker in opponentFieldPlayers)
                {
                    if (attacker == ball.Holder) continue;
                    pairs.Add((Vector3.Distance(marker.Position, attacker.Position), marker, attacker));
                }
            }
            pairs.Sort((a, b) => a.distance.CompareTo(b.distance));

            markedAttackers.Clear();
            foreach (var pair in pairs)
            {
                if (marks.ContainsKey(pair.marker) || markedAttackers.Contains(pair.attacker)) continue;
                marks[pair.marker] = pair.attacker;
                markedAttackers.Add(pair.attacker);
            }
        }

        // El jugador más cercano a la línea del pase va al punto más próximo de esa línea
        void AssignInterceptor()
        {
            Vector3 from = ball.Position;
            Vector3 dir = ball.Velocity;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            float best = float.MaxValue;
            foreach (var p in fieldPlayers)
            {
                Vector3 to = p.Position - from;
                float along = Mathf.Max(0f, Vector3.Dot(to, dir));
                Vector3 point = from + dir * along;
                float d = Vector3.Distance(new Vector3(p.Position.x, 0f, p.Position.z), new Vector3(point.x, 0f, point.z));
                if (d < best)
                {
                    best = d;
                    Interceptor = p;
                    InterceptPoint = point;
                }
            }
        }

        PlayerController Nearest(Vector3 point)
        {
            PlayerController best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in fieldPlayers)
            {
                float d = Vector3.Distance(p.Position, point);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }
            return best;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.AI
{
    // Coordina a un equipo. Al defender, uno se encarga del poseedor y los demás marcan cada uno
    // a un atacante distinto (nunca al poseedor). En modo agresivo (rival) además uno va a por la
    // pelota suelta, uno intenta cortar el pase y, cerca de su portería, un segundo defensor hace presión doble.
    // En modo pasivo (compañeros del humano) solo reparte marcajes.
    // Al atacar (los dos modos) reparte los huecos del triángulo (punta y suport) entre los dos compañeros del
    // poseedor y cuenta los pases de la posesión
    public class TeamAI : MonoBehaviour
    {
        [SerializeField] TeamSide team;
        [SerializeField] BallController ball;
        [Tooltip("Niveles de dificultad en el orden del menú (Fàcil, Difícil): se usa el que se eligió (PlayerSession)")]
        [SerializeField] DifficultySO[] levels;
        [Tooltip("Agresivo = rival con IA completa. Pasivo = compañeros del humano: solo marcajes")]
        [SerializeField] bool aggressive = true;
        [SerializeField] GameConfigSO config;
        [SerializeField] PoolBounds pool;

        readonly List<PlayerController> fieldPlayers = new List<PlayerController>();
        readonly List<PlayerController> opponentFieldPlayers = new List<PlayerController>();
        readonly Dictionary<PlayerController, PlayerController> marks = new Dictionary<PlayerController, PlayerController>();
        readonly List<(float distance, PlayerController marker, PlayerController attacker)> pairs =
            new List<(float, PlayerController, PlayerController)>();
        readonly HashSet<PlayerController> markedAttackers = new HashSet<PlayerController>();
        readonly Dictionary<PlayerController, SupportSlot> supportSlots = new Dictionary<PlayerController, SupportSlot>();
        readonly List<PlayerController> supporters = new List<PlayerController>();
        float timer;

        public TeamSide Team => team;
        public bool IsAggressive => aggressive;
        public DifficultySO Difficulty => levels[Mathf.Clamp(PlayerSession.DifficultyIndex, 0, levels.Length - 1)];
        public PlayerController Chaser { get; private set; }
        public PlayerController Presser { get; private set; }
        public PlayerController Interceptor { get; private set; }
        public PlayerController SecondPresser { get; private set; }

        // Pases completados por este equipo en la posesión actual
        public int PassesThisPossession { get; private set; }

        // Hueco del triángulo que ocupa este jugador cuando su equipo ataca (no el poseedor)
        public bool TryGetSupportSlot(PlayerController player, out SupportSlot slot) => supportSlots.TryGetValue(player, out slot);

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

        void OnEnable()
        {
            GameEvents.PassCompleted += OnPassCompleted;
            GameEvents.PossessionChanged += OnPossessionChanged;
            GameEvents.Goal += OnGoal;
        }

        void OnDisable()
        {
            GameEvents.PassCompleted -= OnPassCompleted;
            GameEvents.PossessionChanged -= OnPossessionChanged;
            GameEvents.Goal -= OnGoal;
        }

        void OnPassCompleted(GameObject from, GameObject to)
        {
            var passer = from != null ? from.GetComponent<PlayerController>() : null;
            if (passer != null && passer.Team == team) PassesThisPossession++;
        }

        void OnPossessionChanged(TeamSide owner) => PassesThisPossession = 0;
        void OnGoal(TeamSide scorer, GameObject player) => PassesThisPossession = 0;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Difficulty.reactionTime;
            Assign();
        }

        void Assign()
        {
            Chaser = null;
            Presser = null;
            Interceptor = null;
            SecondPresser = null;

            // Quién se encarga del poseedor rival (en modo pasivo suele ser el humano: no se le asigna marcaje)
            var holder = ball.Holder;
            if (holder != null && holder.Team != team && !holder.IsGoalkeeper)
            {
                Presser = Nearest(holder.Position);
                // Presión doble cerca de la portería propia: obliga a pasar en vez de ir recto a tirar
                if (aggressive && Flat(holder.Position - pool.AttackedGoalCenter(team.Opponent())).magnitude <= config.aiDoublePressRange)
                    SecondPresser = Nearest(holder.Position, Presser);
            }

            if (aggressive)
            {
                if (ball.State == BallState.Free) Chaser = Nearest(ball.Position);
                else if (ball.State == BallState.InFlight && ball.FlightKind == FlightKind.Pass &&
                         ball.FlightFrom != null && ball.FlightFrom.Team != team)
                    AssignInterceptor();
            }

            AssignMarks();
            AssignSupport(holder);
        }

        // Los dos compañeros del poseedor se reparten punta y suport: la combinación que menos les hace nadar,
        // y solo cambian si la otra es claramente mejor (si no, se cruzarían a cada momento)
        void AssignSupport(PlayerController holder)
        {
            supporters.Clear();
            if (holder != null && holder.Team == team && !holder.IsGoalkeeper)
                foreach (var p in fieldPlayers) if (p != holder) supporters.Add(p);
            if (supporters.Count == 0)
            {
                supportSlots.Clear();
                return;
            }
            if (supporters.Count == 1)
            {
                supportSlots.Clear();
                supportSlots[supporters[0]] = SupportSlot.Point;
                return;
            }

            var pitch = Pitch();
            Vector3 point = SupportSpots.SlotPosition(SupportSlot.Point, holder.Position, team, pitch, config);
            Vector3 wing = SupportSpots.SlotPosition(SupportSlot.Wing, holder.Position, team, pitch, config);
            var a = supporters[0];
            var b = supporters[1];
            float straight = Flat(a.Position - point).magnitude + Flat(b.Position - wing).magnitude;   // a punta, b suport
            float crossed = Flat(a.Position - wing).magnitude + Flat(b.Position - point).magnitude;

            bool keepCurrent = supportSlots.TryGetValue(a, out var aSlot) && supportSlots.ContainsKey(b);
            bool aIsPoint = keepCurrent ? aSlot == SupportSlot.Point : straight <= crossed;
            float current = aIsPoint ? straight : crossed;
            float other = aIsPoint ? crossed : straight;
            if (other + config.aiSupportSwitchMargin < current) aIsPoint = !aIsPoint;

            supportSlots.Clear();
            supportSlots[a] = aIsPoint ? SupportSlot.Point : SupportSlot.Wing;
            supportSlots[b] = aIsPoint ? SupportSlot.Wing : SupportSlot.Point;
        }

        RolePositioning.Pitch Pitch() => new RolePositioning.Pitch { Center = pool.Center, HalfLength = pool.HalfLength, HalfWidth = pool.HalfWidth };

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // Solo marca el equipo que defiende (el rival tuvo la pelota por última vez). Un marcador por atacante,
        // empezando por las parejas más cercanas; el poseedor no se marca (ya va el presionador)
        void AssignMarks()
        {
            marks.Clear();
            if (!ball.HasPossession(team.Opponent())) return;

            pairs.Clear();
            foreach (var marker in fieldPlayers)
            {
                if (marker == Presser || marker == SecondPresser || marker == Chaser || marker == Interceptor) continue;
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

        PlayerController Nearest(Vector3 point, PlayerController except = null)
        {
            PlayerController best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in fieldPlayers)
            {
                if (p == except) continue;
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

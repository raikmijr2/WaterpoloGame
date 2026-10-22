using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Brains
{
    // Control estilo FIFA: el HumanBrain pasa al jugador de campo del equipo humano que recibe la pelota.
    // Al defender, salta al jugador más cercano a la pelota (con margen y espera para no cambiar constantemente).
    // Con Q (LT en el mando) se cambia a mano al más cercano a la pelota; si ya eres tú, al siguiente.
    // Tras un cambio manual el automático espera un poco para no deshacerlo
    public class ControlSwitcher : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] HumanBrain human;
        [SerializeField] BallController ball;
        [SerializeField] TeamSide humanTeam = TeamSide.Home;
        [Tooltip("Indicador del jugador controlado (se coloca bajo sus pies)")]
        [SerializeField] Transform marker;

        readonly List<PlayerController> team = new List<PlayerController>();
        float switchCooldown;
        float manualLockTimer;
        InputAction switchAction;

        public PlayerController Controlled { get; private set; }

        void Awake()
        {
            switchAction = new InputAction("SwitchPlayer", InputActionType.Button);
            switchAction.AddBinding("<Keyboard>/q");
            switchAction.AddBinding("<Gamepad>/leftTrigger");
        }

        void OnEnable()
        {
            ball.HolderChanged += OnHolderChanged;
            switchAction.Enable();
        }

        void OnDisable()
        {
            ball.HolderChanged -= OnHolderChanged;
            switchAction.Disable();
        }

        void OnDestroy() => switchAction.Dispose();

        void Start()
        {
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (p.Team == humanTeam && !p.IsGoalkeeper) team.Add(p);
            team.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (team.Count > 0) SwitchTo(NearestToBall(null));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (switchCooldown > 0f) switchCooldown -= dt;
            if (manualLockTimer > 0f) manualLockTimer -= dt;
            if (Controlled == null) return;

            // Cambio manual: no con la pelota (se quedaría en manos de la IA)
            if (switchAction.WasPressedThisFrame() && !Controlled.HasBall)
            {
                var next = NearestToBall(Controlled);
                if (next != null)
                {
                    SwitchTo(next);
                    manualLockTimer = config.manualSwitchLock;
                }
                return;
            }

            // Cambio automático al defender
            if (switchCooldown > 0f || manualLockTimer > 0f || !IsDefending()) return;
            var nearest = NearestToBall(null);
            if (nearest == null || nearest == Controlled) return;
            float current = Vector3.Distance(Controlled.Position, ball.Position);
            float candidate = Vector3.Distance(nearest.Position, ball.Position);
            if (current - candidate >= config.autoSwitchMargin) SwitchTo(nearest);
        }

        // Defiende si la pelota no es suya: la tiene un rival, está libre o va en un pase o tiro rival.
        // Un pase propio en vuelo no cuenta: el control pasará al receptor al recibirla
        bool IsDefending()
        {
            if (ball.Holder != null) return ball.Holder.Team != humanTeam;
            if (ball.State == BallState.InFlight) return ball.FlightFrom == null || ball.FlightFrom.Team != humanTeam;
            return true;
        }

        void LateUpdate()
        {
            if (marker == null || Controlled == null) return;
            Vector3 p = Controlled.Position;
            marker.position = new Vector3(p.x, marker.position.y, p.z);
        }

        void OnHolderChanged(PlayerController previous, PlayerController next)
        {
            if (next == null || next == Controlled) return;
            if (next.Team == humanTeam && !next.IsGoalkeeper) SwitchTo(next);
        }

        void SwitchTo(PlayerController player)
        {
            if (Controlled != null) Controlled.ResetBrain();
            Controlled = player;
            player.SetBrain(human);
            GameEvents.RaiseControlledPlayerChanged(player.gameObject);
            switchCooldown = config.autoSwitchCooldown;
        }

        // Jugador de campo propio más cercano a la pelota, sin contar a "exclude"
        PlayerController NearestToBall(PlayerController exclude)
        {
            PlayerController best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in team)
            {
                if (p == exclude) continue;
                float d = Vector3.Distance(p.Position, ball.Position);
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

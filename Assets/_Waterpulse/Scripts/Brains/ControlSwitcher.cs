using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Brains
{
    // Control estilo FIFA: el HumanBrain pasa al jugador de campo del equipo humano que recibe la pelota.
    // Al defender, salta al jugador más cercano a la pelota (con margen y espera para no cambiar constantemente)
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

        public PlayerController Controlled { get; private set; }

        void OnEnable() => ball.HolderChanged += OnHolderChanged;
        void OnDisable() => ball.HolderChanged -= OnHolderChanged;

        void Start()
        {
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (p.Team == humanTeam && !p.IsGoalkeeper) team.Add(p);
            team.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (team.Count > 0) SwitchTo(NearestToBall());
        }

        void Update()
        {
            if (switchCooldown > 0f) switchCooldown -= Time.deltaTime;
            if (Controlled == null || switchCooldown > 0f || !IsDefending()) return;

            var nearest = NearestToBall();
            if (nearest == Controlled) return;
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
            switchCooldown = config.autoSwitchCooldown;
        }

        PlayerController NearestToBall()
        {
            PlayerController best = team[0];
            float bestDistance = float.MaxValue;
            foreach (var p in team)
            {
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

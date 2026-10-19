using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Brains
{
    // Control estilo FIFA: el HumanBrain pasa al jugador de campo del equipo humano que recibe la pelota
    public class ControlSwitcher : MonoBehaviour
    {
        [SerializeField] HumanBrain human;
        [SerializeField] BallController ball;
        [SerializeField] TeamSide humanTeam = TeamSide.Home;
        [Tooltip("Indicador del jugador controlado (se coloca bajo sus pies)")]
        [SerializeField] Transform marker;

        readonly List<PlayerController> team = new List<PlayerController>();

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

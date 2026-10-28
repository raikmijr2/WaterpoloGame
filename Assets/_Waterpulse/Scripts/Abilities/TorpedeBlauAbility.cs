using UnityEngine;
using Waterpulse.Config;

namespace Waterpulse.Abilities
{
    // Torpede Blau: acelerón explosivo en la dirección del stick (o hacia donde mira).
    // La dirección queda fija: mal dirigido, aleja al jugador de la jugada. Solo sin la pelota: con ella, L es el Xut del Tràngol
    public class TorpedeBlauAbility : AbilityBehaviour
    {
        [Tooltip("Estela del jugador durante el acelerón")]
        [SerializeField] TrailRenderer trail;

        protected override AbilityDefinitionSO Definition => Player.Boost;
        protected override bool CanActivate() => !Player.HasBall && !Player.IsBoosting;

        protected override void Activate()
        {
            Player.StartBoost(Player.AimDirection(), Definition.power, Definition.duration);
        }

        void Update()
        {
            if (trail != null && trail.emitting != Player.IsBoosting) trail.emitting = Player.IsBoosting;
        }
    }
}

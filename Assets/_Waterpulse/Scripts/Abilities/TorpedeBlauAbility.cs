using UnityEngine;

namespace Waterpulse.Abilities
{
    // Torpede Blau: acelerón explosivo en la dirección del stick (o hacia donde mira).
    // La dirección queda fija: mal dirigido, aleja al jugador de la jugada
    public class TorpedeBlauAbility : AbilityBehaviour
    {
        [Tooltip("Estela del jugador durante el acelerón")]
        [SerializeField] TrailRenderer trail;

        protected override bool CanActivate() => !Player.IsBoosting;

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

using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Gameplay;

namespace Waterpulse.Abilities
{
    // Mur d'Aigua (porteros): con la energía llena (100) levanta un muro de agua delante de su portería que para todos
    // los tiros mientras dura. La energía del portero se recarga sola, despacio (EnergyConfig del portero)
    public class MurAiguaAbility : AbilityBehaviour
    {
        [Tooltip("Muro de la portería que defiende este portero")]
        [SerializeField] WaterWall wall;

        protected override AbilityDefinitionSO Definition => Player.Wall;
        protected override bool CanActivate() => !wall.IsUp;
        protected override void Activate() => wall.Raise(Definition.duration);
    }
}

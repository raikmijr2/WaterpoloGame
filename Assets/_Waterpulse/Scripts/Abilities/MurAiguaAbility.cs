using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Gameplay;

namespace Waterpulse.Abilities
{
    // Mur de Ferro (porteros; antes Mur d'Aigua): con la energía llena (100) deja caer un muro de metal delante de su
    // portería que para todos los tiros durante 5 s y, al caer, empuja fuera del área a los rivales de campo (WaterWall).
    // La energía del portero se recarga sola, despacio (EnergyConfig del portero)
    public class MurAiguaAbility : AbilityBehaviour
    {
        [Tooltip("Muro de la portería que defiende este portero")]
        [SerializeField] WaterWall wall;

        protected override AbilityDefinitionSO Definition => Player.Wall;
        protected override bool CanActivate() => !wall.IsUp;
        protected override void Activate() => wall.Raise(Definition.duration);
    }
}

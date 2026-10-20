using UnityEngine;

namespace Waterpulse.Config
{
    // Tipo de habilidad: decide qué componente la ejecuta y cómo la usa la IA
    public enum AbilityKind { PowerShot, Boost }

    // Datos de una habilidad especial (GDD sección 4). No todos los campos se usan en todos los tipos
    [CreateAssetMenu(menuName = "Waterpulse/Ability", fileName = "Ability")]
    public class AbilityDefinitionSO : ScriptableObject
    {
        public AbilityKind kind;
        [Tooltip("Nombre que aparece en pantalla al activarla (catalán)")]
        public string displayName = "";
        public float cost = 25f;

        [Tooltip("Multiplicador: de la velocidad del tiro (PowerShot) o de la de nado (Boost)")]
        public float power = 2f;
        [Tooltip("Boost: duración del acelerón (s)")]
        public float duration = 1f;
        [Tooltip("PowerShot: distancia al portero a la que la parada llega a 0 (m). Pequeña = solo lo para si le llega al cuerpo")]
        public float goalkeeperReach = 0.6f;
    }
}

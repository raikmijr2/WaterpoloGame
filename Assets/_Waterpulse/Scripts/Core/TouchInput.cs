using UnityEngine;

namespace Waterpulse.Core
{
    public enum TouchButton { Pass, Shoot, Ability, Dash, Switch, Keeper }

    // Lo que piden los controles táctiles (TouchControls, en UI). Lo leen HumanBrain y ControlSwitcher junto al teclado
    // y el mando, así el juego no sabe si se juega con el dedo. Los botones de la derecha cambian de función según la
    // jugada, pero aquí solo llega la acción final (pasar, tirar, habilidad…)
    public static class TouchInput
    {
        // Lo que espera una pulsación a que alguien la lea antes de perderse (s, tiempo real): así no se ejecuta
        // tarde una pulsación hecha cuando nadie podía usarla (en un saque, por ejemplo)
        const float PressLifetime = 0.15f;
        static readonly float[] pressedAt = new float[6];

        // Joystick táctil (0 si no se toca)
        public static Vector2 Move { get; set; }

        public static void Press(TouchButton button) => pressedAt[(int)button] = Time.unscaledTime;

        // ¿Se ha pulsado hace poco? Si sí, la pulsación se gasta (solo la usa uno)
        public static bool Consume(TouchButton button)
        {
            int i = (int)button;
            if (Time.unscaledTime - pressedAt[i] > PressLifetime) return false;
            pressedAt[i] = float.NegativeInfinity;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Move = Vector2.zero;
            for (int i = 0; i < pressedAt.Length; i++) pressedAt[i] = float.NegativeInfinity;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Waterpulse.Core;

namespace Waterpulse.Brains
{
    // Cerebro del jugador humano: WASD o stick izquierdo (el joystick táctil de la Fase 8 emula ese stick)
    public class HumanBrain : MonoBehaviour, IPlayerBrain
    {
        InputAction move;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");
        }

        void OnEnable() => move.Enable();
        void OnDisable() => move.Disable();
        void OnDestroy() => move.Dispose();

        public PlayerIntent GetIntent()
        {
            return new PlayerIntent { Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) };
        }
    }
}

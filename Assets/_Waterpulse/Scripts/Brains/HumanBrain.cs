using UnityEngine;
using UnityEngine.InputSystem;
using Waterpulse.Core;

namespace Waterpulse.Brains
{
    // Input del jugador humano. ControlSwitcher lo asigna al jugador que controla en cada momento.
    // El mando (stick izquierdo, A, B) queda enlazado para los controles táctiles de la Fase 8
    public class HumanBrain : MonoBehaviour, IPlayerBrain
    {
        InputAction move;
        InputAction pass;
        InputAction shoot;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            pass = new InputAction("Pass", InputActionType.Button);
            pass.AddBinding("<Keyboard>/j");
            pass.AddBinding("<Gamepad>/buttonSouth");

            shoot = new InputAction("Shoot", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/k");
            shoot.AddBinding("<Gamepad>/buttonEast");
        }

        void OnEnable()
        {
            move.Enable();
            pass.Enable();
            shoot.Enable();
        }

        void OnDisable()
        {
            move.Disable();
            pass.Disable();
            shoot.Disable();
        }

        void OnDestroy()
        {
            move.Dispose();
            pass.Dispose();
            shoot.Dispose();
        }

        public PlayerIntent GetIntent()
        {
            return new PlayerIntent
            {
                Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f),
                Pass = pass.WasPressedThisFrame(),
                Shoot = shoot.WasPressedThisFrame(),
            };
        }
    }
}

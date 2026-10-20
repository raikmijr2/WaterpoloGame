using UnityEngine;
using UnityEngine.InputSystem;
using Waterpulse.Core;

namespace Waterpulse.Brains
{
    // Input del jugador humano. ControlSwitcher lo asigna al jugador que controla en cada momento.
    // El mando (stick izquierdo, A, B, Y) queda enlazado para los controles táctiles de la Fase 8
    public class HumanBrain : MonoBehaviour, IPlayerBrain
    {
        InputAction move;
        InputAction pass;
        InputAction shoot;
        InputAction ability;

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

            ability = new InputAction("Ability", InputActionType.Button);
            ability.AddBinding("<Keyboard>/l");
            ability.AddBinding("<Gamepad>/buttonNorth");
        }

        void OnEnable()
        {
            move.Enable();
            pass.Enable();
            shoot.Enable();
            ability.Enable();
        }

        void OnDisable()
        {
            move.Disable();
            pass.Disable();
            shoot.Disable();
            ability.Disable();
        }

        void OnDestroy()
        {
            move.Dispose();
            pass.Dispose();
            shoot.Dispose();
            ability.Dispose();
        }

        public PlayerIntent GetIntent()
        {
            return new PlayerIntent
            {
                Move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f),
                Pass = pass.WasPressedThisFrame(),
                Shoot = shoot.WasPressedThisFrame(),
                Ability = ability.WasPressedThisFrame(),
            };
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Brains
{
    // Input del jugador humano. ControlSwitcher lo asigna al jugador que controla en cada momento.
    // Teclado, mando (stick izquierdo, A, B, Y, RB, LB) y controles táctiles (TouchInput), los tres a la vez.
    // Nadar lento: con Espacio (o LB) el stick se escala; en táctil basta con inclinar poco el joystick
    public class HumanBrain : MonoBehaviour, IPlayerBrain
    {
        [SerializeField] GameConfigSO config;

        InputAction move;
        InputAction pass;
        InputAction shoot;
        InputAction ability;
        InputAction dash;
        InputAction slow;

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

            dash = new InputAction("Dash", InputActionType.Button);
            dash.AddBinding("<Keyboard>/leftShift");
            dash.AddBinding("<Keyboard>/rightShift");
            dash.AddBinding("<Gamepad>/rightShoulder");

            slow = new InputAction("Slow", InputActionType.Button);
            slow.AddBinding("<Keyboard>/space");
            slow.AddBinding("<Gamepad>/leftShoulder");
        }

        void OnEnable()
        {
            move.Enable();
            pass.Enable();
            shoot.Enable();
            ability.Enable();
            dash.Enable();
            slow.Enable();
        }

        void OnDisable()
        {
            move.Disable();
            pass.Disable();
            shoot.Disable();
            ability.Disable();
            dash.Disable();
            slow.Disable();
        }

        void OnDestroy()
        {
            move.Dispose();
            pass.Dispose();
            shoot.Dispose();
            ability.Dispose();
            dash.Dispose();
            slow.Dispose();
        }

        public PlayerIntent GetIntent()
        {
            Vector2 stick = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f);
            if (slow.IsPressed()) stick *= config.slowSwimMultiplier;
            // El joystick táctil manda si se está tocando más que el teclado o el mando
            Vector2 touch = Vector2.ClampMagnitude(TouchInput.Move, 1f);
            if (touch.sqrMagnitude > stick.sqrMagnitude) stick = touch;
            return new PlayerIntent
            {
                Move = stick,
                Pass = pass.WasPressedThisFrame() | TouchInput.Consume(TouchButton.Pass),
                Shoot = shoot.WasPressedThisFrame() | TouchInput.Consume(TouchButton.Shoot),
                Ability = ability.WasPressedThisFrame() | TouchInput.Consume(TouchButton.Ability),
                Dash = dash.WasPressedThisFrame() | TouchInput.Consume(TouchButton.Dash),
            };
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Waterpulse.Config;

namespace Waterpulse.UI
{
    // Intro del menú: el logo aparece con un fundido y después parpadea "Prem qualsevol tecla".
    // Cualquier tecla, clic, toque o botón del mando la termina, también durante el fundido
    public class IntroScreen : MonoBehaviour
    {
        [SerializeField] MenuConfigSO config;
        [SerializeField] CanvasGroup logo;
        [SerializeField] CanvasGroup prompt;

        public event Action Finished;

        InputAction anyKey;
        float startedAt;

        void Awake()
        {
            anyKey = new InputAction("AnyKey", InputActionType.Button);
            anyKey.AddBinding("<Keyboard>/anyKey");
            anyKey.AddBinding("<Pointer>/press");   // ratón, táctil y lápiz
            anyKey.AddBinding("<Gamepad>/buttonSouth");
            anyKey.AddBinding("<Gamepad>/buttonEast");
            anyKey.AddBinding("<Gamepad>/buttonNorth");
            anyKey.AddBinding("<Gamepad>/buttonWest");
            anyKey.AddBinding("<Gamepad>/start");
        }

        void OnEnable()
        {
            startedAt = Time.unscaledTime;
            logo.alpha = 0f;
            prompt.alpha = 0f;
            anyKey.Enable();
        }

        void OnDisable() => anyKey.Disable();
        void OnDestroy() => anyKey.Dispose();

        void Update()
        {
            float elapsed = Time.unscaledTime - startedAt;
            logo.alpha = config.logoFadeDuration > 0f ? Mathf.Clamp01(elapsed / config.logoFadeDuration) : 1f;

            // El aviso aparece cuando el logo ya se ve entero y late suavemente: 0 → 1 → 0 en cada periodo
            float blink = elapsed - config.logoFadeDuration;
            if (blink < 0f) prompt.alpha = 0f;
            else prompt.alpha = config.promptBlinkPeriod > 0f ? 0.5f - 0.5f * Mathf.Cos(blink * 2f * Mathf.PI / config.promptBlinkPeriod) : 1f;

            if (anyKey.WasPressedThisFrame()) Finished?.Invoke();
        }
    }
}

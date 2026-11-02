using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Guía de controles abajo en el centro, sin tutorial: solo las teclas que sirven ahora mismo, según lo que haces
    // (con la pelota, cargando el tiro, defendiendo o con el portero). Solo en los niveles que la piden (Fàcil).
    // En pantallas táctiles no sale: ya están los botones
    public class ControlHints : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text label;
        [Tooltip("Niveles de dificultad (Fàcil, Difícil): la guía sale si el elegido tiene showControlHints")]
        [SerializeField] DifficultySO[] levels;
        [Tooltip("Color de las teclas dentro del texto")]
        [SerializeField] Color keyColor = new Color(0.83f, 1f, 0.25f);
        [SerializeField] string withBall = "{J} PASSAR   {K} XUTAR   {L} SÚPER XUT   {Shift} DASH";
        [SerializeField] string charging = "{W/S} APUNTAR   {K} XUTAR";
        [SerializeField] string defending = "{J} ROBAR   {L} TORPEDE   {Shift} DASH   {Q} CANVIAR   {E} PORTER";
        [SerializeField] string keeper = "{L} MUR D'AIGUA   {J} PASSAR   {E} TORNAR AL CAMP";

        PlayerController controlled;
        bool playing;
        string keyHex;

        void Awake()
        {
            keyHex = ColorUtility.ToHtmlStringRGB(keyColor);
            panel.SetActive(false);
        }

        void OnEnable()
        {
            GameEvents.ControlledPlayerChanged += OnControlledChanged;
            GameEvents.MatchStateChanged += OnMatchStateChanged;
        }

        void OnDisable()
        {
            GameEvents.ControlledPlayerChanged -= OnControlledChanged;
            GameEvents.MatchStateChanged -= OnMatchStateChanged;
        }

        void OnControlledChanged(GameObject go) => controlled = go != null ? go.GetComponent<PlayerController>() : null;
        void OnMatchStateChanged(MatchState state) => playing = state == MatchState.Playing;

        void Update()
        {
            bool show = playing && controlled != null && !TouchControls.IsTouchDevice && Level.showControlHints;
            if (panel.activeSelf != show) panel.SetActive(show);
            if (!show) return;

            string hint = controlled.IsGoalkeeper ? keeper
                        : controlled.IsChargingShot ? charging
                        : controlled.HasBall ? withBall
                        : defending;
            string text = Format(hint);
            if (label.text != text) label.text = text;
        }

        DifficultySO Level => levels[Mathf.Clamp(PlayerSession.DifficultyIndex, 0, levels.Length - 1)];

        // {X} -> la tecla X en el color de las teclas
        string Format(string hint) => hint.Replace("{", $"<color=#{keyHex}>").Replace("}", "</color>");
    }
}

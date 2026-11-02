using UnityEngine;
using UnityEngine.InputSystem;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Controles táctiles (móvil y tableta), según la guía de botones del equipo: joystick dinámico a la izquierda
    // (TouchJoystick) y tres botones grandes a la derecha que cambian de función según la jugada:
    //   Atacando (con la pelota): PASSA · XUTA · DASH. Doble toque a XUTA = súper xut (Xut del Tràngol).
    //   Defendiendo:              CANVI · PORTER · ROBA. Doble toque a ROBA = Torpede Blau. DASH pequeño encima.
    //   Con el portero:           CANVI · CAMP (volver al de campo) · MUR (el botón de robar levanta el muro).
    // Cargando el tiro, XUTA suelta al momento y el joystick apunta. Las pulsaciones van a TouchInput, que leen
    // HumanBrain y ControlSwitcher: el juego no cambia. Solo se ven en móvil y tableta (o forzado en el editor),
    // y si el móvil está en vertical se pide girarlo
    public class TouchControls : MonoBehaviour
    {
        public const string ForceInEditorKey = "waterpulse.forceTouchControls";

        [SerializeField] BallController ball;
        [Tooltip("Grupo con todos los controles (apagado en la escena)")]
        [SerializeField] GameObject controls;
        [SerializeField] TouchActionButton first;
        [SerializeField] TouchActionButton second;
        [SerializeField] TouchActionButton third;
        [Tooltip("DASH pequeño: solo defendiendo (atacando, el DASH es el tercer botón)")]
        [SerializeField] TouchActionButton dash;
        [Tooltip("Aviso a pantalla completa para girar el móvil a horizontal")]
        [SerializeField] GameObject rotateNotice;
        [Tooltip("Tiempo para el segundo toque del doble toque (s, tiempo real). XUTA espera esto antes de empezar a cargar")]
        [SerializeField] float doubleTapWindow = 0.25f;

        enum Mode { Attack, Defense, Keeper }

        PlayerController controlled;
        Mode mode;
        float shootTapAt = float.NegativeInfinity;   // primer toque de XUTA esperando un posible segundo
        bool shootPending;
        float stealTapAt = float.NegativeInfinity;

        // Móvil o tableta: el navegador lo dice, y los iPad (que dicen ser un Mac) se reconocen por tener pantalla
        // táctil y ningún ratón. En el editor se puede forzar (Waterpulse/Forçar controls de mòbil)
        public static bool IsTouchDevice =>
            Application.isMobilePlatform
            || (Touchscreen.current != null && Mouse.current == null)
            || (Application.isEditor && PlayerPrefs.GetInt(ForceInEditorKey, 0) == 1);

        void Awake()
        {
            first.Pressed += OnFirst;
            second.Pressed += OnSecond;
            third.Pressed += OnThird;
            dash.Pressed += () => TouchInput.Press(TouchButton.Dash);
        }

        void Start()
        {
            controls.SetActive(IsTouchDevice);
            rotateNotice.SetActive(false);
        }

        void OnEnable() => GameEvents.ControlledPlayerChanged += OnControlledChanged;
        void OnDisable() => GameEvents.ControlledPlayerChanged -= OnControlledChanged;

        void OnControlledChanged(GameObject go) => controlled = go != null ? go.GetComponent<PlayerController>() : null;

        void Update()
        {
            if (!controls.activeSelf) return;
            bool portrait = Screen.height > Screen.width;
            if (rotateNotice.activeSelf != portrait) rotateNotice.SetActive(portrait);

            // XUTA sin segundo toque: empieza a cargar el tiro
            if (shootPending && Time.unscaledTime - shootTapAt > doubleTapWindow)
            {
                shootPending = false;
                TouchInput.Press(TouchButton.Shoot);
            }

            if (controlled == null) return;
            mode = controlled.IsGoalkeeper ? Mode.Keeper : controlled.HasBall ? Mode.Attack : Mode.Defense;
            switch (mode)
            {
                case Mode.Attack:
                    first.SetText("PASSA");
                    second.SetText(controlled.IsChargingShot ? "XUTA!" : "XUTA");
                    third.SetText("DASH");
                    break;
                case Mode.Defense:
                    first.SetText("CANVI");
                    second.SetText("PORTER");
                    third.SetText("ROBA");
                    break;
                default:
                    first.SetText(controlled.HasBall ? "PASSA" : "CANVI");
                    second.SetText("CAMP");
                    third.SetText("MUR");
                    break;
            }
            bool showDash = mode == Mode.Defense;
            if (dash.gameObject.activeSelf != showDash) dash.gameObject.SetActive(showDash);
        }

        void OnFirst()
        {
            bool pass = mode == Mode.Attack || (mode == Mode.Keeper && controlled != null && controlled.HasBall);
            TouchInput.Press(pass ? TouchButton.Pass : TouchButton.Switch);
        }

        void OnSecond()
        {
            if (mode != Mode.Attack)
            {
                TouchInput.Press(TouchButton.Keeper);   // PORTER, o CAMP para volver
                return;
            }
            // Cargando: suelta ya. Si no: un toque carga el tiro (tras esperar el posible segundo), dos = súper xut
            if (controlled != null && controlled.IsChargingShot)
            {
                TouchInput.Press(TouchButton.Shoot);
                return;
            }
            if (shootPending && Time.unscaledTime - shootTapAt <= doubleTapWindow)
            {
                shootPending = false;
                TouchInput.Press(TouchButton.Ability);
                return;
            }
            shootPending = true;
            shootTapAt = Time.unscaledTime;
        }

        void OnThird()
        {
            switch (mode)
            {
                case Mode.Attack: TouchInput.Press(TouchButton.Dash); break;
                case Mode.Keeper: TouchInput.Press(TouchButton.Ability); break;   // MUR
                default:
                    // ROBA al momento (el robo no puede esperar); un segundo toque rápido lanza el Torpede
                    bool doubleTap = Time.unscaledTime - stealTapAt <= doubleTapWindow;
                    stealTapAt = doubleTap ? float.NegativeInfinity : Time.unscaledTime;
                    TouchInput.Press(doubleTap ? TouchButton.Ability : TouchButton.Pass);
                    break;
            }
        }
    }
}

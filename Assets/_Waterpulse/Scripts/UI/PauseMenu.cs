using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Menú de pausa del partido: se abre con Esc, P, Start o el botón II (no con el partido acabado).
    // Continuar · Opcions (volumen, pantalla completa, controles) · Reiniciar partit · Menú principal, estos dos con
    // confirmación. Esc o B vuelven atrás (desde la principal, continúan); P o Start cierran la pausa desde cualquier pantalla
    public class PauseMenu : MonoBehaviour
    {
        enum Page { Root, Options, Controls, Confirm }
        enum Pending { Restart, Menu }

        [SerializeField] MenuConfigSO config;
        [SerializeField] MatchPause pause;
        [SerializeField] MatchFlow match;
        [Tooltip("Fondo oscuro con todas las pantallas de la pausa")]
        [SerializeField] GameObject overlay;
        [Tooltip("Botón II del marcador, para ratón y táctil")]
        [SerializeField] Button pauseButton;

        [Header("Pantallas (en el orden de Page)")]
        [SerializeField] CanvasGroup rootPage;
        [SerializeField] CanvasGroup optionsPage;
        [SerializeField] CanvasGroup controlsPage;
        [SerializeField] CanvasGroup confirmPage;

        [Header("Botones")]
        [SerializeField] Button continueButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button menuButton;
        [Tooltip("Lo que se selecciona al abrir las opciones")]
        [SerializeField] Selectable optionsFirst;
        [SerializeField] Button controlsButton;
        [SerializeField] Button optionsBackButton;
        [SerializeField] Button controlsBackButton;
        [SerializeField] Button confirmYesButton;
        [SerializeField] Button confirmNoButton;
        [SerializeField] Text confirmQuestion;

        [Header("Textos")]
        [SerializeField] string restartQuestion = "Vols reiniciar el partit?";
        [SerializeField] string menuQuestion = "Vols tornar al menú principal?";

        InputAction open;    // Esc, P, Start
        InputAction close;   // P, Start
        InputAction back;    // Esc, B
        PageSwitcher pages;
        Page current;
        Pending pending;

        void Awake()
        {
            open = new InputAction("OpenPause", InputActionType.Button);
            open.AddBinding("<Keyboard>/escape");
            open.AddBinding("<Keyboard>/p");
            open.AddBinding("<Gamepad>/start");
            close = new InputAction("ClosePause", InputActionType.Button);
            close.AddBinding("<Keyboard>/p");
            close.AddBinding("<Gamepad>/start");
            back = new InputAction("PauseBack", InputActionType.Button);
            back.AddBinding("<Keyboard>/escape");
            back.AddBinding("<Gamepad>/buttonEast");

            pages = new PageSwitcher(new[] { rootPage, optionsPage, controlsPage, confirmPage }, config.pageFadeDuration, config.inputGuard);
            pauseButton.onClick.AddListener(Open);
            continueButton.onClick.AddListener(Close);
            optionsButton.onClick.AddListener(() => Show(Page.Options));
            restartButton.onClick.AddListener(() => Confirm(Pending.Restart));
            menuButton.onClick.AddListener(() => Confirm(Pending.Menu));
            controlsButton.onClick.AddListener(() => Show(Page.Controls));
            optionsBackButton.onClick.AddListener(Back);
            controlsBackButton.onClick.AddListener(Back);
            confirmNoButton.onClick.AddListener(Back);
            confirmYesButton.onClick.AddListener(DoPending);
            overlay.SetActive(false);
        }

        void OnEnable()
        {
            open.Enable();
            close.Enable();
            back.Enable();
            GameEvents.MatchStateChanged += OnMatchStateChanged;
        }

        void OnDisable()
        {
            open.Disable();
            close.Disable();
            back.Disable();
            GameEvents.MatchStateChanged -= OnMatchStateChanged;
        }

        void OnDestroy()
        {
            open.Dispose();
            close.Dispose();
            back.Dispose();
        }

        void Update()
        {
            if (!overlay.activeSelf)
            {
                if (open.WasPressedThisFrame()) Open();
                return;
            }
            if (!pages.Tick()) return;
            if (close.WasPressedThisFrame()) Close();
            else if (back.WasPressedThisFrame()) Back();
        }

        void Open()
        {
            if (overlay.activeSelf || !pause.Pause()) return;
            overlay.SetActive(true);
            Show(Page.Root);
        }

        void Close()
        {
            pages.HideAll();
            overlay.SetActive(false);
            pause.Resume();
        }

        void Show(Page page, Selectable select = null)
        {
            current = page;
            switch (page)
            {
                case Page.Root: pages.Show(rootPage, select, continueButton); break;
                case Page.Options: pages.Show(optionsPage, select, optionsFirst); break;
                case Page.Controls: pages.Show(controlsPage, select, controlsBackButton); break;
                case Page.Confirm: pages.Show(confirmPage, select, confirmNoButton); break;   // lo seguro por defecto: no salir
            }
        }

        void Back()
        {
            switch (current)
            {
                case Page.Root: Close(); break;
                case Page.Options: Show(Page.Root, optionsButton); break;
                case Page.Controls: Show(Page.Options, controlsButton); break;
                case Page.Confirm: Show(Page.Root, pending == Pending.Restart ? restartButton : menuButton); break;
            }
        }

        void Confirm(Pending action)
        {
            pending = action;
            confirmQuestion.text = action == Pending.Restart ? restartQuestion : menuQuestion;
            Show(Page.Confirm);
        }

        // Al cambiar de escena, MatchPause devuelve el tiempo a la normalidad
        void DoPending()
        {
            if (pending == Pending.Restart) match.Restart();
            else SceneManager.LoadScene(config.menuScene);
        }

        // Con el partido acabado no hay pausa: el botón II desaparece
        void OnMatchStateChanged(MatchState state)
            => pauseButton.gameObject.SetActive(state != MatchState.FullTime && state != MatchState.Results);
    }
}

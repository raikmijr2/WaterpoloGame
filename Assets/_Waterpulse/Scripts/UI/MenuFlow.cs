using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Waterpulse.Config;

namespace Waterpulse.UI
{
    // Menú principal: pasa de una pantalla a otra (intro → principal → opciones → controles o confirmación),
    // vuelve atrás con Esc o B y carga el partido. La intro solo sale la primera vez en cada sesión.
    // Al cambiar de pantalla la entrada se ignora un momento, para que la misma pulsación no active el primer botón
    public class MenuFlow : MonoBehaviour
    {
        public enum Page { Intro, Main, Options, Controls, ConfirmReset }

        [SerializeField] MenuConfigSO config;
        [SerializeField] IntroScreen intro;
        [SerializeField] OptionsMenu options;
        [Tooltip("Logo pequeño de arriba: se ve en todas las pantallas menos en la intro")]
        [SerializeField] GameObject header;

        [Header("Pantallas (en el orden de Page)")]
        [SerializeField] CanvasGroup introPage;
        [SerializeField] CanvasGroup mainPage;
        [SerializeField] CanvasGroup optionsPage;
        [SerializeField] CanvasGroup controlsPage;
        [SerializeField] CanvasGroup confirmPage;

        [Header("Botones")]
        [SerializeField] Button singlePlayerButton;
        [SerializeField] Button optionsButton;
        [Tooltip("Lo que se selecciona al abrir las opciones")]
        [SerializeField] Selectable optionsFirst;
        [SerializeField] Button controlsButton;
        [SerializeField] Button resetRecordButton;
        [SerializeField] Button optionsBackButton;
        [SerializeField] Button controlsBackButton;
        [SerializeField] Button confirmYesButton;
        [SerializeField] Button confirmNoButton;

        static bool introShown;

        InputAction back;
        CanvasGroup[] pages;
        CanvasGroup current;
        Selectable pendingSelection;
        GameObject lastSelected;
        float shownAt;
        bool ready;

        public Page Current { get; private set; }

        // La intro vuelve a salir al entrar en Play aunque el "domain reload" esté desactivado
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => introShown = false;

        void Awake()
        {
            back = new InputAction("Back", InputActionType.Button);
            back.AddBinding("<Keyboard>/escape");
            back.AddBinding("<Gamepad>/buttonEast");

            pages = new[] { introPage, mainPage, optionsPage, controlsPage, confirmPage };
            intro.Finished += () => Show(Page.Main);
            singlePlayerButton.onClick.AddListener(StartSinglePlayer);
            optionsButton.onClick.AddListener(() => Show(Page.Options));
            controlsButton.onClick.AddListener(() => Show(Page.Controls));
            resetRecordButton.onClick.AddListener(() => Show(Page.ConfirmReset));
            optionsBackButton.onClick.AddListener(Back);
            controlsBackButton.onClick.AddListener(Back);
            confirmNoButton.onClick.AddListener(Back);
            confirmYesButton.onClick.AddListener(ConfirmReset);
        }

        void OnEnable() => back.Enable();
        void OnDisable() => back.Disable();
        void OnDestroy() => back.Dispose();

        void Start()
        {
            Show(introShown ? Page.Main : Page.Intro);
            introShown = true;
        }

        void Update()
        {
            float elapsed = Time.unscaledTime - shownAt;
            current.alpha = config.pageFadeDuration > 0f ? Mathf.Clamp01(elapsed / config.pageFadeDuration) : 1f;
            if (!ready)
            {
                if (elapsed < config.inputGuard) return;
                ready = true;
                current.blocksRaycasts = true;
                Select(pendingSelection != null ? pendingSelection.gameObject : null);
            }

            KeepSelection();
            if (back.WasPressedThisFrame()) Back();
        }

        // Muestra una pantalla. "select" es lo que queda seleccionado; si no vale, el de por defecto de esa pantalla
        public void Show(Page page, Selectable select = null)
        {
            Current = page;
            current = pages[(int)page];
            foreach (var p in pages) p.gameObject.SetActive(p == current);
            header.SetActive(page != Page.Intro);

            current.alpha = 0f;
            current.blocksRaycasts = false;   // ni clics ni selección hasta que pase la espera
            pendingSelection = select != null && select.IsInteractable() ? select : DefaultSelection(page);
            lastSelected = null;
            Select(null);
            shownAt = Time.unscaledTime;
            ready = false;
        }

        // Atrás (Esc, B o "Tornar"): de las opciones al menú, y de los controles o la confirmación a las opciones
        public void Back()
        {
            switch (Current)
            {
                case Page.Options: Show(Page.Main, optionsButton); break;
                case Page.Controls: Show(Page.Options, controlsButton); break;
                case Page.ConfirmReset: Show(Page.Options, resetRecordButton); break;
            }
        }

        void StartSinglePlayer() => SceneManager.LoadScene(config.matchScene);

        void ConfirmReset()
        {
            options.ResetRecord();
            Show(Page.Options, resetRecordButton);   // sin récord el botón queda desactivado y se selecciona el primero
        }

        Selectable DefaultSelection(Page page)
        {
            switch (page)
            {
                case Page.Main: return singlePlayerButton;
                case Page.Options: return optionsFirst;
                case Page.Controls: return controlsBackButton;
                case Page.ConfirmReset: return confirmNoButton;   // lo seguro por defecto: no borrar
                default: return null;
            }
        }

        // Un clic en el vacío deja la selección vacía: se recupera la última para que el teclado y el mando sigan funcionando
        void KeepSelection()
        {
            var events = EventSystem.current;
            if (events == null) return;
            var selected = events.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy) lastSelected = selected;
            else if (lastSelected != null && lastSelected.activeInHierarchy) events.SetSelectedGameObject(lastSelected);
        }

        static void Select(GameObject target)
        {
            var events = EventSystem.current;
            if (events != null) events.SetSelectedGameObject(target);
        }
    }
}

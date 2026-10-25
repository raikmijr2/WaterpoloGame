using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.UI
{
    // Menú principal: intro → "Qui juga?" (o nuevo usuario) → principal → rànquing u opciones → controles o confirmación.
    // Vuelve atrás con Esc o B y carga el partido. La intro y "Qui juga?" salen al abrir el juego; al volver de un partido
    // se va directo al menú principal con el mismo usuario. Las acciones de usuarios (entrar, crear, borrar) las hace UsersMenu
    public class MenuFlow : MonoBehaviour
    {
        public enum Page { Intro, Users, NewUser, ConfirmDeleteUser, Main, Ranking, Options, Controls, ConfirmReset }

        [SerializeField] MenuConfigSO config;
        [SerializeField] IntroScreen intro;
        [SerializeField] UsersMenu users;
        [SerializeField] RankingView ranking;
        [Tooltip("Logo pequeño de arriba: se ve en todas las pantallas menos en la intro")]
        [SerializeField] GameObject header;

        [Header("Pantallas (en el orden de Page)")]
        [SerializeField] CanvasGroup introPage;
        [SerializeField] CanvasGroup usersPage;
        [SerializeField] CanvasGroup newUserPage;
        [SerializeField] CanvasGroup confirmDeleteUserPage;
        [SerializeField] CanvasGroup mainPage;
        [SerializeField] CanvasGroup rankingPage;
        [SerializeField] CanvasGroup optionsPage;
        [SerializeField] CanvasGroup controlsPage;
        [SerializeField] CanvasGroup confirmResetPage;

        [Header("Qui juga? y Nou usuari")]
        [SerializeField] Button newUserButton;
        [Tooltip("Solo se ve si ya hay alguien elegido (se ha llegado desde \"Canviar d'usuari\")")]
        [SerializeField] Button usersBackButton;
        [SerializeField] Selectable nameInput;
        [SerializeField] Button newUserBackButton;
        [SerializeField] Button confirmDeleteNoButton;

        [Header("Principal y rànquing")]
        [SerializeField] Button singlePlayerButton;
        [SerializeField] Button rankingButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button changeUserButton;
        [SerializeField] Button rankingBackButton;

        [Header("Opciones")]
        [Tooltip("Lo que se selecciona al abrir las opciones")]
        [SerializeField] Selectable optionsFirst;
        [SerializeField] Button controlsButton;
        [SerializeField] Button resetRecordButton;
        [SerializeField] Button optionsBackButton;
        [SerializeField] Button controlsBackButton;
        [SerializeField] Button confirmResetNoButton;

        static bool introShown;

        InputAction back;
        PageSwitcher pages;

        public Page Current { get; private set; }

        // La intro vuelve a salir al entrar en Play aunque el "domain reload" esté desactivado
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => introShown = false;

        void Awake()
        {
            back = new InputAction("Back", InputActionType.Button);
            back.AddBinding("<Keyboard>/escape");
            back.AddBinding("<Gamepad>/buttonEast");

            pages = new PageSwitcher(new[] { introPage, usersPage, newUserPage, confirmDeleteUserPage, mainPage, rankingPage,
                                             optionsPage, controlsPage, confirmResetPage }, config.pageFadeDuration, config.inputGuard);

            intro.Finished += () => Show(Page.Users);
            users.SignedIn += () => Show(Page.Main);
            users.DeleteRequested += () => Show(Page.ConfirmDeleteUser);
            users.UserDeleted += () => Show(Page.Users);
            users.RecordReset += () => Show(Page.Options, resetRecordButton);

            newUserButton.onClick.AddListener(() => Show(Page.NewUser));
            usersBackButton.onClick.AddListener(Back);
            newUserBackButton.onClick.AddListener(Back);
            confirmDeleteNoButton.onClick.AddListener(Back);
            singlePlayerButton.onClick.AddListener(StartSinglePlayer);
            rankingButton.onClick.AddListener(() => Show(Page.Ranking));
            optionsButton.onClick.AddListener(() => Show(Page.Options));
            changeUserButton.onClick.AddListener(() => Show(Page.Users));
            rankingBackButton.onClick.AddListener(Back);
            controlsButton.onClick.AddListener(() => Show(Page.Controls));
            resetRecordButton.onClick.AddListener(() => Show(Page.ConfirmReset));
            optionsBackButton.onClick.AddListener(Back);
            controlsBackButton.onClick.AddListener(Back);
            confirmResetNoButton.onClick.AddListener(Back);
        }

        void OnEnable() => back.Enable();
        void OnDisable() => back.Disable();
        void OnDestroy() => back.Dispose();

        void Start()
        {
            if (!introShown) Show(Page.Intro);
            else Show(PlayerSession.HasChosen ? Page.Main : Page.Users);
            introShown = true;
        }

        void Update()
        {
            if (pages.Tick() && back.WasPressedThisFrame()) Back();
        }

        // Muestra una pantalla con sus datos al día. "select" es lo que queda seleccionado si se puede usar;
        // si no, el de por defecto de esa pantalla
        public void Show(Page page, Selectable select = null)
        {
            Current = page;
            header.SetActive(page != Page.Intro);
            switch (page)
            {
                case Page.Users:
                    users.RefreshList();
                    usersBackButton.gameObject.SetActive(PlayerSession.HasChosen);
                    break;
                case Page.NewUser: users.PrepareNewUser(); break;
                case Page.Main:
                case Page.Options: users.RefreshCurrent(); break;
                case Page.Ranking: ranking.Refresh(); break;
            }

            var defaults = DefaultSelection(page);
            var order = new Selectable[defaults.Length + 1];
            order[0] = select;
            defaults.CopyTo(order, 1);
            pages.Show(PageGroup(page), order);
        }

        // Atrás (Esc, B o "Tornar")
        public void Back()
        {
            switch (Current)
            {
                case Page.Users:
                    if (PlayerSession.HasChosen) Show(Page.Main, changeUserButton);   // sin nadie elegido no hay adónde volver
                    break;
                case Page.NewUser: Show(Page.Users, newUserButton); break;
                case Page.ConfirmDeleteUser: Show(Page.Users); break;
                case Page.Ranking: Show(Page.Main, rankingButton); break;
                case Page.Options: Show(Page.Main, optionsButton); break;
                case Page.Controls: Show(Page.Options, controlsButton); break;
                case Page.ConfirmReset: Show(Page.Options, resetRecordButton); break;
            }
        }

        void StartSinglePlayer() => SceneManager.LoadScene(config.matchScene);

        CanvasGroup PageGroup(Page page)
        {
            switch (page)
            {
                case Page.Intro: return introPage;
                case Page.Users: return usersPage;
                case Page.NewUser: return newUserPage;
                case Page.ConfirmDeleteUser: return confirmDeleteUserPage;
                case Page.Main: return mainPage;
                case Page.Ranking: return rankingPage;
                case Page.Options: return optionsPage;
                case Page.Controls: return controlsPage;
                default: return confirmResetPage;
            }
        }

        Selectable[] DefaultSelection(Page page)
        {
            switch (page)
            {
                case Page.Users: return users.SelectionOrder;
                case Page.NewUser: return new[] { nameInput };
                case Page.ConfirmDeleteUser: return new Selectable[] { confirmDeleteNoButton };   // lo seguro por defecto: no borrar
                case Page.Main: return new Selectable[] { singlePlayerButton };
                case Page.Ranking: return new Selectable[] { rankingBackButton };
                case Page.Options: return new[] { optionsFirst };
                case Page.Controls: return new Selectable[] { controlsBackButton };
                case Page.ConfirmReset: return new Selectable[] { confirmResetNoButton };
                default: return new Selectable[0];
            }
        }
    }
}

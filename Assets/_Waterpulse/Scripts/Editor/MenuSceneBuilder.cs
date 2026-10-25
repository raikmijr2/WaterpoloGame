using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Waterpulse.Audio;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;
using Waterpulse.UI;
using static Waterpulse.EditorTools.MatchSceneBuilder;
using static Waterpulse.EditorTools.SharedPages;
using static Waterpulse.EditorTools.UiFactory;

namespace Waterpulse.EditorTools
{
    // Genera la escena del menú principal: de fondo, la piscina del partido (sin jugadores) con la cámara girando despacio;
    // delante, la intro y las pantallas del menú: "Qui juga?", nuevo usuario, principal, rànquing, opciones, controles
    // y las confirmaciones. Crea la escena desde cero cada vez (no duplica objetos) y solo crea los assets que no existen
    public static class MenuSceneBuilder
    {
        public const string ScenePath = RootDir + "/Scenes/Menu.unity";
        public const string ConfigPath = RootDir + "/Config/MenuConfig.asset";

        // Medidas de la interfaz (resolución de referencia 1920×1080; el lienzo se ajusta por altura)
        static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        static readonly Vector2 IntroLogoAnchor = new Vector2(0.5f, 0.6f);
        static readonly Vector2 PromptAnchor = new Vector2(0.5f, 0.22f);
        static readonly Vector2 OptionsAnchor = new Vector2(0.5f, 0.42f);   // el panel de opciones es más alto: un poco más abajo que los demás
        static readonly Vector2 MainButtonSize = new Vector2(620f, 90f);
        static readonly Vector2 LogoOutline = new Vector2(4f, -4f);
        static readonly Vector2 BadgeSize = new Vector2(140f, 44f);
        static readonly Vector2 UserCellSize = new Vector2(500f, 70f);
        static readonly Vector2 UserCellSpacing = new Vector2(40f, 12f);
        const float UserDeleteGap = 10f;
        const int MainButtonFont = 46;
        const float BadgeTilt = 8f;
        const float RankingLineSpacing = 1.1f;

        // Colores propios del menú (el resto son los de UiFactory y SharedPages, compartidos con el partido)
        static readonly Color ShadeColor = new Color(0.02f, 0.07f, 0.15f, 0.45f);   // oscurece la piscina para que se lea el texto
        static readonly Color DisabledText = new Color(0.75f, 0.78f, 0.82f);
        static readonly Color BadgeText = new Color(0.05f, 0.1f, 0.25f);
        static readonly Color ErrorColor = new Color(1f, 0.55f, 0.45f);
        static readonly Color CameraBackground = new Color(0.12f, 0.16f, 0.2f);   // el mismo que la cámara del partido

        [MenuItem("Waterpulse/Build Menu Scene", priority = 1)]
        static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            Debug.Log($"[Waterpulse] Menú generado en {ScenePath}. Pulsa Play para ver la intro.");
        }

        public static void Build()
        {
            EnsureFolder(RootDir + "/Scenes");
            EnsureFolder(RootDir + "/Config");
            EnsureFolder(MaterialDir);

            // La escena nueva se crea ANTES de cargar assets: NewScene descarga los assets no usados
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = LoadOrCreate<GameConfigSO>(MatchSceneBuilder.ConfigPath);
            var menuConfig = LoadOrCreate<MenuConfigSO>(ConfigPath);
            var usersConfig = LoadOrCreate<UsersConfigSO>(UsersConfigPath);
            var scoringConfig = LoadOrCreate<ScoringConfigSO>(ScoringConfigPath);
            var mats = new Materials();

            // Fondo: la misma piscina que el partido, sin jugadores y con la pelota (solo decorado) en el centro
            BuildLight();
            var pool = BuildPool(config, mats);
            BuildGoal(config, mats, pool.transform, -1f, TeamSide.Home);
            BuildGoal(config, mats, pool.transform, 1f, TeamSide.Away);
            Prim(PrimitiveType.Sphere, "Ball", null, pool.Center, Vector3.one * config.ballRadius * 2f, mats.Ball, false);
            BuildCamera(menuConfig, pool);

            // Música del menú y sonidos de la interfaz (las habilidades, si el partido ya se ha generado alguna vez)
            BuildAudio(SoundGenerator.EnsureAudio(AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(XutTragolPath),
                                                  AssetDatabase.LoadAssetAtPath<AbilityDefinitionSO>(TorpedeBlauPath)), AudioScene.Menu);

            BuildMenu(menuConfig, usersConfig, scoringConfig);
            // Los botones necesitan un EventSystem que lea el Input System nuevo
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
        }

        // Escenas del build: primero el menú (es la que se abre al cargar el juego) y después el partido.
        // Solo se añaden las que ya existen; lo llaman los dos generadores
        internal static void RegisterBuildScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in new[] { ScenePath, MatchSceneBuilder.ScenePath })
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void BuildCamera(MenuConfigSO menu, PoolBounds pool)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = CameraBackground;
            cam.fieldOfView = menu.cameraFieldOfView;
            go.AddComponent<AudioListener>();
            var orbit = go.AddComponent<MenuCamera>();
            SetRef(orbit, "config", menu);
            SetRef(orbit, "pool", pool);
            orbit.Place(0f);
        }

        static void BuildMenu(MenuConfigSO menu, UsersConfigSO usersConfig, ScoringConfigSO scoring)
        {
            var canvasGo = new GameObject("Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 1f;   // orientación horizontal: el menú siempre cabe en altura
            var root = canvasGo.transform;

            StretchedImage(root, "Shade", ShadeColor);

            // Logo pequeño de arriba, en todas las pantallas menos en la intro
            var header = StretchedGroup(root, "Header").gameObject;
            Logo(header.transform, "Title", 120, Top, new Vector2(0f, -40f), new Vector2(1400f, 150f));
            Subtitle(header.transform, 40, Top, new Vector2(0f, -190f));

            // Intro: logo grande que aparece con un fundido y "Prem qualsevol tecla" parpadeando
            var introPage = StretchedGroup(root, "IntroPage");
            var logo = StretchedGroup(introPage.transform, "Logo");
            Logo(logo.transform, "Title", 200, IntroLogoAnchor, Vector2.zero, new Vector2(1600f, 240f));
            Subtitle(logo.transform, 52, IntroLogoAnchor, new Vector2(0f, -150f));
            var prompt = Label(introPage.transform, "Prompt", 46, PromptAnchor, Vector2.zero, new Vector2(1200f, 70f));
            prompt.text = "Prem qualsevol tecla";
            var intro = introPage.gameObject.AddComponent<IntroScreen>();
            SetRef(intro, "config", menu);
            SetRef(intro, "logo", logo);
            SetRef(intro, "prompt", prompt.gameObject.AddComponent<CanvasGroup>());

            // Qui juga?: los usuarios en dos columnas (UsersMenu clona la plantilla), nuevo usuario, convidat y tornar
            var usersPage = StretchedGroup(root, "UsersPage");
            var usersBox = Box(usersPage.transform, "Box", BoxAnchor, new Vector2(1200f, 660f));
            Heading(usersBox, "Qui juga?");
            var slotTemplate = BuildUserList(usersBox);
            var emptyUsers = Label(usersBox, "EmptyLabel", 36, Top, new Vector2(0f, -280f), new Vector2(1000f, 50f));
            emptyUsers.text = "Encara no hi ha cap usuari: crea'n un!";
            emptyUsers.color = SoftText;
            var newUserButton = MenuButton<Button>(usersBox, "NewUserButton", "Nou usuari", BoxButtonFont, Top, new Vector2(-400f, -590f), new Vector2(360f, 80f));
            var guestButton = MenuButton<Button>(usersBox, "GuestButton", "Jugar com a convidat", BoxButtonFont, Top, new Vector2(30f, -590f), new Vector2(440f, 80f));
            var usersBack = MenuButton<Button>(usersBox, "BackButton", "Tornar", BoxButtonFont, Top, new Vector2(430f, -590f), new Vector2(300f, 80f));

            // Nou usuari: solo el nombre
            var newUserPage = StretchedGroup(root, "NewUserPage");
            var newUserBox = Box(newUserPage.transform, "Box", BoxAnchor, new Vector2(900f, 440f));
            Heading(newUserBox, "Nou usuari");
            var nameInput = StyledInputField(newUserBox, "NameInput", "Escriu el teu nom", 40, Top, new Vector2(0f, -150f), new Vector2(600f, 80f));
            nameInput.characterLimit = usersConfig.maxNameLength;
            var nameError = Label(newUserBox, "NameError", 32, Top, new Vector2(0f, -205f), new Vector2(820f, 44f));
            nameError.text = string.Empty;
            nameError.color = ErrorColor;
            var createButton = MenuButton<Button>(newUserBox, "CreateButton", "Crear", BoxButtonFont, Top, new Vector2(-170f, -330f), ConfirmButtonSize);
            var newUserBack = MenuButton<Button>(newUserBox, "BackButton", "Tornar", BoxButtonFont, Top, new Vector2(170f, -330f), ConfirmButtonSize);

            var deleteUserPage = StretchedGroup(root, "ConfirmDeleteUserPage");
            var deleteUser = Confirm(deleteUserPage.transform, BoxAnchor, string.Empty, "També s'esborrarà el seu rècord. No es pot desfer.");
            MakeDanger(deleteUser.Yes);

            // Principal: jugador actual, partido individual, 1 contra 1 local (se ve, pero el modo aún no existe),
            // rànquing, opciones y cambiar de usuario
            var mainPage = StretchedGroup(root, "MainPage");
            var playerLabel = Label(mainPage.transform, "PlayerLabel", 40, Top, new Vector2(0f, -265f), new Vector2(1200f, 56f));
            playerLabel.color = Yellow;
            var singlePlayer = MenuButton<Button>(mainPage.transform, "SinglePlayerButton", "Partit individual", MainButtonFont, Middle, new Vector2(0f, 150f), MainButtonSize);
            var versus = MenuButton<Button>(mainPage.transform, "LocalVersusButton", "1 contra 1 local", MainButtonFont, Middle, new Vector2(0f, 40f), MainButtonSize);
            versus.interactable = false;
            versus.GetComponentInChildren<Text>().color = DisabledText;
            Badge(versus.transform, "Aviat");
            var rankingButton = MenuButton<Button>(mainPage.transform, "RankingButton", "Rànquing", MainButtonFont, Middle, new Vector2(0f, -70f), MainButtonSize);
            var optionsButton = MenuButton<Button>(mainPage.transform, "OptionsButton", "Opcions", MainButtonFont, Middle, new Vector2(0f, -180f), MainButtonSize);
            var changeUser = MenuButton<Button>(mainPage.transform, "ChangeUserButton", "Canviar d'usuari", MainButtonFont, Middle, new Vector2(0f, -290f), MainButtonSize);

            // Rànquing: posición, nombre y puntos en tres columnas
            var rankingPage = StretchedGroup(root, "RankingPage");
            var rankingBox = Box(rankingPage.transform, "Box", BoxAnchor, new Vector2(900f, 680f));
            Heading(rankingBox, "Rànquing");
            var positions = RankingColumn(rankingBox, "Positions", -330f, 120f, TextAnchor.UpperRight);
            var names = RankingColumn(rankingBox, "Names", -20f, 440f, TextAnchor.UpperLeft);
            var points = RankingColumn(rankingBox, "Points", 300f, 240f, TextAnchor.UpperRight);
            var emptyRanking = Label(rankingBox, "EmptyLabel", 36, Top, new Vector2(0f, -300f), new Vector2(800f, 50f));
            emptyRanking.text = "Encara no hi ha cap rècord";
            emptyRanking.color = SoftText;
            var rankingBack = MenuButton<Button>(rankingBox, "BackButton", "Tornar", BoxButtonFont, Top, new Vector2(0f, -625f), BoxButtonSize);

            // Opciones (con el récord del usuario), controles y confirmación para borrar el récord
            var optionsPage = StretchedGroup(root, "OptionsPage");
            var options = Options(optionsPage.transform, OptionsAnchor, true);
            var controlsPage = StretchedGroup(root, "ControlsPage");
            var controlsBack = Controls(controlsPage.transform, BoxAnchor);
            var resetPage = StretchedGroup(root, "ConfirmResetPage");
            var reset = Confirm(resetPage.transform, BoxAnchor, "Vols esborrar el teu rècord?", "No es pot desfer.");
            MakeDanger(reset.Yes);

            WireOptions(canvasGo.AddComponent<OptionsMenu>(), menu, options);

            var users = canvasGo.AddComponent<UsersMenu>();
            SetRef(users, "config", usersConfig);
            SetRef(users, "scoringConfig", scoring);
            SetRef(users, "slotTemplate", slotTemplate);
            SetRef(users, "emptyLabel", emptyUsers);
            SetRef(users, "newUserButton", newUserButton);
            SetRef(users, "guestButton", guestButton);
            SetRef(users, "nameInput", nameInput);
            SetRef(users, "nameError", nameError);
            SetRef(users, "createButton", createButton);
            SetRef(users, "deleteQuestion", deleteUser.Question);
            SetRef(users, "confirmDeleteButton", deleteUser.Yes);
            SetRef(users, "playerLabel", playerLabel);
            SetRef(users, "recordLabel", options.RecordLabel);
            SetRef(users, "resetRecordButton", options.ResetRecord);
            SetRef(users, "confirmResetButton", reset.Yes);

            var ranking = canvasGo.AddComponent<RankingView>();
            SetRef(ranking, "config", usersConfig);
            SetRef(ranking, "scoringConfig", scoring);
            SetRef(ranking, "positionsColumn", positions);
            SetRef(ranking, "namesColumn", names);
            SetRef(ranking, "pointsColumn", points);
            SetRef(ranking, "emptyLabel", emptyRanking);

            var flow = canvasGo.AddComponent<MenuFlow>();
            SetRef(flow, "config", menu);
            SetRef(flow, "intro", intro);
            SetRef(flow, "users", users);
            SetRef(flow, "ranking", ranking);
            SetRef(flow, "header", header);
            SetRef(flow, "introPage", introPage);
            SetRef(flow, "usersPage", usersPage);
            SetRef(flow, "newUserPage", newUserPage);
            SetRef(flow, "confirmDeleteUserPage", deleteUserPage);
            SetRef(flow, "mainPage", mainPage);
            SetRef(flow, "rankingPage", rankingPage);
            SetRef(flow, "optionsPage", optionsPage);
            SetRef(flow, "controlsPage", controlsPage);
            SetRef(flow, "confirmResetPage", resetPage);
            SetRef(flow, "newUserButton", newUserButton);
            SetRef(flow, "usersBackButton", usersBack);
            SetRef(flow, "nameInput", nameInput);
            SetRef(flow, "newUserBackButton", newUserBack);
            SetRef(flow, "confirmDeleteNoButton", deleteUser.No);
            SetRef(flow, "singlePlayerButton", singlePlayer);
            SetRef(flow, "rankingButton", rankingButton);
            SetRef(flow, "optionsButton", optionsButton);
            SetRef(flow, "changeUserButton", changeUser);
            SetRef(flow, "rankingBackButton", rankingBack);
            SetRef(flow, "optionsFirst", options.Master);
            SetRef(flow, "controlsButton", options.Controls);
            SetRef(flow, "resetRecordButton", options.ResetRecord);
            SetRef(flow, "optionsBackButton", options.Back);
            SetRef(flow, "controlsBackButton", controlsBack);
            SetRef(flow, "confirmResetNoButton", reset.No);

            // En la escena guardada se ve la intro; al empezar, MenuFlow decide qué pantalla enseña
            header.SetActive(false);
            foreach (var page in new[] { usersPage, newUserPage, deleteUserPage, mainPage, rankingPage, optionsPage, controlsPage, resetPage })
                page.gameObject.SetActive(false);
        }

        // Cuadrícula de dos columnas para los usuarios y su plantilla (inactiva): botón con el nombre y, a su derecha, el de borrar
        static UserSlot BuildUserList(Transform box)
        {
            var list = new GameObject("UserList", typeof(RectTransform), typeof(GridLayoutGroup));
            list.transform.SetParent(box, false);
            var rt = list.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = Top;
            rt.anchoredPosition = new Vector2(0f, -110f);
            rt.sizeDelta = new Vector2(UserCellSize.x * 2f + UserCellSpacing.x, 420f);
            var grid = list.GetComponent<GridLayoutGroup>();
            grid.cellSize = UserCellSize;
            grid.spacing = UserCellSpacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            var go = new GameObject("UserSlotTemplate", typeof(RectTransform));
            go.transform.SetParent(list.transform, false);
            float deleteSize = UserCellSize.y;
            float nameWidth = UserCellSize.x - deleteSize - UserDeleteGap;
            var nameButton = MenuButton<Button>(go.transform, "NameButton", "Nom", BoxButtonFont, new Vector2(0f, 0.5f), new Vector2(nameWidth * 0.5f, 0f), new Vector2(nameWidth, UserCellSize.y));
            var deleteButton = MenuButton<Button>(go.transform, "DeleteButton", "X", BoxButtonFont, new Vector2(1f, 0.5f), new Vector2(-deleteSize * 0.5f, 0f), new Vector2(deleteSize, deleteSize));
            MakeDanger(deleteButton);
            var slot = go.AddComponent<UserSlot>();
            SetRef(slot, "nameButton", nameButton);
            SetRef(slot, "nameLabel", nameButton.GetComponentInChildren<Text>());
            SetRef(slot, "deleteButton", deleteButton);
            go.SetActive(false);
            return slot;
        }

        static Text RankingColumn(Transform box, string name, float x, float width, TextAnchor alignment)
        {
            var column = Label(box, name, 36, Top, new Vector2(x, -110f), new Vector2(width, 470f));
            column.alignment = alignment;
            column.lineSpacing = RankingLineSpacing;
            column.text = string.Empty;
            return column;
        }

        // Logo "WATERPULSE" con "PULSE" en el amarillo de los avisos
        static void Logo(Transform parent, string name, int size, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var logo = Label(parent, name, size, anchor, position, dimensions);
            logo.text = $"WATER<color=#{ColorUtility.ToHtmlStringRGB(Yellow)}>PULSE</color>";
            logo.fontStyle = FontStyle.Bold;
            logo.GetComponent<Outline>().effectDistance = LogoOutline;
        }

        static void Subtitle(Transform parent, int size, Vector2 anchor, Vector2 position)
        {
            var subtitle = Label(parent, "Subtitle", size, anchor, position, new Vector2(1200f, size * 1.5f));
            subtitle.text = "Waterpolo arcade";
            subtitle.color = SoftText;
        }

        // Etiqueta amarilla, un poco inclinada, en la esquina de un botón
        static void Badge(Transform button, string text)
        {
            var go = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(button, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.one;
            rt.anchoredPosition = new Vector2(-BadgeSize.x * 0.5f, 0f);
            rt.sizeDelta = BadgeSize;
            rt.localRotation = Quaternion.Euler(0f, 0f, -BadgeTilt);
            var image = go.GetComponent<Image>();
            image.color = Yellow;
            image.raycastTarget = false;
            var label = Label(go.transform, "Label", 28, Middle, Vector2.zero, BadgeSize);
            label.text = text;
            label.color = BadgeText;
            Object.DestroyImmediate(label.GetComponent<Outline>());   // texto oscuro sobre amarillo: sin contorno
        }
    }
}

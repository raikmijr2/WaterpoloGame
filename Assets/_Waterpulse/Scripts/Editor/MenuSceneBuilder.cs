using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;
using Waterpulse.UI;
using static Waterpulse.EditorTools.MatchSceneBuilder;
using static Waterpulse.EditorTools.UiFactory;

namespace Waterpulse.EditorTools
{
    // Genera la escena del menú principal: de fondo, la piscina del partido (sin jugadores) con la cámara girando despacio;
    // delante, la intro y las pantallas del menú (principal, opciones, controles y confirmación para borrar el récord).
    // Crea la escena desde cero cada vez (no duplica objetos) y solo crea MenuConfig.asset si no existe
    public static class MenuSceneBuilder
    {
        public const string ScenePath = RootDir + "/Scenes/Menu.unity";
        public const string ConfigPath = RootDir + "/Config/MenuConfig.asset";

        // Medidas de la interfaz (resolución de referencia 1920×1080; el lienzo se ajusta por altura)
        static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        static readonly Vector2 Middle = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Top = new Vector2(0.5f, 1f);
        static readonly Vector2 BoxAnchor = new Vector2(0.5f, 0.44f);
        static readonly Vector2 IntroLogoAnchor = new Vector2(0.5f, 0.6f);
        static readonly Vector2 PromptAnchor = new Vector2(0.5f, 0.22f);
        static readonly Vector2 MainButtonSize = new Vector2(620f, 100f);
        static readonly Vector2 BoxButtonSize = new Vector2(600f, 80f);
        static readonly Vector2 ConfirmButtonSize = new Vector2(280f, 90f);
        static readonly Vector2 LogoOutline = new Vector2(4f, -4f);
        static readonly Vector2 BadgeSize = new Vector2(140f, 44f);
        const int MainButtonFont = 48;
        const int BoxButtonFont = 40;
        const float BadgeTilt = 8f;
        const float ControlsLineSpacing = 1.3f;

        // Colores propios del menú (el resto son los de UiFactory, compartidos con el partido)
        static readonly Color ShadeColor = new Color(0.02f, 0.07f, 0.15f, 0.45f);   // oscurece la piscina para que se lea el texto
        static readonly Color SoftText = new Color(0.8f, 0.85f, 0.9f);
        static readonly Color DisabledText = new Color(0.75f, 0.78f, 0.82f);
        static readonly Color BadgeText = new Color(0.05f, 0.1f, 0.25f);
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
            var scoringConfig = LoadOrCreate<ScoringConfigSO>(ScoringConfigPath);
            var mats = new Materials();

            // Fondo: la misma piscina que el partido, sin jugadores y con la pelota (solo decorado) en el centro
            BuildLight();
            var pool = BuildPool(config, mats);
            BuildGoal(config, mats, pool.transform, -1f, TeamSide.Home);
            BuildGoal(config, mats, pool.transform, 1f, TeamSide.Away);
            Prim(PrimitiveType.Sphere, "Ball", null, pool.Center, Vector3.one * config.ballRadius * 2f, mats.Ball, false);
            BuildCamera(menuConfig, pool);

            BuildMenu(menuConfig, scoringConfig);
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

        static void BuildMenu(MenuConfigSO menu, ScoringConfigSO scoring)
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

            // Principal: partido individual, 1 contra 1 local (se ve, pero el modo aún no existe) y opciones
            var mainPage = StretchedGroup(root, "MainPage");
            var singlePlayer = MenuButton<Button>(mainPage.transform, "SinglePlayerButton", "Partit individual", MainButtonFont, Middle, new Vector2(0f, 80f), MainButtonSize);
            var versus = MenuButton<Button>(mainPage.transform, "LocalVersusButton", "1 contra 1 local", MainButtonFont, Middle, new Vector2(0f, -50f), MainButtonSize);
            versus.interactable = false;
            versus.GetComponentInChildren<Text>().color = DisabledText;
            Badge(versus.transform, "Aviat");
            var optionsButton = MenuButton<Button>(mainPage.transform, "OptionsButton", "Opcions", MainButtonFont, Middle, new Vector2(0f, -180f), MainButtonSize);

            // Opciones: volumen, pantalla completa, controles y récord
            var optionsPage = StretchedGroup(root, "OptionsPage");
            var optionsBox = Box(optionsPage.transform, "Box", BoxAnchor, new Vector2(900f, 680f));
            Heading(optionsBox, "Opcions");
            var volumeLabel = Label(optionsBox, "VolumeLabel", 36, Top, new Vector2(0f, -115f), new Vector2(800f, 50f));
            var volume = StyledSlider(optionsBox, "VolumeSlider", Top, new Vector2(0f, -195f), new Vector2(600f, 36f));
            volume.gameObject.AddComponent<SelectOnHover>();
            var fullscreen = MenuButton<PressButton>(optionsBox, "FullscreenButton", "Pantalla completa", BoxButtonFont, Top, new Vector2(0f, -275f), BoxButtonSize);
            var controlsButton = MenuButton<Button>(optionsBox, "ControlsButton", "Controls", BoxButtonFont, Top, new Vector2(0f, -375f), BoxButtonSize);
            var recordLabel = Label(optionsBox, "RecordLabel", 32, Top, new Vector2(0f, -440f), new Vector2(800f, 44f));
            recordLabel.color = SoftText;
            var resetRecord = MenuButton<Button>(optionsBox, "ResetRecordButton", "Esborrar rècord", BoxButtonFont, Top, new Vector2(0f, -535f), BoxButtonSize);
            var optionsBack = MenuButton<Button>(optionsBox, "BackButton", "Tornar", BoxButtonFont, Top, new Vector2(0f, -625f), BoxButtonSize);

            // Controles de teclado y mando (los de HumanBrain y ControlSwitcher)
            var controlsPage = StretchedGroup(root, "ControlsPage");
            var controlsBox = Box(controlsPage.transform, "Box", BoxAnchor, new Vector2(1200f, 680f));
            Heading(controlsBox, "Controls");
            ControlsColumn(controlsBox, "Actions", -360f, "Acció", "Nedar", "Passar / recuperar", "Xutar", "Habilitat", "Esprint", "Nedar lent (mantenir)", "Canviar de jugador");
            ControlsColumn(controlsBox, "Keyboard", 60f, "Teclat", "W A S D", "J", "K", "L", "Shift", "Espai", "Q");
            ControlsColumn(controlsBox, "Gamepad", 400f, "Comandament", "Joystick esquerre", "A", "B", "Y", "RB", "LB", "LT");
            var controlsBack = MenuButton<Button>(controlsBox, "BackButton", "Tornar", BoxButtonFont, Top, new Vector2(0f, -610f), BoxButtonSize);

            // Confirmación antes de borrar el récord ("No" es lo seleccionado por defecto)
            var confirmPage = StretchedGroup(root, "ConfirmPage");
            var confirmBox = Box(confirmPage.transform, "Box", BoxAnchor, new Vector2(900f, 340f));
            var question = Label(confirmBox, "Question", 42, Top, new Vector2(0f, -40f), new Vector2(820f, 60f));
            question.text = "Vols esborrar el rècord personal?";
            var warning = Label(confirmBox, "Warning", 32, Top, new Vector2(0f, -110f), new Vector2(820f, 44f));
            warning.text = "No es pot desfer.";
            warning.color = SoftText;
            var yes = MenuButton<Button>(confirmBox, "YesButton", "Sí", BoxButtonFont, Top, new Vector2(-170f, -240f), ConfirmButtonSize);
            var no = MenuButton<Button>(confirmBox, "NoButton", "No", BoxButtonFont, Top, new Vector2(170f, -240f), ConfirmButtonSize);

            var options = canvasGo.AddComponent<OptionsMenu>();
            SetRef(options, "config", menu);
            SetRef(options, "scoringConfig", scoring);
            SetRef(options, "volumeSlider", volume);
            SetRef(options, "volumeLabel", volumeLabel);
            SetRef(options, "fullscreenButton", fullscreen);
            SetRef(options, "fullscreenLabel", fullscreen.GetComponentInChildren<Text>());
            SetRef(options, "recordLabel", recordLabel);
            SetRef(options, "resetRecordButton", resetRecord);

            var flow = canvasGo.AddComponent<MenuFlow>();
            SetRef(flow, "config", menu);
            SetRef(flow, "intro", intro);
            SetRef(flow, "options", options);
            SetRef(flow, "header", header);
            SetRef(flow, "introPage", introPage);
            SetRef(flow, "mainPage", mainPage);
            SetRef(flow, "optionsPage", optionsPage);
            SetRef(flow, "controlsPage", controlsPage);
            SetRef(flow, "confirmPage", confirmPage);
            SetRef(flow, "singlePlayerButton", singlePlayer);
            SetRef(flow, "optionsButton", optionsButton);
            SetRef(flow, "optionsFirst", volume);
            SetRef(flow, "controlsButton", controlsButton);
            SetRef(flow, "resetRecordButton", resetRecord);
            SetRef(flow, "optionsBackButton", optionsBack);
            SetRef(flow, "controlsBackButton", controlsBack);
            SetRef(flow, "confirmYesButton", yes);
            SetRef(flow, "confirmNoButton", no);

            // En la escena guardada se ve la intro; al empezar, MenuFlow decide qué pantalla enseña
            header.SetActive(false);
            foreach (var page in new[] { mainPage, optionsPage, controlsPage, confirmPage })
                page.gameObject.SetActive(false);
        }

        // Botón del menú: el del estilo del juego, que además se selecciona al pasar el ratón
        static T MenuButton<T>(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 position, Vector2 size) where T : Button
        {
            var button = StyledButton<T>(parent, name, text, fontSize, anchor, position, size);
            button.gameObject.AddComponent<SelectOnHover>();
            return button;
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

        static void Heading(Transform box, string text)
        {
            var heading = Label(box, "Heading", 56, Top, new Vector2(0f, -25f), new Vector2(800f, 70f));
            heading.text = text;
            heading.color = Yellow;
        }

        // Columna de la tabla de controles: título en amarillo y una línea por acción
        static void ControlsColumn(Transform box, string name, float x, string title, params string[] rows)
        {
            var heading = Label(box, name + "Title", 34, Top, new Vector2(x, -115f), new Vector2(400f, 50f));
            heading.text = title;
            heading.color = Yellow;
            var column = Label(box, name, 32, Top, new Vector2(x, -175f), new Vector2(400f, 360f));
            column.alignment = TextAnchor.UpperCenter;
            column.lineSpacing = ControlsLineSpacing;
            column.text = string.Join("\n", rows);
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

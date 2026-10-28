using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.UI;
using static Waterpulse.EditorTools.MatchSceneBuilder;
using static Waterpulse.EditorTools.UiFactory;

namespace Waterpulse.EditorTools
{
    // Piezas y pantallas que comparten el menú principal y la pausa del partido (opciones, controles y confirmación),
    // con las mismas medidas y textos en los dos sitios. Las posiciones dentro de un panel se miden desde su borde de arriba
    internal static class SharedPages
    {
        internal static readonly Vector2 Middle = new Vector2(0.5f, 0.5f);
        internal static readonly Vector2 Top = new Vector2(0.5f, 1f);
        internal static readonly Vector2 BoxAnchor = new Vector2(0.5f, 0.44f);   // en el menú, por debajo del logo
        internal static readonly Vector2 BoxButtonSize = new Vector2(600f, 80f);
        internal static readonly Vector2 ConfirmButtonSize = new Vector2(280f, 90f);
        internal const int BoxButtonFont = 40;
        const float ControlsLineSpacing = 1.3f;

        internal struct OptionsWidgets
        {
            public Slider Master;
            public Text MasterLabel;
            public Slider Music;
            public Text MusicLabel;
            public Slider Effects;
            public Text EffectsLabel;
            public PressButton Fullscreen;
            public Button Controls;
            public Text RecordLabel;    // solo en el menú principal
            public Button ResetRecord;  // solo en el menú principal
            public Button Back;
        }

        internal struct ConfirmWidgets
        {
            public Text Question;
            public Button Yes;
            public Button No;
        }

        // Botón de menú: el del estilo del juego, que además se selecciona al pasar el ratón
        internal static T MenuButton<T>(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 position, Vector2 size) where T : Button
        {
            var button = StyledButton<T>(parent, name, text, fontSize, anchor, position, size);
            button.gameObject.AddComponent<SelectOnHover>();
            return button;
        }

        // Título de un panel, en mayúsculas como los rótulos de la tele
        internal static void Heading(Transform box, string text)
        {
            var heading = Label(box, "Heading", 56, Top, new Vector2(0f, -25f), new Vector2(800f, 70f));
            heading.text = text.ToUpperInvariant();
            heading.color = Yellow;
        }

        // Opciones: volúmenes (general, música y efectos), pantalla completa y controles; en el menú principal, también
        // el récord del usuario. Una sola columna: con los deslizadores, izquierda y derecha cambian el valor y no servirían
        // para pasar a otra columna con el teclado o el mando
        internal static OptionsWidgets Options(Transform page, Vector2 anchor, bool withRecord)
        {
            var box = Box(page, "Box", anchor, new Vector2(900f, withRecord ? 725f : 585f));
            Heading(box, "Opcions");
            var w = new OptionsWidgets();
            w.MasterLabel = VolumeRow(box, "Master", -130f, out w.Master);
            w.MusicLabel = VolumeRow(box, "Music", -195f, out w.Music);
            w.EffectsLabel = VolumeRow(box, "Effects", -260f, out w.Effects);
            w.Fullscreen = MenuButton<PressButton>(box, "FullscreenButton", "Pantalla completa", BoxButtonFont, Top, new Vector2(0f, -340f), BoxButtonSize);
            w.Controls = MenuButton<Button>(box, "ControlsButton", "Controls", BoxButtonFont, Top, new Vector2(0f, -430f), BoxButtonSize);
            if (withRecord)
            {
                w.RecordLabel = Label(box, "RecordLabel", 30, Top, new Vector2(0f, -490f), new Vector2(800f, 40f), true);
                w.RecordLabel.color = SoftText;
                w.ResetRecord = MenuButton<Button>(box, "ResetRecordButton", "Esborrar el meu rècord", BoxButtonFont, Top, new Vector2(0f, -575f), BoxButtonSize);
            }
            w.Back = MenuButton<Button>(box, "BackButton", "Tornar", BoxButtonFont, Top, new Vector2(0f, withRecord ? -665f : -525f), BoxButtonSize);
            return w;
        }

        // Conecta la pantalla de opciones con su OptionsMenu
        internal static void WireOptions(OptionsMenu menu, MenuConfigSO config, OptionsWidgets w)
        {
            SetRef(menu, "config", config);
            SetRef(menu, "masterSlider", w.Master);
            SetRef(menu, "masterLabel", w.MasterLabel);
            SetRef(menu, "musicSlider", w.Music);
            SetRef(menu, "musicLabel", w.MusicLabel);
            SetRef(menu, "effectsSlider", w.Effects);
            SetRef(menu, "effectsLabel", w.EffectsLabel);
            SetRef(menu, "fullscreenButton", w.Fullscreen);
            SetRef(menu, "fullscreenLabel", w.Fullscreen.GetComponentInChildren<Text>());
        }

        // Una fila de volumen: el nombre y el porcentaje a la izquierda (lo escribe OptionsMenu) y el deslizador a la derecha
        static Text VolumeRow(Transform box, string name, float y, out Slider slider)
        {
            var label = Label(box, name + "Label", 34, Top, new Vector2(-210f, y + 25f), new Vector2(380f, 50f));
            label.alignment = TextAnchor.MiddleLeft;
            slider = StyledSlider(box, name + "Slider", Top, new Vector2(190f, y), new Vector2(400f, 34f));
            slider.gameObject.AddComponent<SelectOnHover>();
            return label;
        }

        // Tabla de controles de teclado y mando (los de HumanBrain, ControlSwitcher y PauseMenu). Devuelve "Tornar"
        internal static Button Controls(Transform page, Vector2 anchor)
        {
            var box = Box(page, "Box", anchor, new Vector2(1200f, 700f));
            Heading(box, "Controls");
            ControlsColumn(box, "Actions", -360f, "Acció", "Nedar", "Passar / recuperar", "Xutar (2 cops, al verd)", "Habilitat", "Esprint", "Nedar lent (mantenir)", "Canviar de jugador", "Pausa");
            ControlsColumn(box, "Keyboard", 60f, "Teclat", "W A S D", "J", "K + K", "L", "Shift", "Espai", "Q", "Esc / P");
            ControlsColumn(box, "Gamepad", 400f, "Comandament", "Joystick esquerre", "A", "B + B", "Y", "RB", "LB", "LT", "Start");
            return MenuButton<Button>(box, "BackButton", "Tornar", BoxButtonFont, Top, new Vector2(0f, -625f), BoxButtonSize);
        }

        // Pregunta de confirmación con "Sí" y "No". La pregunta puede quedar vacía si se escribe al usarla
        internal static ConfirmWidgets Confirm(Transform page, Vector2 anchor, string question, string warning)
        {
            var box = Box(page, "Box", anchor, new Vector2(900f, 340f));
            var w = new ConfirmWidgets
            {
                Question = Label(box, "Question", 42, Top, new Vector2(0f, -40f), new Vector2(820f, 60f)),
                Yes = MenuButton<Button>(box, "YesButton", "Sí", BoxButtonFont, Top, new Vector2(-170f, -240f), ConfirmButtonSize),
                No = MenuButton<Button>(box, "NoButton", "No", BoxButtonFont, Top, new Vector2(170f, -240f), ConfirmButtonSize),
            };
            w.Question.text = question;
            var note = Label(box, "Warning", 30, Top, new Vector2(0f, -110f), new Vector2(820f, 44f), true);
            note.text = warning;
            note.color = SoftText;
            return w;
        }

        // Columna de la tabla de controles: título en amarillo y una línea por acción
        static void ControlsColumn(Transform box, string name, float x, string title, params string[] rows)
        {
            var heading = Label(box, name + "Title", 34, Top, new Vector2(x, -115f), new Vector2(400f, 50f));
            heading.text = title;
            heading.color = Yellow;
            var column = Label(box, name, 30, Top, new Vector2(x, -175f), new Vector2(400f, 400f), true);
            column.alignment = TextAnchor.UpperCenter;
            column.lineSpacing = ControlsLineSpacing;
            column.text = string.Join("\n", rows);
        }
    }
}

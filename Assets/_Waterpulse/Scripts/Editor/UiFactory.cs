using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Audio;
using Waterpulse.UI;

namespace Waterpulse.EditorTools
{
    // Piezas de interfaz compartidas por los generadores, con el estilo "retransmisión deportiva" de todo el juego:
    // fuente Barlow Condensed (títulos, botones, números) y Barlow (textos), paneles azul marino translúcidos con
    // esquinas redondeadas, borde fino y franja volt (verde lima) arriba, y botones oscuros que se vuelven volt (con el texto
    // oscuro) al seleccionarlos. Las formas salen de UiSpriteGenerator; sin ellas o sin las fuentes, todo sigue funcionando
    public static class UiFactory
    {
        public const string DisplayFontPath = MatchSceneBuilder.RootDir + "/Art/Fonts/BarlowCondensed-Bold.ttf";
        public const string BodyFontPath = MatchSceneBuilder.RootDir + "/Art/Fonts/Barlow-SemiBold.ttf";

        // Paleta
        public static readonly Color Navy = new Color(0.04f, 0.08f, 0.16f);
        public static readonly Color Volt = new Color(0.83f, 1f, 0.25f);          // acento y energía (#D4FF3F)
        public static readonly Color HomeBlue = new Color(0.12f, 0.38f, 0.95f);   // relleno de los deslizadores
        public static readonly Color LocalColor = new Color(0.95f, 0.96f, 0.98f); // local en el HUD (#F2F6FA)
        public static readonly Color RivalColor = new Color(1f, 0.42f, 0.35f);    // visitante en el HUD (#FF6B5A)
        public static readonly Color DarkText = new Color(0.05f, 0.09f, 0.18f);
        public static readonly Color SoftText = new Color(0.78f, 0.84f, 0.92f);
        public static readonly Color EnergyColor = Volt;
        public static readonly Color EnergyIdleColor = new Color(0.6f, 0.72f, 0.2f);   // energía sin llegar al coste: volt apagado
        public static readonly Color EnergyGlowColor = new Color(Volt.r, Volt.g, Volt.b, 0.4f);   // resplandor con la habilidad lista
        public static readonly Color StaminaLineColor = new Color(1f, 1f, 1f, 0.75f);   // estamina: línea fina blanca
        public static readonly Color HairlineColor = new Color(1f, 1f, 1f, 0.14f);       // separadores finos
        public static readonly Color PanelColor = new Color(0.02f, 0.05f, 0.1f, 0.82f);   // capa que tapa el partido
        static readonly Color BoxColor = new Color(0.04f, 0.08f, 0.16f, 0.9f);
        static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.14f);
        static readonly Color ButtonNormal = new Color(0.1f, 0.18f, 0.34f);
        static readonly Color ButtonHighlight = new Color(0.16f, 0.28f, 0.5f);
        static readonly Color ButtonPressed = new Color(0.07f, 0.12f, 0.24f);
        static readonly Color DisabledTint = new Color(0.3f, 0.33f, 0.38f, 0.8f);
        static readonly Color KnobHighlight = new Color(0.85f, 0.9f, 1f);
        static readonly Color KnobPressed = new Color(0.75f, 0.75f, 0.75f);
        static readonly Color Red = new Color(0.62f, 0.12f, 0.16f);
        static readonly Color RedHighlight = new Color(0.8f, 0.2f, 0.22f);
        static readonly Color RedPressed = new Color(0.45f, 0.08f, 0.1f);
        static readonly Color FieldNormal = new Color(0.07f, 0.13f, 0.25f);
        static readonly Color FieldHighlight = new Color(0.12f, 0.21f, 0.38f);
        static readonly Color FieldSelected = new Color(0.15f, 0.27f, 0.48f);
        static readonly Color PlaceholderText = new Color(0.6f, 0.66f, 0.75f);
        static readonly Color TextOutline = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color ButtonShadowColor = new Color(0f, 0f, 0f, 0.35f);
        static readonly Vector2 TextOutlineDistance = new Vector2(1.5f, -1.5f);
        static readonly Vector2 ButtonShadow = new Vector2(0f, -4f);
        static readonly Vector2 FieldPadding = new Vector2(20f, 6f);
        const float AccentHeight = 5f;     // franja volt de arriba de los paneles
        const float AccentInset = 18f;
        const int CaretWidth = 3;
        const float TintFadeDuration = 0.08f;
        const string KnobSprite = "UI/Skin/Knob.psd";

        static Font displayFont;
        static Font bodyFont;

        // Barlow Condensed para casi todo; Barlow (body) para textos largos o pequeños. Sin las fuentes, la de Unity
        public static Font DisplayFont => displayFont != null ? displayFont : displayFont = LoadFont(DisplayFontPath);
        public static Font BodyFont => bodyFont != null ? bodyFont : bodyFont = LoadFont(BodyFontPath);

        static Font LoadFont(string path)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(path);
            return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Text Label(Transform parent, string name, int size, Vector2 anchor, Vector2 position, Vector2 dimensions, bool body = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.font = body ? BodyFont : DisplayFont;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = TextOutline;
            outline.effectDistance = TextOutlineDistance;
            return text;
        }

        // Botón con el estilo del juego: oscuro con borde fino; seleccionado, volt con el texto oscuro.
        // T permite usar variantes de Button (por ejemplo, PressButton)
        public static T StyledButton<T>(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 position, Vector2 size) where T : Button
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Shadow));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, size);
            var image = go.GetComponent<Image>();
            Rounded(image, Color.white);   // blanca: el color lo pone el tinte del botón
            var shadow = go.GetComponent<Shadow>();
            shadow.effectDistance = ButtonShadow;
            shadow.effectColor = ButtonShadowColor;
            var button = go.AddComponent<T>();
            button.targetGraphic = image;
            button.colors = Tint(ButtonNormal, ButtonHighlight, ButtonPressed);
            go.AddComponent<UiSound>();
            Border(go.transform);
            var label = Label(go.transform, "Label", fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            label.text = text;
            var textColor = go.AddComponent<SelectionTextColor>();
            MatchSceneBuilder.SetRef(textColor, "label", label);
            return button;
        }

        // Deslizador con el estilo del juego: carril en cápsula, relleno azul y tirador blanco que se vuelve volt al seleccionarlo
        public static Slider StyledSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources { knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSprite) });
            go.name = name;
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, size);

            var slider = go.GetComponent<Slider>();
            slider.colors = Tint(Color.white, KnobHighlight, KnobPressed);
            go.AddComponent<UiSound>();
            float trackHeight = size.y * 0.5f;   // el carril y el relleno ocupan la mitad central del deslizador
            Pill(go.transform.Find("Background").GetComponent<Image>(), TrackColor, trackHeight);
            Pill(slider.fillRect.GetComponent<Image>(), HomeBlue, trackHeight);

            // Tirador redondo (tan ancho como alto) que no se sale del carril; el relleno llega hasta él
            float knob = size.y;
            slider.handleRect.sizeDelta = new Vector2(knob, 0f);
            ((RectTransform)slider.handleRect.parent).sizeDelta = new Vector2(-knob, 0f);
            var fillArea = (RectTransform)slider.fillRect.parent;
            fillArea.anchoredPosition = Vector2.zero;
            fillArea.sizeDelta = new Vector2(-knob, 0f);
            slider.fillRect.sizeDelta = new Vector2(knob, 0f);
            return slider;
        }

        // Botón de una acción que borra algo: rojo en lugar de azul (seleccionado, volt como los demás)
        public static void MakeDanger(Button button) => button.colors = Tint(Red, RedHighlight, RedPressed);

        // Campo de texto con el estilo del juego: fondo azul oscuro (más claro al seleccionarlo), texto blanco y una pista gris.
        // Sin navegación: con el Input System, escribir W, A, S o D movería la selección a otro botón
        public static InputField StyledInputField(Transform parent, string name, string placeholder, int fontSize, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = DefaultControls.CreateInputField(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, size);
            Rounded(go.GetComponent<Image>(), Color.white);   // el color lo pone el tinte
            Border(go.transform);

            var input = go.GetComponent<InputField>();
            var colors = Tint(FieldNormal, FieldHighlight, FieldSelected);
            colors.selectedColor = FieldSelected;
            input.colors = colors;
            input.navigation = new Navigation { mode = Navigation.Mode.None };
            input.lineType = InputField.LineType.SingleLine;
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.caretWidth = CaretWidth;

            foreach (var text in new[] { input.textComponent, (Text)input.placeholder })
            {
                text.font = BodyFont;
                text.fontSize = fontSize;
                text.alignment = TextAnchor.MiddleLeft;
                var rt = text.rectTransform;
                rt.offsetMin = FieldPadding;
                rt.offsetMax = -FieldPadding;
            }
            input.textComponent.color = Color.white;
            var hint = (Text)input.placeholder;
            hint.text = placeholder;
            hint.color = PlaceholderText;
            return input;
        }

        // Grupo que ocupa todo su padre; su CanvasGroup lo muestra, lo oculta o lo funde entero
        public static CanvasGroup StretchedGroup(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            return go.GetComponent<CanvasGroup>();
        }

        // Imagen de un color que cubre todo su padre y no bloquea los clics
        public static Image StretchedImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // Panel de retransmisión: azul marino translúcido, esquinas redondeadas, borde fino y (si accent) franja volt arriba
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 size, bool accent = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, anchor, Vector2.zero, size);
            var image = go.GetComponent<Image>();
            Rounded(image, BoxColor);
            image.raycastTarget = false;
            Border(rt);
            if (accent) Accent(rt, Volt);
            return rt;
        }

        // Rectángulo redondeado de un color (bloques del marcador, cajas de las teclas…)
        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, size);
            var image = go.GetComponent<Image>();
            Rounded(image, color);
            image.raycastTarget = false;
            return image;
        }

        // Barra en cápsula: carril claro y relleno de color. Devuelve el relleno: su anchorMax.x (0-1) es el valor
        public static RectTransform Bar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var track = new GameObject(name, typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            Place(track.GetComponent<RectTransform>(), anchor, position, size);
            Pill(track.GetComponent<Image>(), TrackColor, size.y);
            var fill = StretchedImage(track.transform, "Fill", color);
            Pill(fill, color, size.y);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            return fill.rectTransform;
        }

        // Resplandor en cápsula detrás de una barra (devuelta por Bar): sobresale "spread" px por cada lado.
        // Se crea apagado (alfa 0); quien lo usa lo enciende
        public static Image Glow(RectTransform fill, float spread, Color color)
        {
            var track = (RectTransform)fill.parent;
            var go = new GameObject(track.name + "Glow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(track.parent, false);
            go.transform.SetSiblingIndex(track.GetSiblingIndex());   // justo detrás de la barra
            var rt = go.GetComponent<RectTransform>();
            Place(rt, track.anchorMin, track.anchoredPosition, track.sizeDelta + Vector2.one * spread * 2f);
            var image = go.GetComponent<Image>();
            Pill(image, new Color(color.r, color.g, color.b, 0f), rt.sizeDelta.y);
            image.raycastTarget = false;
            return image;
        }

        // Icono de UiSpriteGenerator teñido de un color
        public static Image Icon(Transform parent, string name, string sprite, Vector2 anchor, Vector2 position, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, new Vector2(size, size));
            var image = go.GetComponent<Image>();
            image.sprite = UiSpriteGenerator.Load(sprite);
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // Franja de color pegada al borde de arriba de un panel
        public static Image Accent(Transform panel, Color color)
        {
            var go = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(AccentInset, -AccentHeight);
            rt.offsetMax = new Vector2(-AccentInset, 0f);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Border(Transform parent)
        {
            var ring = StretchedImage(parent, "Border", BorderColor);
            ring.sprite = UiSpriteGenerator.Load(UiSpriteGenerator.Ring);
            ring.type = Image.Type.Sliced;
            if (ring.sprite == null) ring.enabled = false;   // sin la forma, un borde cuadrado taparía el panel
        }

        static void Rounded(Image image, Color color)
        {
            image.sprite = UiSpriteGenerator.Load(UiSpriteGenerator.Rounded);
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        // Cápsula: los bordes del 9-slice se escalan para que el extremo sea un semicírculo de la altura de la barra
        static void Pill(Image image, Color color, float height)
        {
            image.sprite = UiSpriteGenerator.Load(UiSpriteGenerator.Pill);
            image.type = Image.Type.Sliced;
            image.color = color;
            if (image.sprite != null && height > 0f) image.pixelsPerUnitMultiplier = image.sprite.rect.height / height;
        }

        static ColorBlock Tint(Color normal, Color highlighted, Color pressed)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = Volt;
            colors.disabledColor = DisabledTint;
            colors.fadeDuration = TintFadeDuration;
            return colors;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}

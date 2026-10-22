using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Waterpulse.EditorTools
{
    // Piezas de interfaz compartidas por los generadores, para que el menú y el partido tengan el mismo estilo:
    // texto blanco con contorno negro, botones azules (el gorro del local) que se vuelven amarillos al seleccionarlos
    // (el amarillo de los avisos del HUD) y paneles oscuros semitransparentes como el de la pantalla final
    internal static class UiFactory
    {
        public static readonly Color Blue = new Color(0.1f, 0.3f, 0.95f);
        public static readonly Color Yellow = new Color(1f, 0.85f, 0.2f);
        public static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.75f);
        static readonly Color BlueHighlight = new Color(0.25f, 0.45f, 1f);
        static readonly Color BluePressed = new Color(0.05f, 0.18f, 0.6f);
        static readonly Color DisabledTint = new Color(0.35f, 0.38f, 0.42f, 0.85f);
        static readonly Color SliderTrack = new Color(0.05f, 0.1f, 0.2f, 0.9f);
        static readonly Color KnobHighlight = new Color(0.85f, 0.9f, 1f);
        static readonly Color KnobPressed = new Color(0.75f, 0.75f, 0.75f);
        static readonly Vector2 ButtonShadow = new Vector2(0f, -6f);
        const float TintFadeDuration = 0.08f;
        const string KnobSprite = "UI/Skin/Knob.psd";

        public static Text Label(Transform parent, string name, int size, Vector2 anchor, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            return text;
        }

        // Botón con el estilo del juego. T permite usar variantes de Button (por ejemplo, PressButton)
        public static T StyledButton<T>(Transform parent, string name, string text, int fontSize, Vector2 anchor, Vector2 position, Vector2 size) where T : Button
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Shadow));
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, size);
            go.GetComponent<Shadow>().effectDistance = ButtonShadow;
            var button = go.AddComponent<T>();
            button.targetGraphic = go.GetComponent<Image>();   // imagen blanca: el color lo pone el tinte del botón
            button.colors = Tint(Blue, BlueHighlight, BluePressed);
            var label = Label(go.transform, "Label", fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            label.text = text;
            return button;
        }

        // Deslizador con el estilo del juego: carril oscuro, relleno azul y tirador blanco que se vuelve amarillo al seleccionarlo
        public static Slider StyledSlider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources { knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobSprite) });
            go.name = name;
            go.transform.SetParent(parent, false);
            Place(go.GetComponent<RectTransform>(), anchor, position, size);

            var slider = go.GetComponent<Slider>();
            slider.colors = Tint(Color.white, KnobHighlight, KnobPressed);
            go.transform.Find("Background").GetComponent<Image>().color = SliderTrack;
            slider.fillRect.GetComponent<Image>().color = Blue;

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

        // Panel oscuro semitransparente, como el de la pantalla final
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            Place(rt, anchor, Vector2.zero, size);
            var image = go.GetComponent<Image>();
            image.color = PanelColor;
            image.raycastTarget = false;
            return rt;
        }

        static ColorBlock Tint(Color normal, Color highlighted, Color pressed)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = normal;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = Yellow;
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

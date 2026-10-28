using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Waterpulse.UI
{
    // Color del texto de un botón según su estado: oscuro (y sin contorno) cuando está seleccionado, porque el fondo se
    // vuelve volt; claro en el resto; apagado si está desactivado
    [RequireComponent(typeof(Selectable))]
    public class SelectionTextColor : MonoBehaviour
    {
        [SerializeField] Text label;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color selectedColor = new Color(0.05f, 0.09f, 0.18f);
        [SerializeField] Color disabledColor = new Color(0.7f, 0.74f, 0.8f);

        Selectable selectable;
        Outline outline;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            outline = label != null ? label.GetComponent<Outline>() : null;
        }

        void LateUpdate()
        {
            if (label == null) return;
            var events = EventSystem.current;
            bool selected = events != null && events.currentSelectedGameObject == gameObject && selectable.IsInteractable();
            var color = !selectable.IsInteractable() ? disabledColor : selected ? selectedColor : normalColor;
            if (label.color != color) label.color = color;
            if (outline != null && outline.enabled == selected) outline.enabled = !selected;
        }
    }
}

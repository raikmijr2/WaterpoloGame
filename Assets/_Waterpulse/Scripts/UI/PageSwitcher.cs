using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Waterpulse.UI
{
    // Cambia entre las pantallas de un menú (lo usan el menú principal y la pausa): solo se ve la actual, aparece con un
    // fundido y durante un momento ignora la entrada, para que la misma pulsación no active el primer botón.
    // Mantiene siempre algo seleccionado para que el teclado y el mando sigan funcionando aunque se haga clic en el vacío.
    // Usa tiempo sin escalar: funciona con el partido en pausa
    public class PageSwitcher
    {
        readonly CanvasGroup[] pages;
        readonly float fadeDuration;
        readonly float inputGuard;
        Selectable[] candidates;
        GameObject lastSelected;
        float shownAt;

        public PageSwitcher(CanvasGroup[] pages, float fadeDuration, float inputGuard)
        {
            this.pages = pages;
            this.fadeDuration = fadeDuration;
            this.inputGuard = inputGuard;
        }

        public CanvasGroup Current { get; private set; }

        // Muestra una pantalla. Cuando pasa la espera se selecciona el primero de "select" que se pueda usar
        // (se decide entonces porque los botones de una pantalla oculta no cuentan como activos)
        public void Show(CanvasGroup page, params Selectable[] select)
        {
            Current = page;
            foreach (var p in pages) p.gameObject.SetActive(p == page);
            page.alpha = 0f;
            page.blocksRaycasts = false;   // ni clics ni selección hasta que pase la espera
            candidates = select;
            lastSelected = null;
            SetSelected(null);
            shownAt = Time.unscaledTime;
        }

        public void HideAll()
        {
            Current = null;
            foreach (var p in pages) p.gameObject.SetActive(false);
            candidates = null;
            lastSelected = null;
            SetSelected(null);
        }

        // Llamar cada frame. Devuelve true cuando la pantalla actual ya acepta entrada
        public bool Tick()
        {
            if (Current == null) return false;
            float elapsed = Time.unscaledTime - shownAt;
            Current.alpha = fadeDuration > 0f ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;
            if (elapsed < inputGuard) return false;

            if (!Current.blocksRaycasts)
            {
                Current.blocksRaycasts = true;
                var first = candidates != null ? FirstUsable(candidates) : null;
                SetSelected(first != null ? first.gameObject : null);
            }
            KeepSelection();
            return true;
        }

        // El primero de la lista que se puede usar (existe, está activo y se puede pulsar)
        public static Selectable FirstUsable(params Selectable[] options)
        {
            foreach (var s in options)
                if (s != null && s.isActiveAndEnabled && s.IsInteractable()) return s;
            return null;
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

        static void SetSelected(GameObject target)
        {
            var events = EventSystem.current;
            if (events != null) events.SetSelectedGameObject(target);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Waterpulse.UI
{
    // Al pasar el ratón por encima, el elemento queda seleccionado: el ratón y el teclado o el mando resaltan lo mismo
    [RequireComponent(typeof(Selectable))]
    public class SelectOnHover : MonoBehaviour, IPointerEnterHandler
    {
        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable.IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}

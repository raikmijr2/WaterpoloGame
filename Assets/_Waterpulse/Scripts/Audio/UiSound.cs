using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Waterpulse.Audio
{
    // Sonidos de un botón o deslizador: "moverse" al navegar con teclado o mando o al pasar el ratón por encima,
    // y "confirmar" al pulsarlo. Lo añaden los generadores a cada elemento con el estilo del juego
    [RequireComponent(typeof(Selectable))]
    public class UiSound : MonoBehaviour, IPointerEnterHandler, IMoveHandler, ISubmitHandler, IPointerClickHandler
    {
        Selectable selectable;

        void Awake() => selectable = GetComponent<Selectable>();

        bool Usable => selectable != null && selectable.IsInteractable();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Usable) AudioDirector.PlayUi(UiSoundKind.Move);
        }

        public void OnMove(AxisEventData eventData)
        {
            if (Usable) AudioDirector.PlayUi(UiSoundKind.Move);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (Usable) AudioDirector.PlayUi(UiSoundKind.Confirm);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && Usable) AudioDirector.PlayUi(UiSoundKind.Confirm);
        }
    }
}

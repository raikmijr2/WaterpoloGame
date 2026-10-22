using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Waterpulse.UI
{
    // Botón que se activa al pulsar, no al soltar. En Web el navegador solo concede la pantalla completa dentro de un
    // gesto del usuario y Unity hace la petición en el siguiente evento: si se pide al pulsar, se concede al soltar.
    // Con teclado o mando funciona igual que un Button (Enter y A ya actúan al pulsar)
    public class PressButton : Button
    {
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
                onClick.Invoke();
        }

        // Al soltar ya no hace nada: el botón se ha activado al pulsar
        public override void OnPointerClick(PointerEventData eventData) { }
    }
}

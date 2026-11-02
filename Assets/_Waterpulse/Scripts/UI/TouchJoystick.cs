using UnityEngine;
using UnityEngine.EventSystems;
using Waterpulse.Core;

namespace Waterpulse.UI
{
    // Joystick táctil dinámico: va en una zona invisible (la mitad izquierda de la pantalla). Donde se pone el dedo
    // aparece la base, y arrastrar mueve el mando hasta "range" px; la inclinación va a TouchInput.Move
    // (poca inclinación = nadar lento). Al soltar, desaparece y el jugador deja de nadar
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Tooltip("Base del joystick (aparece donde se toca)")]
        [SerializeField] RectTransform stickBase;
        [Tooltip("Mando que sigue al dedo dentro de la base")]
        [SerializeField] RectTransform knob;
        [Tooltip("Lo que se puede alejar el mando del centro (px de referencia)")]
        [SerializeField] float range = 110f;

        Canvas canvas;
        int pointerId = int.MinValue;
        Vector2 origin;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            stickBase.gameObject.SetActive(false);
        }

        void OnDisable() => Release();

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;   // solo un dedo
            pointerId = e.pointerId;
            origin = e.position;
            stickBase.position = origin;
            knob.position = origin;
            stickBase.gameObject.SetActive(true);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 offset = Vector2.ClampMagnitude(e.position - origin, range * scale);
            knob.position = origin + offset;
            TouchInput.Move = offset / (range * scale);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) Release();
        }

        void Release()
        {
            pointerId = int.MinValue;
            TouchInput.Move = Vector2.zero;
            if (stickBase != null) stickBase.gameObject.SetActive(false);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Waterpulse.UI
{
    // Botón táctil: avisa al tocarlo (sin esperar a soltar, para que responda rápido) y se encoge un poco mientras
    // se mantiene. Lo que hace lo decide TouchControls según la jugada; aquí solo están su texto y su aspecto
    public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Text label;
        [Tooltip("Tamaño mientras se mantiene pulsado (1 = normal)")]
        [SerializeField] float pressedScale = 0.9f;

        public event Action Pressed;

        public void SetText(string text)
        {
            if (label.text != text) label.text = text;
        }

        public void OnPointerDown(PointerEventData e)
        {
            transform.localScale = Vector3.one * pressedScale;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData e) => transform.localScale = Vector3.one;
    }
}

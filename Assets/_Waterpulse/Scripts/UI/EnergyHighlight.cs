using UnityEngine;
using UnityEngine.UI;

namespace Waterpulse.UI
{
    // Resalta un poco una barra de energía cuando el jugador puede usar su habilidad: el relleno pasa de volt apagado
    // a volt y se enciende un resplandor suave detrás. Sin más avisos: el jugador lo entiende al verlo.
    // Lo usan la mini placa sobre el jugador (PlayerPlate) y el panel de abajo a la izquierda (PlayerHudView)
    public class EnergyHighlight : MonoBehaviour
    {
        [SerializeField] Image fill;
        [Tooltip("Resplandor detrás de la barra (su color sin alfa; el alfa lo pone este componente)")]
        [SerializeField] Image glow;
        [SerializeField] Color idleColor = new Color(0.6f, 0.72f, 0.2f);
        [SerializeField] Color readyColor = new Color(0.83f, 1f, 0.25f);
        [Tooltip("Opacidad del resplandor con la habilidad lista")]
        [Range(0f, 1f)] [SerializeField] float glowAlpha = 0.4f;
        [Tooltip("Lo que tarda en encenderse o apagarse (s)")]
        [SerializeField] float fadeDuration = 0.25f;

        bool ready;
        float amount;   // 0 = apagado, 1 = lista

        public void SetReady(bool value) => ready = value;

        // Sin transición (al cambiar de jugador)
        public void Snap(bool value)
        {
            ready = value;
            amount = value ? 1f : 0f;
            Apply();
        }

        void Update()
        {
            float target = ready ? 1f : 0f;
            if (Mathf.Approximately(amount, target)) return;
            amount = fadeDuration > 0f ? Mathf.MoveTowards(amount, target, Time.deltaTime / fadeDuration) : target;
            Apply();
        }

        void Apply()
        {
            fill.color = Color.Lerp(idleColor, readyColor, amount);
            var c = glow.color;
            glow.color = new Color(c.r, c.g, c.b, glowAlpha * amount);
        }
    }
}

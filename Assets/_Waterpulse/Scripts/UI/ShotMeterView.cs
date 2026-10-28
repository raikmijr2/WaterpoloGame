using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Barra de tiro vertical al lado del jugador del humano mientras carga el tiro: se llena de abajo arriba
    // y la zona verde marca dónde hay que volver a pulsar K
    public class ShotMeterView : MonoBehaviour
    {
        [SerializeField] BallController ball;
        [SerializeField] Camera viewCamera;
        [SerializeField] TeamSide humanTeam = TeamSide.Home;
        [SerializeField] RectTransform bar;
        [SerializeField] RectTransform greenZone;
        [SerializeField] RectTransform fill;
        [Tooltip("Punto del jugador sobre el que se coloca la barra (m por encima del agua)")]
        [SerializeField] float worldHeight = 1f;
        [Tooltip("Separación de la barra respecto al jugador en pantalla (px de referencia)")]
        [SerializeField] Vector2 screenOffset = new Vector2(70f, 0f);

        Canvas canvas;

        void Awake() => canvas = GetComponentInParent<Canvas>();

        void LateUpdate()
        {
            var holder = ball.Holder;
            bool show = holder != null && holder.Team == humanTeam && holder.IsChargingShot;
            if (bar.gameObject.activeSelf != show) bar.gameObject.SetActive(show);
            if (!show) return;

            var meter = holder.ShotMeter;
            greenZone.anchorMin = new Vector2(0f, meter.GreenStart);
            greenZone.anchorMax = new Vector2(1f, meter.GreenEnd);
            fill.anchorMax = new Vector2(1f, meter.Fill);

            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector3 screen = viewCamera.WorldToScreenPoint(holder.Position + Vector3.up * worldHeight);
            bar.position = screen + (Vector3)(screenOffset * scale);
        }
    }
}

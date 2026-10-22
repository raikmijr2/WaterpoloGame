using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Flecha en el borde de la pantalla hacia la portería rival cuando la cámara no la enseña
    // (pilar 3 del GDD: siempre se tiene que saber dónde está la portería)
    public class GoalIndicator : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] PoolBounds pool;
        [SerializeField] Camera viewCamera;
        [SerializeField] TeamSide humanTeam = TeamSide.Home;
        [SerializeField] RectTransform indicator;
        [SerializeField] Text label;
        [Tooltip("Separación del borde de la pantalla (fracción del viewport)")]
        [SerializeField] float edgeMargin = 0.06f;
        [SerializeField] string rightText = "PORTERIA ►";
        [SerializeField] string leftText = "◄ PORTERIA";

        void LateUpdate()
        {
            Vector3 goal = pool.AttackedGoalCenter(humanTeam) + Vector3.up * (config.goalHeight * 0.5f);
            Vector3 vp = viewCamera.WorldToViewportPoint(goal);
            bool visible = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            if (indicator.gameObject.activeSelf == visible) indicator.gameObject.SetActive(!visible);
            if (visible) return;

            bool right = vp.x > 0.5f;
            float x = right ? 1f - edgeMargin : edgeMargin;
            float y = Mathf.Clamp(vp.y, edgeMargin, 1f - edgeMargin);
            indicator.position = new Vector3(x * Screen.width, y * Screen.height, 0f);
            indicator.pivot = new Vector2(right ? 1f : 0f, 0.5f);
            label.text = right ? rightText : leftText;
            label.alignment = right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;

namespace Waterpulse.UI
{
    // Al marcar: un destello que cubre la pantalla y se apaga enseguida, y el bàner de "GOL!" entra grande y se asienta.
    // Solo aspecto; el texto del bàner lo pone ScoreboardView
    public class GoalFlashView : MonoBehaviour
    {
        [Tooltip("Imagen a pantalla completa del destello (transparente en reposo)")]
        [SerializeField] Image flash;
        [Tooltip("Bàner de los avisos (\"GOL!\")")]
        [SerializeField] RectTransform banner;
        [SerializeField] Color flashColor = new Color(0.83f, 1f, 0.25f);
        [Tooltip("Opacidad del destello al empezar")]
        [Range(0f, 1f)] [SerializeField] float flashAlpha = 0.45f;
        [Tooltip("Lo que tarda en apagarse el destello (s, tiempo real)")]
        [SerializeField] float flashDuration = 0.35f;
        [Tooltip("Tamaño extra del bàner al entrar (fracción) y lo que tarda en asentarse (s)")]
        [SerializeField] float bannerPunch = 0.45f;
        [SerializeField] float bannerPunchDuration = 0.5f;

        float timer = float.MaxValue;

        void Awake() => SetFlash(0f);

        void OnEnable() => GameEvents.Goal += OnGoal;
        void OnDisable() => GameEvents.Goal -= OnGoal;

        void OnGoal(TeamSide team, GameObject scorer) => timer = 0f;

        void Update()
        {
            if (timer > Mathf.Max(flashDuration, bannerPunchDuration)) return;
            timer += Time.unscaledDeltaTime;
            float flashT = flashDuration > 0f ? Mathf.Clamp01(timer / flashDuration) : 1f;
            SetFlash(flashAlpha * (1f - flashT) * (1f - flashT));
            float punchT = bannerPunchDuration > 0f ? Mathf.Clamp01(timer / bannerPunchDuration) : 1f;
            // Rebote: grande al entrar, un poco por debajo y se asienta en 1
            float scale = 1f + bannerPunch * Mathf.Cos(punchT * Mathf.PI * 1.5f) * (1f - punchT);
            banner.localScale = Vector3.one * scale;
        }

        void SetFlash(float alpha)
        {
            flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            flash.enabled = alpha > 0.001f;
        }
    }
}

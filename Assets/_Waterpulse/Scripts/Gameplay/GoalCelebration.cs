using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Celebración del gol, solo aspecto (estilo Rocket League): cañones de confeti en las cuatro esquinas de la piscina con
    // los colores del equipo que marca (el confeti cae como papel, se queda flotando en el agua y se desvanece) y el público
    // de las gradas salta más alto y más rápido unos segundos. El temblor y el zoom de la cámara los hace MatchCamera, el
    // sonido AudioDirector y el destello de pantalla GoalFlashView
    public class GoalCelebration : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [Tooltip("Cañones de confeti (uno por esquina)")]
        [SerializeField] ParticleSystem[] cannons;
        [Tooltip("Gradas con el público (opcional)")]
        [SerializeField] Gradas stands;
        [Tooltip("Colores del confeti cuando marca el local")]
        [SerializeField] Color[] homeColors = { new Color(0.95f, 0.96f, 0.98f), new Color(0.83f, 1f, 0.25f), new Color(0.12f, 0.38f, 0.95f) };
        [Tooltip("Colores del confeti cuando marca el visitante")]
        [SerializeField] Color[] awayColors = { new Color(1f, 0.42f, 0.35f), new Color(0.95f, 0.96f, 0.98f), new Color(1f, 0.8f, 0.2f) };

        float hypeTimer;

        void OnEnable() => GameEvents.Goal += OnGoal;
        void OnDisable() => GameEvents.Goal -= OnGoal;

        void OnGoal(TeamSide team, GameObject scorer)
        {
            var colors = new ParticleSystem.MinMaxGradient(Palette(team == TeamSide.Home ? homeColors : awayColors))
            {
                mode = ParticleSystemGradientMode.RandomColor,
            };
            foreach (var cannon in cannons)
            {
                var main = cannon.main;
                main.startColor = colors;
                cannon.Play();
            }
            hypeTimer = config.goalCrowdHypeDuration;
        }

        void Update()
        {
            if (stands == null) return;
            if (hypeTimer > 0f) hypeTimer -= Time.unscaledDeltaTime;
            // De todo el empuje al principio a nada al final
            float hype = config.goalCrowdHypeDuration > 0f ? Mathf.Clamp01(hypeTimer / config.goalCrowdHypeDuration) : 0f;
            stands.JumpBoost = Mathf.Lerp(1f, config.goalCrowdJumpBoost, hype);
            stands.SpeedBoost = Mathf.Lerp(1f, config.goalCrowdSpeedBoost, hype);
        }

        // Gradiente de colores fijos: el confeti sale de uno de ellos al azar, sin mezclas
        static Gradient Palette(Color[] colors)
        {
            var keys = new GradientColorKey[colors.Length];
            for (int i = 0; i < colors.Length; i++) keys[i] = new GradientColorKey(colors[i], (i + 1f) / colors.Length);
            var gradient = new Gradient { mode = GradientMode.Fixed };
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }
    }
}

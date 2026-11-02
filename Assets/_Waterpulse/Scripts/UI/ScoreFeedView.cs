using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using Waterpulse.Core;
using Waterpulse.Gameplay;
using Waterpulse.Logic;

namespace Waterpulse.UI
{
    // Puntos y multiplicador del equipo humano en pantalla:
    // - Debajo del marcador, "MULTIPLICADOR ×1,6": lo que vale ahora el próximo gol. Late al subir y se apaga a ×1.
    // - A la derecha, avisos cortos que suben y se desvanecen: "PASSADA +10 · ×1,1", "RATXA DE 3 PASSADES! ×1,5",
    //   "SÚPER GOL! 150 × 1,6 = +240", "CONTRAATAC +50" o, en rojo, "MULTIPLICADOR PERDUT".
    // Solo los del equipo humano (los del rival no ayudan a decidir nada)
    public class ScoreFeedView : MonoBehaviour
    {
        [SerializeField] MatchScoring scoring;
        [SerializeField] TeamSide humanTeam = TeamSide.Home;
        [SerializeField] GameObject multiplierPanel;
        [SerializeField] Text multiplierLabel;
        [Tooltip("Líneas de avisos, de abajo arriba (la primera es la más nueva)")]
        [SerializeField] Text[] lines;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color highlightColor = new Color(0.83f, 1f, 0.25f);
        [SerializeField] Color lostColor = new Color(1f, 0.35f, 0.3f);
        [Tooltip("Lo que dura cada aviso en pantalla (s, tiempo real)")]
        [SerializeField] float messageDuration = 2f;
        [Tooltip("Al final de su vida, lo que tarda en desvanecerse (s)")]
        [SerializeField] float fadeDuration = 0.5f;
        [Tooltip("Cuánto crece el multiplicador al subir (fracción de su tamaño)")]
        [SerializeField] float pulse = 0.3f;
        [Tooltip("Lo que dura el latido del multiplicador (s)")]
        [SerializeField] float pulseDuration = 0.25f;
        [Tooltip("Transparencia del multiplicador a ×1")]
        [Range(0f, 1f)] [SerializeField] float idleAlpha = 0.45f;

        struct Message
        {
            public string Text;
            public Color Color;
            public float BornAt;
        }

        readonly List<Message> messages = new List<Message>();
        float shownMultiplier = 1f;
        float pulseTimer;
        bool playing;

        void Awake()
        {
            multiplierPanel.SetActive(false);
            foreach (var line in lines) line.text = string.Empty;
        }

        void OnEnable()
        {
            scoring.Scored += OnScored;
            GameEvents.MatchStateChanged += OnMatchStateChanged;
        }

        void OnDisable()
        {
            scoring.Scored -= OnScored;
            GameEvents.MatchStateChanged -= OnMatchStateChanged;
        }

        void OnMatchStateChanged(MatchState state) => playing = state != MatchState.Results && state != MatchState.FullTime;

        void OnScored(PlayerController player, ScoreEvent e)
        {
            if (e.Team != (int)humanTeam) return;
            switch (e.Kind)
            {
                case ScoreKind.Pass: Push($"PASSADA +{e.Points}   ×{Format(e.Multiplier)}", normalColor); break;
                case ScoreKind.Recovery: Push($"ROBATORI +{e.Points}   ×{Format(e.Multiplier)}", normalColor); break;
                case ScoreKind.Save: Push($"PARADA +{e.Points}", normalColor); break;
                case ScoreKind.Streak: Push($"RATXA DE {e.Streak} PASSADES!   ×{Format(e.Multiplier)}", highlightColor); break;
                case ScoreKind.Goal:
                    Push($"{(e.WithAbility ? "SÚPER GOL!" : "GOL!")}  {e.GoalBase} × {Format(e.Multiplier)} = +{e.Points}", highlightColor);
                    break;
                case ScoreKind.CounterAttack: Push($"CONTRAATAC +{e.Points}", highlightColor); break;
                case ScoreKind.MultiplierLost: Push($"MULTIPLICADOR PERDUT (×{Format(e.Multiplier)})", lostColor); break;
            }
        }

        void Push(string text, Color color)
        {
            messages.Insert(0, new Message { Text = text, Color = color, BornAt = Time.unscaledTime });
            if (messages.Count > lines.Length) messages.RemoveAt(messages.Count - 1);
        }

        void Update()
        {
            float now = Time.unscaledTime;
            for (int i = messages.Count - 1; i >= 0; i--)
                if (now - messages[i].BornAt > messageDuration) messages.RemoveAt(i);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i >= messages.Count)
                {
                    lines[i].text = string.Empty;
                    continue;
                }
                var m = messages[i];
                float left = messageDuration - (now - m.BornAt);
                float alpha = fadeDuration > 0f ? Mathf.Clamp01(left / fadeDuration) : 1f;
                lines[i].text = m.Text;
                lines[i].color = new Color(m.Color.r, m.Color.g, m.Color.b, alpha);
            }

            if (multiplierPanel.activeSelf != playing) multiplierPanel.SetActive(playing);
            if (!playing) return;
            float multiplier = scoring.MultiplierOf(humanTeam);
            if (multiplier > shownMultiplier + 0.001f) pulseTimer = pulseDuration;
            shownMultiplier = multiplier;
            if (pulseTimer > 0f) pulseTimer -= Time.unscaledDeltaTime;
            float scale = 1f + pulse * (pulseDuration > 0f ? Mathf.Clamp01(pulseTimer / pulseDuration) : 0f);
            multiplierPanel.transform.localScale = Vector3.one * scale;
            multiplierLabel.text = $"MULTIPLICADOR ×{Format(multiplier)}";
            var c = multiplier > 1.001f ? highlightColor : normalColor;
            multiplierLabel.color = new Color(c.r, c.g, c.b, multiplier > 1.001f ? 1f : idleAlpha);
        }

        // 1,6 (coma decimal, en catalán)
        static string Format(float value) => value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
    }
}

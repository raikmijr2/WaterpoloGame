using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.UI
{
    // Pantalla final básica: resultado, ganador y "Tornar a jugar" (botón o J).
    // La versión completa con puntos, MVP y récord llega en la Fase 7
    public class ResultsScreen : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] MatchFlow match;
        [SerializeField] GameObject panel;
        [SerializeField] Text resultLabel;
        [SerializeField] Text winnerLabel;
        [SerializeField] Button restartButton;
        [SerializeField] string winsFormat = "Guanya {0}!";
        [SerializeField] string drawText = "Empat";

        InputAction restart;

        void Awake()
        {
            restart = new InputAction("Restart", InputActionType.Button);
            restart.AddBinding("<Keyboard>/j");
            restart.AddBinding("<Keyboard>/enter");
            restart.AddBinding("<Gamepad>/buttonSouth");
            restartButton.onClick.AddListener(match.Restart);
            panel.SetActive(false);
        }

        void OnEnable() => GameEvents.MatchStateChanged += OnStateChanged;

        void OnDisable()
        {
            GameEvents.MatchStateChanged -= OnStateChanged;
            restart.Disable();
        }

        void OnDestroy() => restart.Dispose();

        void Update()
        {
            if (panel.activeSelf && restart.WasPressedThisFrame()) match.Restart();
        }

        void OnStateChanged(MatchState state)
        {
            if (state != MatchState.Results) return;
            resultLabel.text = $"{config.homeTeamName}  {match.HomeGoals} - {match.AwayGoals}  {config.awayTeamName}";
            string winner = match.Winner == TeamSide.Home ? config.homeTeamName : config.awayTeamName;
            winnerLabel.text = match.IsDraw ? drawText : string.Format(winsFormat, winner);
            panel.SetActive(true);
            restart.Enable();
        }
    }
}

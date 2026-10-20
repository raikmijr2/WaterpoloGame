using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;
using Waterpulse.Gameplay;

namespace Waterpulse.Abilities
{
    // Reparte energía según los eventos del partido (GDD sección 4):
    // pase completado al pasador, recuperación al que intercepta o roba, gol al que marca
    public class EnergyRewards : MonoBehaviour
    {
        [SerializeField] EnergyConfigSO config;

        void OnEnable()
        {
            GameEvents.PassCompleted += OnPassCompleted;
            GameEvents.Interception += OnInterception;
            GameEvents.BallRecovered += OnBallRecovered;
            GameEvents.Goal += OnGoal;
        }

        void OnDisable()
        {
            GameEvents.PassCompleted -= OnPassCompleted;
            GameEvents.Interception -= OnInterception;
            GameEvents.BallRecovered -= OnBallRecovered;
            GameEvents.Goal -= OnGoal;
        }

        void OnPassCompleted(GameObject passer, GameObject receiver) => Reward(passer, config.passReward);
        void OnInterception(GameObject interceptor, GameObject passer) => Reward(interceptor, config.recoveryReward);
        void OnBallRecovered(GameObject player) => Reward(player, config.recoveryReward);

        // Un gol en propia puerta no da energía a quien la tocó por última vez
        void OnGoal(TeamSide scoringTeam, GameObject scorer)
        {
            if (scorer == null) return;
            var player = scorer.GetComponent<PlayerController>();
            if (player != null && player.Team == scoringTeam) Reward(scorer, config.goalReward);
        }

        static void Reward(GameObject player, float amount)
        {
            if (player == null) return;
            var energy = player.GetComponent<PlayerEnergy>();
            if (energy != null) energy.Add(amount);
        }
    }
}

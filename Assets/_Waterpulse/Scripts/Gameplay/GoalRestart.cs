using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Reanudación tras gol: congela la pelota y, tras una pausa, la da en el centro al equipo que ha encajado.
    // La Fase 5 integrará esto en la MatchStateMachine
    public class GoalRestart : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] BallController ball;
        [SerializeField] PoolBounds pool;

        float timer;
        bool pending;
        TeamSide conceding;

        void OnEnable() => GameEvents.Goal += OnGoal;
        void OnDisable() => GameEvents.Goal -= OnGoal;

        void OnGoal(TeamSide scoringTeam, GameObject scorer)
        {
            if (pending) return;
            ball.Freeze();
            conceding = scoringTeam.Opponent();
            timer = config.goalRestartDelay;
            pending = true;
        }

        void Update()
        {
            if (!pending) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            pending = false;
            ball.PlaceForRestart(NearestFieldPlayerToCenter(conceding));
        }

        PlayerController NearestFieldPlayerToCenter(TeamSide team)
        {
            PlayerController best = null;
            float bestDistance = float.MaxValue;
            foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (p.Team != team || p.IsGoalkeeper || !p.CanTakeBall) continue;
                float d = Vector3.Distance(p.Position, pool.Center);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = p;
                }
            }
            return best;
        }
    }
}

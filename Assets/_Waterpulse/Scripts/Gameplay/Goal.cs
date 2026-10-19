using UnityEngine;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Trigger dentro de la red: si entra la pelota, marca el equipo rival del que defiende esta portería
    [RequireComponent(typeof(BoxCollider))]
    public class Goal : MonoBehaviour
    {
        [SerializeField] TeamSide defendingTeam;

        public TeamSide DefendingTeam => defendingTeam;

        void Awake() => GetComponent<BoxCollider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var ball = other.GetComponentInParent<BallController>();
            if (ball == null || ball.IsFrozen) return;
            var scorer = ball.LastToucher != null ? ball.LastToucher.gameObject : null;
            GameEvents.RaiseGoal(defendingTeam.Opponent(), scorer);
        }
    }
}

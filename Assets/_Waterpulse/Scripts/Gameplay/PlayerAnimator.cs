using UnityEngine;

namespace Waterpulse.Gameplay
{
    // Va en el modelo, junto a su Animator: le pasa la velocidad y los disparadores del jugador y recibe el evento
    // ReleaseBall de la animación de lanzar (Unity solo busca las funciones de los eventos en el objeto del Animator)
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [Header("Parámetros del Animator")]
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string throwTrigger = "Throw";
        [SerializeField] string stealTrigger = "Steal";

        Animator animator;
        int speedId;
        int throwId;
        int stealId;

        void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;   // al jugador lo mueve su Rigidbody, no la animación
            speedId = Animator.StringToHash(speedParameter);
            throwId = Animator.StringToHash(throwTrigger);
            stealId = Animator.StringToHash(stealTrigger);
            if (player == null) player = GetComponentInParent<PlayerController>();
        }

        void OnEnable()
        {
            if (player == null) return;
            player.HasThrowAnimation = true;
            player.BallHand = animator.isHuman ? animator.GetBoneTransform(player.Config.ballHandBone) : null;
            player.ThrowStarted += OnThrowStarted;
            player.StealAttempted += OnStealAttempted;
        }

        void OnDisable()
        {
            if (player == null) return;
            player.HasThrowAnimation = false;
            player.BallHand = null;
            player.ThrowStarted -= OnThrowStarted;
            player.StealAttempted -= OnStealAttempted;
        }

        void Update() => animator.SetFloat(speedId, player.SpeedRatio);

        void OnThrowStarted() => animator.SetTrigger(throwId);
        void OnStealAttempted() => animator.SetTrigger(stealId);

        // Evento de la animación Throw: el fotograma en el que la pelota sale de la mano
        public void ReleaseBall() => player.ReleaseBall();
    }
}

using System.Collections.Generic;
using UnityEngine;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Va en el modelo, junto a su Animator: le pasa la velocidad y los disparadores del jugador y recibe el evento
    // ReleaseBall de la animación de lanzar (Unity solo busca las funciones de los eventos en el objeto del Animator).
    // Solo toca los parámetros que tiene su Animator (el del portero no tiene Steal, por ejemplo)
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [Header("Parámetros del Animator")]
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string throwTrigger = "Throw";
        [SerializeField] string stealTrigger = "Steal";
        [Tooltip("Parada del portero")]
        [SerializeField] string saveTrigger = "Save";

        Animator animator;
        int speedId;
        int throwId;
        int stealId;
        int saveId;
        readonly HashSet<int> parameters = new HashSet<int>();

        void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;   // al jugador lo mueve su Rigidbody, no la animación
            speedId = Animator.StringToHash(speedParameter);
            throwId = Animator.StringToHash(throwTrigger);
            stealId = Animator.StringToHash(stealTrigger);
            saveId = Animator.StringToHash(saveTrigger);
            foreach (var p in animator.parameters) parameters.Add(p.nameHash);
            if (player == null) player = GetComponentInParent<PlayerController>();
        }

        void OnEnable()
        {
            if (player == null) return;
            player.HasThrowAnimation = true;
            player.BallHand = animator.isHuman ? animator.GetBoneTransform(player.Config.ballHandBone) : null;
            player.ThrowStarted += OnThrowStarted;
            player.StealAttempted += OnStealAttempted;
            GameEvents.Save += OnSave;
        }

        void OnDisable()
        {
            GameEvents.Save -= OnSave;
            if (player == null) return;
            player.HasThrowAnimation = false;
            player.BallHand = null;
            player.ThrowStarted -= OnThrowStarted;
            player.StealAttempted -= OnStealAttempted;
        }

        void Update()
        {
            if (parameters.Contains(speedId)) animator.SetFloat(speedId, player.SpeedRatio);
        }

        void OnThrowStarted() => Trigger(throwId);
        void OnStealAttempted() => Trigger(stealId);

        void OnSave(GameObject goalkeeper)
        {
            if (player != null && goalkeeper == player.gameObject) Trigger(saveId);
        }

        void Trigger(int id)
        {
            if (parameters.Contains(id)) animator.SetTrigger(id);
        }

        // Evento de la animación Throw: el fotograma en el que la pelota sale de la mano
        public void ReleaseBall() => player.ReleaseBall();
    }
}

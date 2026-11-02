using UnityEngine;
using Waterpulse.Core;

namespace Waterpulse.Gameplay
{
    // Partículas de agua de un jugador, solo visuales (no cambian nada del juego):
    // - Espuma detrás mientras nada, más cuanto más rápido va (quieto o nadando lento casi no deja).
    // - Esquitxada hacia atrás y un anillo de espuma al hacer el dash (escucha DashUsed).
    // - Esquitxades laterales mientras dura el Torpede Blau (el acelerón).
    // Las salpicaduras propias de cada habilidad al activarse las lanza AbilityBehaviour
    public class PlayerWaterFx : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [Tooltip("Espuma detrás del jugador (emite en continuo; aquí se ajusta cuánto)")]
        [SerializeField] ParticleSystem wake;
        [Tooltip("Partículas por segundo de la espuma a velocidad máxima")]
        [SerializeField] float wakeRate = 28f;
        [Tooltip("Por debajo de esta fracción de la velocidad máxima no deja espuma")]
        [Range(0f, 1f)] [SerializeField] float wakeMinSpeed = 0.25f;
        [Tooltip("Ráfaga del dash (sus hijos, como el anillo de espuma, salen a la vez)")]
        [SerializeField] ParticleSystem dashBurst;
        [Tooltip("Esquitxades laterales mientras dura el acelerón del Torpede Blau")]
        [SerializeField] ParticleSystem boostSpray;
        [Tooltip("Partículas por segundo de las esquitxades del acelerón")]
        [SerializeField] float boostRate = 70f;

        void OnEnable() => GameEvents.DashUsed += OnDashUsed;
        void OnDisable() => GameEvents.DashUsed -= OnDashUsed;

        void OnDashUsed(GameObject go)
        {
            if (go == gameObject) dashBurst.Play();
        }

        void Update()
        {
            float speed = player.SpeedRatio;
            var wakeEmission = wake.emission;
            wakeEmission.rateOverTime = speed >= wakeMinSpeed ? wakeRate * Mathf.Clamp01(speed) : 0f;
            var boostEmission = boostSpray.emission;
            boostEmission.rateOverTime = player.IsBoosting ? boostRate : 0f;
        }
    }
}

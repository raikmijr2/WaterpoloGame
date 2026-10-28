using UnityEngine;
using Waterpulse.Config;

namespace Waterpulse.Gameplay
{
    // Pelota visible (hija de la física). Mientras la lleva un jugador con modelo se ve en su mano; al soltarla
    // pasa en ballReleaseBlendTime de la mano a su trayectoria. Solo visual: la pelota física no cambia
    [DefaultExecutionOrder(100)]   // después de que PlayerController mueva el modelo (saltito y subida del tiro)
    public class BallVisual : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] BallController ball;

        Vector3 offset;        // de la pelota física a la visible
        float releaseTimer;

        void LateUpdate()
        {
            Vector3 physical = ball.transform.position;
            var holder = ball.Holder;
            var hand = holder != null ? holder.BallHand : null;

            if (hand != null)
            {
                // Desplazamiento en los ejes de la mano, sin su escala (los esqueletos de Mixamo vienen escalados)
                offset = hand.position + hand.rotation * config.ballHandOffset - physical;
                releaseTimer = config.ballReleaseBlendTime;
            }
            else if (releaseTimer > 0f)
            {
                releaseTimer -= Time.deltaTime;
                if (releaseTimer <= 0f) offset = Vector3.zero;
            }
            else offset = Vector3.zero;

            float blend = hand != null || config.ballReleaseBlendTime <= 0f ? 1f : Mathf.Clamp01(releaseTimer / config.ballReleaseBlendTime);
            transform.position = physical + offset * blend;
        }
    }
}

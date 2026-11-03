using UnityEngine;
using Waterpulse.Config;

namespace Waterpulse.Gameplay
{
    // Pelota visible (hija de la física). Mientras la lleva un jugador con modelo, va en el agua delante de su cara,
    // empujada con un vaivén que crece con la velocidad; al pasar o cargar el tiro sube a su mano en ballGrabBlendTime,
    // y al soltarla pasa en ballReleaseBlendTime de la mano a su trayectoria. Solo visual: la pelota física no cambia
    [DefaultExecutionOrder(100)]   // después de que PlayerController mueva el modelo (saltito y subida del tiro)
    public class BallVisual : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] BallController ball;

        Vector3 offset;        // de la pelota física a la visible
        float releaseTimer;
        float handWeight;      // 0 = empujada en el agua, 1 = en la mano
        float surgePhase;
        PlayerController lastHolder;

        void LateUpdate()
        {
            Vector3 physical = ball.transform.position;
            var holder = ball.Holder;
            var hand = holder != null ? holder.BallHand : null;
            if (holder != lastHolder)
            {
                handWeight = 0f;
                lastHolder = holder;
            }

            if (hand != null)
            {
                float step = config.ballGrabBlendTime > 0f ? Time.deltaTime / config.ballGrabBlendTime : 1f;
                handWeight = Mathf.MoveTowards(handWeight, holder.BallInHand ? 1f : 0f, step);

                // Desplazamiento en los ejes de la mano, sin su escala (los esqueletos de Mixamo vienen escalados)
                Vector3 handOffset = hand.position + hand.rotation * config.ballHandOffset - physical;
                offset = Vector3.Lerp(DribbleOffset(holder), handOffset, handWeight);
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

        // La pelota física ya va en el agua a ballHoldDistance delante del jugador: aquí solo se ajusta la distancia
        // que se ve y se le añade el empuje de las brazadas
        Vector3 DribbleOffset(PlayerController holder)
        {
            Vector3 forward = holder.Forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) return Vector3.zero;
            forward.Normalize();

            surgePhase = Mathf.Repeat(surgePhase + Time.deltaTime * config.ballDribbleSurgeFrequency, 1f);
            float surge = Mathf.Sin(surgePhase * 2f * Mathf.PI) * config.ballDribbleSurge * Mathf.Clamp01(holder.SpeedRatio);
            return forward * (config.ballDribbleDistance - config.ballHoldDistance + surge);
        }
    }
}

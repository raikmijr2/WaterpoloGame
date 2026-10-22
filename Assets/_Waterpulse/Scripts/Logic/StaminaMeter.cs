using UnityEngine;

namespace Waterpulse.Logic
{
    // Estamina de un jugador: se recarga sola con el tiempo y la gasta el dash, que además tiene espera
    public class StaminaMeter
    {
        readonly float regenPerSecond;
        float cooldownTimer;

        public float Max { get; }
        public float Current { get; private set; }
        public float Normalized => Max > 0f ? Current / Max : 0f;
        public bool IsCoolingDown => cooldownTimer > 0f;

        public StaminaMeter(float max, float regenPerSecond)
        {
            Max = Mathf.Max(0f, max);
            Current = Max;
            this.regenPerSecond = Mathf.Max(0f, regenPerSecond);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (cooldownTimer > 0f) cooldownTimer -= dt;
            Current = Mathf.Min(Max, Current + regenPerSecond * dt);
        }

        public bool CanSpend(float cost) => !IsCoolingDown && cost >= 0f && cost <= Current;

        // Gasta y arranca la espera solo si alcanza y no está en espera
        public bool TrySpend(float cost, float cooldown)
        {
            if (!CanSpend(cost)) return false;
            Current -= cost;
            cooldownTimer = Mathf.Max(0f, cooldown);
            return true;
        }
    }
}

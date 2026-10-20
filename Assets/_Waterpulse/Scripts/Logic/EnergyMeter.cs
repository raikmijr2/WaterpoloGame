using UnityEngine;

namespace Waterpulse.Logic
{
    // Barra de energía de un jugador: se recarga jugando bien y se gasta en habilidades
    public class EnergyMeter
    {
        public float Max { get; }
        public float Current { get; private set; }
        public float Normalized => Max > 0f ? Current / Max : 0f;

        public EnergyMeter(float max, float initial)
        {
            Max = Mathf.Max(0f, max);
            Current = Mathf.Clamp(initial, 0f, Max);
        }

        public void Add(float amount)
        {
            if (amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
        }

        public bool CanSpend(float cost) => cost >= 0f && cost <= Current;

        // Solo gasta si hay suficiente; si no, no cambia nada
        public bool TrySpend(float cost)
        {
            if (!CanSpend(cost)) return false;
            Current -= cost;
            return true;
        }
    }
}

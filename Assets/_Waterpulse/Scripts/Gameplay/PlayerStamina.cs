using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Estamina de un jugador de campo (distinta de la energía de habilidades): se recarga sola y la gasta el dash
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;

        StaminaMeter meter;

        // Se crea al primer uso para que funcione aunque Awake aún no se haya llamado
        StaminaMeter Meter => meter ??= new StaminaMeter(config.maxStamina, config.staminaRegenPerSecond);

        public float Current => Meter.Current;
        public float Max => Meter.Max;
        public float Normalized => Meter.Normalized;
        public bool CanDash => Meter.CanSpend(config.dashCost);

        public bool TrySpendDash() => Meter.TrySpend(config.dashCost, config.dashCooldown);

        void Update() => Meter.Tick(Time.deltaTime);
    }
}

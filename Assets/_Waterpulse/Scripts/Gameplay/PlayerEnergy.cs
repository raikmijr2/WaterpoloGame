using UnityEngine;
using Waterpulse.Config;
using Waterpulse.Logic;

namespace Waterpulse.Gameplay
{
    // Barra de energía de un jugador (GDD: cada jugador tiene la suya)
    public class PlayerEnergy : MonoBehaviour
    {
        [SerializeField] EnergyConfigSO config;

        EnergyMeter meter;

        // Se crea al primer uso para que funcione aunque Awake aún no se haya llamado
        EnergyMeter Meter => meter ??= new EnergyMeter(config.maxEnergy, config.initialEnergy);

        public float Current => Meter.Current;
        public float Max => Meter.Max;
        public float Normalized => Meter.Normalized;

        public void Add(float amount) => Meter.Add(amount);
        public bool CanSpend(float cost) => Meter.CanSpend(cost);
        public bool TrySpend(float cost) => Meter.TrySpend(cost);

        // Recarga sola (el porter, para el muro). Los demás tienen 0 y solo ganan energía con las recompensas
        void Update()
        {
            if (config.passiveRechargePerSecond > 0f) Meter.Add(config.passiveRechargePerSecond * Time.deltaTime);
        }
    }
}

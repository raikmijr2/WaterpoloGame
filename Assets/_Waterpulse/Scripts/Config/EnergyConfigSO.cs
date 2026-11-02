using UnityEngine;

namespace Waterpulse.Config
{
    // Energía de los jugadores (GDD sección 4)
    [CreateAssetMenu(menuName = "Waterpulse/Energy Config", fileName = "EnergyConfig")]
    public class EnergyConfigSO : ScriptableObject
    {
        public float maxEnergy = 100f;
        [Tooltip("Energía con la que empieza cada jugador")]
        public float initialEnergy = 50f;
        [Tooltip("Recarga sola por segundo (0 = solo con las recompensas). El porter la usa para el muro: 100 en ~60 s")]
        public float passiveRechargePerSecond = 0f;

        [Header("Recompensas")]
        [Tooltip("Al pasador, por pase completado")]
        public float passReward = 5f;
        [Tooltip("Al que intercepta un pase o roba con J")]
        public float recoveryReward = 10f;
        [Tooltip("Al que marca")]
        public float goalReward = 10f;
    }
}

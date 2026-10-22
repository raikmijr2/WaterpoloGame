using UnityEngine;

namespace Waterpulse.Config
{
    // Puntos por jugador (GDD sección 8) y reglas del combo. Los puntos nunca deciden el resultado
    [CreateAssetMenu(menuName = "Waterpulse/Scoring Config", fileName = "ScoringConfig")]
    public class ScoringConfigSO : ScriptableObject
    {
        [Header("Puntos (GDD)")]
        public int goalPoints = 100;
        [Tooltip("Extra por marcar con el Xut del Tràngol")]
        public int abilityGoalBonus = 50;
        [Tooltip("Extra al que marca tras esta cantidad de pases seguidos de su equipo")]
        public int comboPassesRequired = 3;
        public int comboPassesBonus = 30;
        public int savePoints = 25;
        [Tooltip("Por interceptar un pase rival")]
        public int interceptionPoints = 20;
        [Tooltip("Por robar la pelota con J (no está en el GDD: igual que interceptar, decidido en la Fase 7)")]
        public int recoveryPoints = 20;
        [Tooltip("Por pase completado")]
        public int passPoints = 10;

        [Header("Multiplicador de combo")]
        [Tooltip("Cuánto sube el multiplicador por cada acción positiva encadenada")]
        public float comboStep = 0.1f;
        [Tooltip("Multiplicador máximo")]
        public float comboMax = 2f;

        [Header("Habilidades con éxito")]
        [Tooltip("Torpede Blau: tiempo extra tras el acelerón para considerarlo un éxito (s)")]
        public float torpedeSuccessGrace = 0.5f;

        [Header("Récord personal")]
        [Tooltip("Clave con la que se guarda el récord en el navegador")]
        public string recordKey = "waterpulse.record.homePoints";
    }
}

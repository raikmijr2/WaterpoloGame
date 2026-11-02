using UnityEngine;

namespace Waterpulse.Config
{
    // Puntos por jugador (GDD sección 8), multiplicador de posesión y contraataque. Los puntos nunca deciden el resultado
    [CreateAssetMenu(menuName = "Waterpulse/Scoring Config", fileName = "ScoringConfig")]
    public class ScoringConfigSO : ScriptableObject
    {
        [Header("Puntos (GDD)")]
        public int goalPoints = 100;
        [Tooltip("Extra por marcar con el Xut del Tràngol")]
        public int abilityGoalBonus = 50;
        public int savePoints = 25;
        [Tooltip("Por interceptar un pase rival")]
        public int interceptionPoints = 20;
        [Tooltip("Por robar la pelota con J (no está en el GDD: igual que interceptar, decidido en la Fase 7)")]
        public int recoveryPoints = 20;
        [Tooltip("Por pase completado")]
        public int passPoints = 10;

        [Header("Multiplicador de posesión (solo multiplica el gol; vuelve a ×1 al perder la pelota)")]
        [Tooltip("Lo que sube por cada pase completado")]
        public float passMultiplier = 0.1f;
        [Tooltip("Lo que sube al recuperar la pelota (intercepción o robo con J)")]
        public float recoveryMultiplier = 0.2f;
        [Tooltip("Multiplicador máximo (decidido: ×3)")]
        public float multiplierMax = 3f;
        [Tooltip("Primera racha: pases seguidos y lo que sube además")]
        public int shortStreak = 3;
        public float shortStreakBonus = 0.2f;
        [Tooltip("Segunda racha: pases seguidos y lo que sube además")]
        public int longStreak = 5;
        public float longStreakBonus = 0.3f;

        [Header("Contraataque (decidido)")]
        [Tooltip("Marcar antes de este tiempo desde que se recupera la pelota (o desde la parada) da el extra (s)")]
        public float counterAttackWindow = 5f;
        public int counterAttackBonus = 50;

        [Header("Habilidades con éxito")]
        [Tooltip("Torpede Blau: tiempo extra tras el acelerón para considerarlo un éxito (s)")]
        public float torpedeSuccessGrace = 0.5f;

        [Header("Récord personal")]
        [Tooltip("Clave con la que se guarda el récord de cada usuario en el navegador ({0} = id del usuario)")]
        public string userRecordKeyFormat = "waterpulse.record.user.{0}";
    }
}

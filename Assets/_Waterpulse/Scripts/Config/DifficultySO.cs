using UnityEngine;

namespace Waterpulse.Config
{
    // Nivel de dificultad de la IA rival (GDD sección 9). Ajusta velocidad y agresividad, nunca hace trampas
    [CreateAssetMenu(menuName = "Waterpulse/Difficulty", fileName = "Difficulty")]
    public class DifficultySO : ScriptableObject
    {
        [Tooltip("Nombre visible del nivel (en catalán)")]
        public string displayName = "Competidor";
        [Tooltip("Cada cuánto vuelve a decidir la IA (s). Más bajo = reacciona antes")]
        public float reactionTime = 0.35f;
        [Tooltip("Probabilidad de intentar recuperar en cada decisión cuando presiona (0-1)")]
        [Range(0f, 1f)] public float aggressiveness = 0.6f;
        [Tooltip("Velocidad de los rivales respecto a la máxima (0-1)")]
        [Range(0.1f, 1f)] public float rivalSpeedMultiplier = 1f;
        [Tooltip("La IA rival usa habilidades especiales (GDD: nivel Competidor)")]
        public bool useAbilities = true;
        [Tooltip("Probabilidad de que la IA suelte el tiro en la zona verde de la barra (0-1)")]
        [Range(0f, 1f)] public float shotGreenChance = 0.5f;
        [Tooltip("Cuando falla el verde, lo antes que suelta el tiro (0-1 de la barra); suelta entre esto y el verde")]
        [Range(0f, 1f)] public float shotMissMinFill = 0.5f;
    }
}

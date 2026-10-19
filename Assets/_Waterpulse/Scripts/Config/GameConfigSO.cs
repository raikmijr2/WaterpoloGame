using UnityEngine;

namespace Waterpulse.Config
{
    // Valores generales del partido: movimiento en el agua, medidas de la piscina y cámara
    [CreateAssetMenu(menuName = "Waterpulse/Game Config", fileName = "GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Movimiento en el agua")]
        [Tooltip("Velocidad máxima de nado (m/s)")]
        public float maxSpeed = 4f;
        [Tooltip("Aceleración hacia la velocidad deseada mientras hay input (m/s²)")]
        public float acceleration = 8f;
        [Tooltip("Frenado del Rigidbody al soltar el input. Más bajo = más deriva")]
        public float linearDamping = 2f;
        [Tooltip("Velocidad de giro del jugador hacia donde nada (grados/s)")]
        public float turnSpeed = 540f;
        [Tooltip("Radio del cuerpo del jugador (m). Requiere regenerar la escena")]
        public float playerRadius = 0.4f;

        [Header("Piscina (requiere regenerar la escena)")]
        [Tooltip("Largo del campo, de portería a portería (m)")]
        public float poolLength = 20f;
        [Tooltip("Ancho del campo (m)")]
        public float poolWidth = 12f;
        public float goalWidth = 3f;
        public float goalHeight = 0.9f;

        [Header("Cámara")]
        [Tooltip("Inclinación: 90 = cenital")]
        [Range(30f, 90f)] public float cameraPitch = 60f;
        [Tooltip("Margen alrededor del campo que siempre queda visible (m)")]
        public float cameraMargin = 1.5f;
        public float cameraFieldOfView = 40f;
    }
}

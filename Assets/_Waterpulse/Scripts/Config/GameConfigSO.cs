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
        [Tooltip("Masa del jugador (kg). Requiere regenerar la escena")]
        public float playerMass = 70f;

        [Header("Pelota")]
        [Tooltip("Radio de la pelota (m). Mayor que el real para que se lea bien. Requiere regenerar la escena")]
        public float ballRadius = 0.15f;
        [Tooltip("Masa de la pelota (kg). Requiere regenerar la escena")]
        public float ballMass = 0.45f;
        [Tooltip("Distancia entre centros a la que un jugador recoge o recibe la pelota (m)")]
        public float pickupRadius = 0.6f;
        [Tooltip("Distancia delante del jugador a la que lleva la pelota (m)")]
        public float ballHoldDistance = 0.6f;
        [Tooltip("Frenado de la pelota suelta en el agua")]
        public float ballLinearDamping = 1.5f;
        [Tooltip("Rebote de la pelota contra paredes, postes y jugadores (0-1). Requiere regenerar la escena")]
        [Range(0f, 1f)] public float ballBounciness = 0.6f;
        [Tooltip("Tiempo durante el que quien suelta la pelota no puede volver a cogerla (s)")]
        public float releaserCooldown = 0.3f;

        [Header("Pase")]
        public float passSpeed = 10f;
        [Tooltip("Semiángulo del cono en el que se busca receptor alrededor de la dirección del stick (grados)")]
        public float passConeAngle = 60f;
        [Tooltip("Distancia que recorre un pase sin receptor antes de quedar libre (m)")]
        public float passNoTargetDistance = 6f;

        [Header("Tiro")]
        public float shotSpeed = 14f;
        [Tooltip("Distancia al palo que se deja al apuntar con el stick (m)")]
        public float shotPostMargin = 0.3f;

        [Header("Portero")]
        [Tooltip("Velocidad del portero siguiendo la pelota (m/s)")]
        public float goalkeeperSpeed = 3f;
        [Tooltip("Velocidad del portero al reaccionar a un tiro (m/s)")]
        public float goalkeeperReactionSpeed = 4f;
        [Tooltip("Distancia del portero a su línea de gol (m)")]
        public float goalkeeperDepth = 0.8f;
        [Tooltip("Lo más lejos de la línea que sale a por una pelota suelta (m)")]
        public float goalkeeperMaxDepth = 2.5f;
        [Tooltip("Cuánto puede salir más allá de cada palo (m)")]
        public float goalkeeperLateralExtra = 0.3f;
        [Tooltip("Fracción del desplazamiento lateral de la pelota que sigue (0-1)")]
        [Range(0f, 1f)] public float goalkeeperFollow = 0.6f;
        [Tooltip("Sale a por pelotas sueltas a esta distancia de su posición (m)")]
        public float goalkeeperLooseBallRadius = 2.5f;
        [Tooltip("Probabilidad de parar un tiro que le llega al cuerpo (0-1)")]
        [Range(0f, 1f)] public float saveChanceAtBody = 0.8f;
        [Tooltip("Distancia al portero a la que la probabilidad de parada llega a 0 (m)")]
        public float saveReach = 1.2f;
        [Tooltip("A esta distancia de su objetivo deja de moverse (m). Más pequeño que el de los jugadores: necesita precisión")]
        public float goalkeeperArriveRadius = 0.1f;
        [Tooltip("Empieza a frenar a esta distancia de su objetivo (m)")]
        public float goalkeeperSlowRadius = 1f;
        [Tooltip("Tiempo que retiene la pelota antes de sacar (s)")]
        public float goalkeeperHoldTime = 1f;
        [Tooltip("Un compañero está libre si no tiene rivales a menos de esta distancia (m)")]
        public float goalkeeperOutletFreeDistance = 2f;

        [Header("Colocación por rol (x = avance hacia la portería rival, y = desplazamiento hacia el centro)")]
        [Tooltip("Fracción del desplazamiento lateral de la pelota que siguen los jugadores (0-1)")]
        [Range(0f, 1f)] public float formationLateralFollow = 0.5f;
        public Vector2 attackerWithBall = new Vector2(4f, 2.5f);
        public Vector2 playmakerWithBall = new Vector2(-1.5f, -2.5f);
        public Vector2 defenderWithBall = new Vector2(-5f, 0f);
        public Vector2 attackerWithoutBall = new Vector2(-1.5f, -2f);
        public Vector2 playmakerWithoutBall = new Vector2(-3f, 2f);
        public Vector2 defenderWithoutBall = new Vector2(-5f, 0f);
        [Tooltip("Distancia mínima a la línea de gol rival a la que se colocan (m)")]
        public float formationMinGoalDistance = 2f;
        [Tooltip("Distancia mínima a bandas y línea de gol propia (m)")]
        public float formationEdgeMargin = 1f;
        [Tooltip("Sin posesión, marcan al rival que esté a menos de esta distancia de su posición (m)")]
        public float markRadius = 3f;
        [Tooltip("Distancia a la que se ponen del rival marcado, hacia su portería (m)")]
        public float markDistance = 1.2f;

        [Header("IA: movimiento")]
        [Tooltip("A esta distancia del objetivo se considera que ha llegado (m)")]
        public float aiArriveRadius = 0.5f;
        [Tooltip("Empieza a frenar a esta distancia del objetivo (m)")]
        public float aiSlowRadius = 2f;

        [Header("Reanudación tras gol")]
        [Tooltip("Pausa entre el gol y la reanudación desde el centro (s)")]
        public float goalRestartDelay = 1.5f;

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

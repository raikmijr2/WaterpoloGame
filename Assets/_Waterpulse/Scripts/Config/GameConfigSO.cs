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
        public float acceleration = 30f;
        [Tooltip("Frenado del Rigidbody al soltar el input. Más bajo = más deriva")]
        public float linearDamping = 15f;
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
        [Tooltip("Tiempo que el pasador se queda quieto al pasar (s); dura también su saltito")]
        public float passFreezeDuration = 0.3f;
        [Tooltip("Altura del saltito al pasar (m, solo visual)")]
        public float passHopHeight = 0.3f;
        [Tooltip("Con modelo animado la pelota sale con el evento ReleaseBall de la animación; esto es lo máximo que se espera antes de soltarla igualmente (s). Sin modelo sale al momento")]
        public float throwReleaseTimeout = 1f;

        [Header("Tiro")]
        public float shotSpeed = 14f;
        [Tooltip("Distancia al palo que se deja al apuntar con el stick (m)")]
        public float shotPostMargin = 0.3f;

        [Header("Barra de tiro (K para empezar a cargar, K otra vez para tirar)")]
        [Tooltip("Tiempo de juego que tarda la barra en llenarse (s; va con la cámara lenta, así que en tiempo real es esto / shotSlowMotionScale). Si se llena entera, tira solo con la peor calidad")]
        public float shotMeterDuration = 0.4f;
        [Tooltip("Inicio de la zona verde (0-1 de la barra)")]
        [Range(0f, 1f)] public float shotGreenStart = 0.85f;
        [Tooltip("Ancho de la zona verde (0-1 de la barra)")]
        [Range(0f, 1f)] public float shotGreenWidth = 0.08f;
        [Tooltip("Calidad máxima fuera del verde (justo al lado de la zona). Baja hasta 0 en los extremos de la barra")]
        [Range(0f, 1f)] public float shotOffGreenMaxQuality = 0.7f;
        [Tooltip("Cuánto sube el jugador mientras carga, con la barra llena (m, solo visual)")]
        public float shotRiseHeight = 0.25f;
        [Tooltip("Potencia del tiro con calidad 0 respecto a la normal (con calidad 1 es la normal)")]
        [Range(0f, 1f)] public float shotMinPowerFactor = 0.6f;
        [Tooltip("Desvío máximo del tiro con calidad 0 (grados). Con calidad 1 va donde apuntas")]
        public float shotMaxErrorAngle = 12f;
        [Tooltip("Velocidad del tiempo mientras alguien carga el tiro (1 = normal). Deja tiempo para apuntar y para coger al portero")]
        [Range(0.05f, 1f)] public float shotSlowMotionScale = 0.3f;
        [Tooltip("Ratón: tiempo para el segundo clic del doble clic (súper xut). El clic izquierdo espera esto antes de cargar el tiro (s, tiempo real)")]
        public float mouseDoubleClickWindow = 0.25f;
        [Tooltip("Lo que se mueve el punto de mira por segundo real mientras carga (fracción de media portería)")]
        public float shotAimSpeed = 2f;

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
        [Tooltip("Distancia a la que se ponen del rival marcado, hacia su portería (m)")]
        public float markDistance = 1.2f;

        [Header("Recuperar (J sin pelota)")]
        [Tooltip("Distancia máxima al poseedor para intentar recuperar (m)")]
        public float recoverRange = 1.2f;
        [Tooltip("Solo se puede robar desde delante del que lleva la pelota: arco frontal en grados (180 = su mitad de delante). De espaldas la protege")]
        [Range(0f, 360f)] public float recoverFrontAngle = 180f;
        [Tooltip("A esta distancia o menos se aplica la probabilidad máxima (m)")]
        public float recoverCloseDistance = 0.8f;
        [Tooltip("Probabilidad al límite del alcance (0-1)")]
        [Range(0f, 1f)] public float recoverChanceFar = 0.5f;
        [Tooltip("Probabilidad pegado al poseedor (0-1)")]
        [Range(0f, 1f)] public float recoverChanceClose = 0.75f;
        [Tooltip("Espera tras un intento fallido (s)")]
        public float recoverCooldown = 0.8f;
        [Tooltip("Tiempo tras ganar la pelota durante el que no te la pueden quitar (s). 0 = sin protección")]
        public float recoverImmunity = 0.5f;

        [Header("Intercepción de pases")]
        [Tooltip("Un pase que pasa a esta distancia de un rival puede ser interceptado (m)")]
        public float interceptRadius = 0.8f;
        [Tooltip("Probabilidad de interceptar si la pelota le llega al cuerpo (una tirada por rival y pase)")]
        [Range(0f, 1f)] public float interceptChanceAtBody = 0.9f;
        [Tooltip("Probabilidad de interceptar si la pelota pasa justo en el borde del alcance")]
        [Range(0f, 1f)] public float interceptChanceAtEdge = 0.3f;

        [Header("Cambio automático de jugador al defender")]
        [Tooltip("El otro jugador debe estar al menos esto más cerca de la pelota para cambiar (m)")]
        public float autoSwitchMargin = 1.5f;
        [Tooltip("Tiempo mínimo entre cambios automáticos (s)")]
        public float autoSwitchCooldown = 0.5f;
        [Tooltip("Tras cambiar de jugador a mano (Q), el cambio automático espera este tiempo para no deshacerlo (s)")]
        public float manualSwitchLock = 2f;

        [Header("IA: ataque")]
        [Tooltip("Tira si está a esta distancia de la portería o menos y la línea está libre (m)")]
        public float aiShootRange = 6f;
        [Tooltip("Se siente presionado si un rival está a esta distancia o menos (m)")]
        public float aiPressureRadius = 1.5f;
        [Tooltip("Anchura de las líneas de pase y tiro: un rival a menos de esto la tapa (m)")]
        public float aiLaneWidth = 0.8f;
        [Tooltip("Distancia a la portería rival a la que avanza el jugador con pelota (m)")]
        public float aiAdvanceDistance = 5f;
        [Tooltip("Distancia del presionador al poseedor, del lado de su portería (m)")]
        public float aiPressDistance = 0.8f;
        [Tooltip("Tiempo máximo con la pelota sin hacer nada: después tira (en zona de tiro) o pasa (s)")]
        public float aiMaxHoldTime = 3f;
        [Tooltip("Pasa sin que le presionen si un compañero con línea limpia está al menos esto más adelantado (m)")]
        public float aiPassAdvanceGain = 3f;
        [Tooltip("Cuánto se desplaza en lateral para buscar hueco de tiro cuando le tapan la línea (m)")]
        public float aiRepositionStep = 2f;
        [Tooltip("La IA usa el acelerón (Torpede Blau) para llegar a una pelota suelta más lejos que esto (m)")]
        public float aiBoostDistance = 6f;

        [Header("IA: juego en triángulo")]
        [Tooltip("Desmarcado = ningún defensor de campo a menos de esto (m)")]
        public float aiOpenRadius = 2.5f;
        [Tooltip("Sin presión, no pasa antes de llevar la pelota esto (s): así no se la pasan a lo loco")]
        public float aiMinHoldBeforePass = 0.5f;
        [Tooltip("Pasa a un compañero que esté al menos esto más libre que él (m)")]
        public float aiPassOpenGain = 1f;
        [Tooltip("Al elegir a quién pasar, lo que vale cada metro que el compañero está más adelantado")]
        public float aiPassAdvanceWeight = 0.5f;
        [Tooltip("Punta del triángulo: por delante del poseedor (m) y separación del centro hacia el lado libre (m)")]
        public float aiPointAhead = 4f;
        public float aiPointSide = 2.5f;
        [Tooltip("Suport del triángulo: por detrás del poseedor (m) y abierto hacia el lado libre (m)")]
        public float aiWingBack = 1.5f;
        public float aiWingSide = 5f;
        [Tooltip("Para desmarcarse, cuántos puntos prueba alrededor de su sitio y a qué distancia (m)")]
        public int aiSupportCandidates = 8;
        public float aiSupportSearchRadius = 2f;
        [Tooltip("Solo cambia de punto si el nuevo es mejor por al menos esto (evita que tiemble)")]
        public float aiSupportSwitchMargin = 0.75f;
        [Tooltip("Resta a un punto con la línea de pase tapada o demasiado pegado al poseedor")]
        public float aiSupportBlockedPenalty = 3f;
        [Tooltip("Suma a un punto en zona de tiro con la línea libre")]
        public float aiSupportShotBonus = 1f;
        [Tooltip("Resta por cada metro que se aleja de su sitio en el triángulo")]
        public float aiSupportShapeWeight = 0.3f;
        [Tooltip("Separación mínima con el poseedor para recibir (m)")]
        public float aiSupportMinSpacing = 3f;
        [Tooltip("Presión doble: si el poseedor rival está a esto de la portería o menos, va un segundo defensor (m)")]
        public float aiDoublePressRange = 7f;
        [Tooltip("Ángulo del segundo defensor respecto al primero, al lado del poseedor (grados)")]
        public float aiDoublePressAngle = 60f;

        [Header("IA: movimiento")]
        [Tooltip("A esta distancia del objetivo se considera que ha llegado (m)")]
        public float aiArriveRadius = 0.5f;
        [Tooltip("Empieza a frenar a esta distancia del objetivo (m)")]
        public float aiSlowRadius = 2f;

        [Header("Flujo de partido")]
        [Tooltip("Duración del partido (s). El GDD pide entre 120 y 180. Si hay empate al acabar, gol de oro")]
        public float matchDuration = 150f;
        [Tooltip("Tiempo quieto en el saque antes de empezar a jugar (s)")]
        public float kickoffDelay = 1.5f;
        [Tooltip("Pausa entre el gol y el siguiente saque (s)")]
        public float goalRestartDelay = 1.5f;
        [Tooltip("Pausa entre el final del tiempo y la pantalla de resultados (s)")]
        public float fullTimeDelay = 1.5f;

        [Header("Posiciones de saque (x = avance hacia la portería rival, y = lateral; el visitante se refleja)")]
        public Vector2 attackerKickoff = new Vector2(-1.5f, 2f);
        public Vector2 playmakerKickoff = new Vector2(-4f, -2.5f);
        public Vector2 defenderKickoff = new Vector2(-7f, 0f);

        [Header("Textos visibles (catalán)")]
        public string homeTeamName = "LOCAL";
        public string awayTeamName = "VISITANT";
        [Tooltip("Tiempo que se ve en pantalla el nombre de una habilidad al activarse (s)")]
        public float abilityAnnounceDuration = 1f;

        [Header("Modelos de los jugadores de campo (requiere regenerar la escena; los porteros son cápsulas)")]
        [Tooltip("Prefab del equipo LOCAL con el modelo y su Animator. Vacío = el de Prefabs/Waterpolo_TreadingWater; si no existe, cápsulas")]
        public GameObject homePlayerModel;
        [Tooltip("Prefab del equipo VISITANTE. Vacío = el de Prefabs/WaterpoloEnemy; si no existe, cápsulas")]
        public GameObject awayPlayerModel;
        [Tooltip("Prefab del portero LOCAL (portería de la izquierda). Vacío = el de Prefabs/Waterpolo_GoalSave; si no existe, cápsula")]
        public GameObject homeGoalkeeperModel;
        [Tooltip("Prefab del portero VISITANTE (portería de la derecha). Vacío = el de Prefabs/Waterpolo_GoalSave 1; si no existe, cápsula")]
        public GameObject awayGoalkeeperModel;
        [Tooltip("Posición del modelo respecto al jugador (m), para los dos equipos. Y negativa = más hundido en el agua")]
        public Vector3 playerModelOffset = new Vector3(0f, -0.825f, 0f);
        [Tooltip("Escala del modelo")]
        public float playerModelScale = 1.5f;
        [Tooltip("Hueso de la mano en la que el modelo coge la pelota para pasar o tirar (solo visual)")]
        public HumanBodyBones ballHandBone = HumanBodyBones.RightHand;
        [Tooltip("Posición de la pelota respecto al hueso de la mano, en sus ejes (m). Para dejarla en la palma")]
        public Vector3 ballHandOffset = new Vector3(0f, 0.1f, 0f);
        [Tooltip("Tiempo en que la pelota visible pasa de la mano a su trayectoria al soltarla (s)")]
        public float ballReleaseBlendTime = 0.1f;
        [Tooltip("Tiempo en que la pelota visible sube del agua a la mano al empezar a pasar o a cargar el tiro (s)")]
        public float ballGrabBlendTime = 0.1f;
        [Tooltip("Distancia delante del jugador a la que se ve la pelota mientras la empuja nadando (m, solo visual). Para dejarla delante de la cara")]
        public float ballDribbleDistance = 0.6f;
        [Tooltip("Vaivén adelante y atrás de la pelota empujada a velocidad máxima (m, solo visual). Parado no se mueve")]
        public float ballDribbleSurge = 0.08f;
        [Tooltip("Vaivenes por segundo de la pelota empujada")]
        public float ballDribbleSurgeFrequency = 2.5f;

        [Header("Piscina (requiere regenerar la escena)")]
        [Tooltip("Largo del campo, de portería a portería (m)")]
        public float poolLength = 30f;
        [Tooltip("Ancho del campo (m)")]
        public float poolWidth = 20f;
        public float goalWidth = 3f;
        public float goalHeight = 0.9f;

        [Header("Cámara")]
        [Tooltip("Inclinación: 90 = cenital")]
        [Range(30f, 90f)] public float cameraPitch = 60f;
        [Tooltip("Lo más que se ve fuera del campo cuando la cámara llega a un borde (m)")]
        public float cameraMargin = 1.5f;
        public float cameraFieldOfView = 40f;
        [Tooltip("Largo del campo que encuadra la cámara a lo ancho de la pantalla (m)")]
        public float cameraViewLength = 17f;
        [Tooltip("Suavizado del seguimiento (s). Más alto = la cámara va más por detrás")]
        public float cameraSmoothTime = 0.3f;
        [Tooltip("Adelanto de la cámara hacia la portería que ataca el equipo con la pelota (m)")]
        public float cameraLead = 2f;
        [Tooltip("Temblor de cámara cuando fallas un intento de robo: duración (s)")]
        public float cameraShakeDuration = 0.15f;
        [Tooltip("Temblor de cámara cuando fallas un intento de robo: amplitud (m)")]
        public float cameraShakeAmplitude = 0.12f;

        [Header("Celebración del gol (solo aspecto)")]
        [Tooltip("Temblor de cámara al marcar: duración (s, tiempo real) y amplitud (m)")]
        public float goalShakeDuration = 0.7f;
        public float goalShakeAmplitude = 0.45f;
        [Tooltip("Golpe de zoom al marcar: grados que se cierra la cámara y lo que tarda en volver (s)")]
        public float goalZoomPunch = 7f;
        public float goalZoomDuration = 0.9f;
        [Tooltip("El público salta más alto y más rápido durante este tiempo tras un gol (s)")]
        public float goalCrowdHypeDuration = 4f;
        [Tooltip("Cuántas veces más alto y más rápido salta el público al principio de la celebración")]
        public float goalCrowdJumpBoost = 3f;
        public float goalCrowdSpeedBoost = 1.7f;

        [Header("Estamina, dash y nado lento")]
        public float maxStamina = 100f;
        [Tooltip("Recarga por segundo")]
        public float staminaRegenPerSecond = 20f;
        public float dashCost = 30f;
        [Tooltip("Duración del impulso (s)")]
        public float dashDuration = 0.075f;
        [Tooltip("Velocidad del dash respecto a la máxima de nado. Siempre da este impulso, también yendo a tope (decidido por el usuario)")]
        public float dashSpeedMultiplier = 3f;
        [Tooltip("Espera mínima entre dashes (s)")]
        public float dashCooldown = 0.5f;
        [Tooltip("Velocidad al nadar lento (Espacio) respecto a la máxima (0-1)")]
        [Range(0.1f, 1f)] public float slowSwimMultiplier = 0.4f;
        [Tooltip("Frenada cuando vas más rápido de lo que pide el stick, p. ej. al pulsar Espacio (m/s²). Alto = para casi en seco")]
        public float slowBrakeDeceleration = 40f;
        [Tooltip("La IA (rivales y compañeros) hace dash si el objetivo está más lejos que esto (m)")]
        public float aiDashDistance = 4f;

        [Header("Mur de Ferro (portero)")]
        [Tooltip("Altura desde la que cae el muro (m) y lo que tarda en caer (s)")]
        public float wallDropHeight = 6f;
        public float wallDropTime = 0.25f;
        [Tooltip("Lo que tarda en hundirse al acabar (s)")]
        public float wallSinkTime = 0.4f;
        [Tooltip("La onada empuja a los rivales de campo que estén a menos de esto de la portería (m), como la línea de 5 m")]
        public float wallPushRadius = 5f;
        [Tooltip("Se quedan esto más allá del radio (m): salen bien despedidos")]
        public float wallPushMargin = 2.5f;
        [Tooltip("Lo que dura el empujón (s): salen despedidos y van frenando")]
        public float wallPushDuration = 0.6f;
        [Tooltip("Salto (solo visual) de los empujados (m) y vueltas que dan sobre sí mismos")]
        public float wallPushHopHeight = 0.9f;
        public float wallPushSpins = 1f;
        [Tooltip("Lo que tarda el anillo de espuma en llegar al radio (s)")]
        public float wallWaveDuration = 0.5f;
        [Tooltip("Onada en el agua (shader): velocidad (m/s), altura (m), anchura de la cresta (m), duración (s) y espuma (0-1)")]
        public float wallWaveSpeed = 9f;
        public float wallWaveHeight = 0.55f;
        public float wallWaveWidth = 0.7f;
        public float wallWaveLife = 1.6f;
        [Range(0f, 1f)] public float wallWaveFoam = 0.9f;
        [Tooltip("Temblor de cámara al caer el muro")]
        public float wallShakeDuration = 0.45f;
        public float wallShakeAmplitude = 0.4f;

        [Header("Pausa")]
        [Tooltip("Al continuar, el partido espera esto antes de seguir, para que la tecla con la que se confirma no sea un pase (s)")]
        public float resumeDelay = 0.2f;
    }
}

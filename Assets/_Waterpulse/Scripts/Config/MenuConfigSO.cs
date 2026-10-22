using UnityEngine;

namespace Waterpulse.Config
{
    // Valores del menú principal y de la intro: escenas, tiempos de las animaciones, cámara de fondo y volumen
    [CreateAssetMenu(menuName = "Waterpulse/Menu Config", fileName = "MenuConfig")]
    public class MenuConfigSO : ScriptableObject
    {
        [Header("Escenas")]
        [Tooltip("Escena del partido individual contra la IA")]
        public string matchScene = "Match";
        [Tooltip("Escena del menú principal (la primera del build)")]
        public string menuScene = "Menu";

        [Header("Intro")]
        [Tooltip("Duración del fundido con el que aparece el logo (s)")]
        public float logoFadeDuration = 1f;
        [Tooltip("Periodo del parpadeo de \"Prem qualsevol tecla\" (s)")]
        public float promptBlinkPeriod = 1f;

        [Header("Navegación")]
        [Tooltip("Duración del fundido al cambiar de pantalla (s)")]
        public float pageFadeDuration = 0.2f;
        [Tooltip("Tras cambiar de pantalla, la entrada se ignora este tiempo para que la misma pulsación no active el primer botón (s)")]
        public float inputGuard = 0.2f;

        [Header("Cámara de fondo")]
        [Tooltip("Velocidad de giro alrededor de la piscina (grados/s)")]
        public float cameraOrbitSpeed = 4f;
        [Tooltip("Distancia horizontal de la cámara al centro de la piscina (m)")]
        public float cameraDistance = 20f;
        [Tooltip("Altura de la cámara sobre el agua (m)")]
        public float cameraHeight = 11f;
        [Tooltip("Campo de visión vertical de la cámara (grados)")]
        public float cameraFieldOfView = 50f;

        [Header("Volumen")]
        [Tooltip("Volumen general la primera vez que se juega (0-1)")]
        [Range(0f, 1f)] public float defaultVolume = 1f;
        [Tooltip("Espera desde el último cambio del deslizador hasta guardar el volumen, para no escribir en el navegador a cada paso (s)")]
        public float volumeSaveDelay = 0.5f;
        [Tooltip("Clave con la que se guarda el volumen en el navegador")]
        public string volumeKey = "waterpulse.settings.volume";
    }
}

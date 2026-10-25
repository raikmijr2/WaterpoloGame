using System;
using UnityEngine;

namespace Waterpulse.Config
{
    // Sonidos y música del juego y su mezcla. Los sonidos reales están en Audio/ (créditos en docs/audio-credits.md);
    // "Waterpulse/Generate Sounds" (y los generadores de escena) los asigna aquí y solo sintetiza los que falten
    [CreateAssetMenu(menuName = "Waterpulse/Audio Config", fileName = "AudioConfig")]
    public class AudioConfigSO : ScriptableObject
    {
        // Sonido que suena al activar una habilidad (se busca por la habilidad usada)
        [Serializable]
        public class AbilitySound
        {
            public AbilityDefinitionSO ability;
            public AudioClip clip;
        }

        [Header("Música (en bucle) y jingles del resumen")]
        public AudioClip menuMusic;
        public AudioClip matchMusic;
        public AudioClip winJingle;
        public AudioClip loseJingle;

        [Header("Árbitro y marcador (como en el waterpolo: el árbitro pita los goles y la bocina marca el final del tiempo)")]
        [Tooltip("Silbato del árbitro: al reanudar el juego y al marcar")]
        public AudioClip whistleShort;
        [Tooltip("Silbato largo: empieza el gol de oro")]
        public AudioClip whistleLong;
        [Tooltip("Bocina del marcador: se acaba el tiempo (final o paso al gol de oro)")]
        public AudioClip periodBuzzer;

        [Header("Piscina y público")]
        [Tooltip("Agua de fondo durante el partido: nadadores en la piscina (en bucle)")]
        public AudioClip poolLoop;
        [Tooltip("Público de fondo durante el partido: grada de una competición en la piscina (en bucle)")]
        public AudioClip crowdLoop;
        [Tooltip("Ovación: gol del equipo del jugador")]
        public AudioClip crowdCheer;
        [Tooltip("\"Oooh\": gol del rival o parada")]
        public AudioClip crowdOoh;

        [Header("Juego")]
        public AudioClip passSplash;
        public AudioClip shotSplash;
        [Tooltip("Al recibir un pase")]
        public AudioClip catchSlap;
        [Tooltip("Intercepción o robo con J")]
        public AudioClip steal;
        [Tooltip("Intento de robo fallido")]
        public AudioClip stealFail;
        public AudioClip dash;
        [Tooltip("La pelota en vuelo choca con un palo, una pared o un jugador")]
        public AudioClip bounce;
        [Tooltip("Parada del portero")]
        public AudioClip save;
        public AbilitySound[] abilitySounds = new AbilitySound[0];

        [Header("Interfaz")]
        [Tooltip("Moverse entre botones (teclado, mando o pasar el ratón)")]
        public AudioClip uiMove;
        [Tooltip("Pulsar un botón")]
        public AudioClip uiConfirm;

        [Header("Comentarios en catalán (vacío = no suena nada; si hay varios, uno al azar)")]
        public AudioClip[] goalComments = new AudioClip[0];
        public AudioClip[] saveComments = new AudioClip[0];
        public AudioClip[] goldenGoalComments = new AudioClip[0];
        public AudioClip[] fullTimeComments = new AudioClip[0];

        [Header("Mezcla (se multiplica por los volúmenes de Opcions)")]
        [Range(0f, 1f)] public float musicLevel = 0.45f;
        [Range(0f, 1f)] public float crowdLevel = 0.35f;
        [Range(0f, 1f)] public float poolLevel = 0.3f;
        [Range(0f, 1f)] public float effectsLevel = 0.85f;
        [Range(0f, 1f)] public float uiLevel = 0.5f;
        [Range(0f, 1f)] public float commentLevel = 1f;
        [Tooltip("Variación al azar del tono de los efectos (±), para que no suenen siempre igual")]
        [Range(0f, 0.5f)] public float pitchVariation = 0.08f;
        [Tooltip("Efectos que pueden sonar a la vez")]
        [Min(1)] public int effectVoices = 8;
        [Tooltip("Fundido de la música del partido al acabar el tiempo (s)")]
        public float musicFadeOut = 1f;
        [Tooltip("El agua y el público de fondo son grabaciones, no bucles perfectos: al acabar se funden consigo mismas durante este tiempo (s)")]
        public float ambienceCrossfade = 2f;
        [Tooltip("Lo más que dura una reacción del público (ovación o \"oooh\") antes de apagarse (s)")]
        public float crowdReactionMaxDuration = 4f;
        [Tooltip("Fundido con el que se apaga una reacción del público que llega a su duración máxima (s)")]
        public float crowdReactionFadeOut = 0.6f;
        [Tooltip("Gol de oro: tiempo entre la bocina y el silbato largo (s)")]
        public float goldenGoalWhistleDelay = 1.2f;
        [Tooltip("Espera mínima entre dos sonidos de interfaz (s), para que moverse muy rápido no sature")]
        public float uiMinInterval = 0.04f;
    }
}
